using CavagnaDemo.Data;
using CavagnaDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CavagnaDemo.Services;

public class QuoteService
{
    private readonly CavagnaDbContext _db;

    public QuoteService(CavagnaDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Restituisce tutti i preventivi presenti nel DB, senza filtri e senza ordinamento.
    /// Carica <c>Customer</c> e <c>Righe</c> ma NON i prodotti dentro le righe:
    /// <c>riga.Product</c> sarà <c>null</c> su ogni <see cref="QuoteItem"/>.
    /// </summary>
    /// <returns>Lista di tutti i preventivi con cliente e righe inclusi.</returns>
    /// <remarks>
    /// Nessuna paginazione: su volumi elevati la query può diventare costosa.
    /// Per il dettaglio con i prodotti usare <see cref="OttieniPreventivo"/>.
    /// </remarks>
    public async Task<List<Quote>> GetAll()
    {
        return await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Righe)
            .ToListAsync();
    }

    /// <summary>
    /// Restituisce il preventivo con l'ID specificato, inclusi cliente, righe e prodotto
    /// per ogni riga. È l'unico metodo che esegue il <c>ThenInclude</c> sui prodotti.
    /// </summary>
    /// <param name="id">Chiave primaria del preventivo.</param>
    /// <returns>
    /// Il preventivo con tutte le navigation property caricate,
    /// oppure <c>null</c> se non esiste.
    /// </returns>
    public async Task<Quote?> OttieniPreventivo(int id)
    {
        return await _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Righe)
                .ThenInclude(r => r.Product)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <summary>
    /// Crea un nuovo preventivo, calcola i subtotali per riga, applica lo sconto globale,
    /// aggiunge l'IVA al 22% e persiste tutto nel DB in un'unica transazione implicita.
    /// </summary>
    /// <param name="customerId">
    /// ID del cliente intestatario. Non viene verificata l'esistenza nel DB prima del salvataggio:
    /// se l'ID non esiste, EF genera un errore di FK violation a runtime.
    /// </param>
    /// <param name="righe">
    /// Righe del preventivo già costruite dal chiamante con <c>PrezzoUnitario</c>,
    /// <c>Quantita</c> e <c>ScontoPercentuale</c> valorizzati.
    /// <c>Subtotale</c> viene ignorato in ingresso e riscritto da questo metodo.
    /// La lista è assegnata per riferimento: modifiche esterne successive si rifletteranno
    /// sull'oggetto <see cref="Quote"/> restituito.
    /// </param>
    /// <param name="scontoTotale">
    /// Sconto globale espresso in percentuale (es. <c>5</c> = 5%). Default 0.
    /// Si applica sul totale già scontato per riga, non sul lordo.
    /// </param>
    /// <returns>Il preventivo salvato con <c>Id</c> valorizzato dal DB.</returns>
    /// <remarks>
    /// Calcolo applicato in sequenza:
    /// <list type="number">
    ///   <item>Per ogni riga: <c>Subtotale = PrezzoUnitario × Quantità × (1 − ScontoPercentuale/100)</c></item>
    ///   <item>Totale righe: somma dei subtotali netti</item>
    ///   <item>Sconto globale: <c>totaleRighe × (1 − scontoTotale/100)</c></item>
    ///   <item>IVA: <c>× 1.22</c> (hardcoded — non usa <see cref="PricingHelper.AddVat"/>)</item>
    /// </list>
    /// Gli sconti si compongono moltiplicativamente: sconto riga 10% + sconto globale 5%
    /// produce un effettivo ~14,5%, non 15%.
    /// Il risultato finale non viene arrotondato.
    /// Il numero preventivo (<c>PRV-{yyyyMMdd}-{rand 1000-9999}</c>) non è garantito univoco:
    /// due preventivi creati nello stesso giorno con lo stesso numero random produrrebbero
    /// lo stesso valore; non esiste vincolo UNIQUE nel DB.
    /// La scadenza è fissa a 30 giorni dalla creazione, senza eccezioni.
    /// Le date usano <c>DateTime.Now</c> (ora locale del server), non UTC.
    /// </remarks>
    public async Task<Quote> CreaPreventivo(int customerId, List<QuoteItem> righe, decimal scontoTotale = 0m)
    {
        var quote = new Quote
        {
            CustomerId = customerId,
            DataCreazione = DateTime.Now,
            DataScadenza = DateTime.Now.AddDays(30),
            NumeroPreventivo = $"PRV-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Stato = "Bozza",
            Righe = righe,
            ScontoTotale = scontoTotale
        };

        // Calcola subtotale per riga
        foreach (var r in righe)
        {
            var subtotale = r.PrezzoUnitario * r.Quantita;
            r.Subtotale = subtotale - (subtotale * r.ScontoPercentuale / 100m);
        }

        // Applica eventuale sconto globale e IVA
        var totaleRighe = righe.Sum(r => r.Subtotale);
        var scontoTotalePercentuale = quote.ScontoTotale;

        var totaleConSconto = totaleRighe - (totaleRighe * scontoTotalePercentuale / 100m);
        quote.Totale = totaleConSconto * 1.22m;

        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();
        return quote;
    }

    /// <summary>
    /// Aggiorna il campo <c>Stato</c> di un preventivo esistente.
    /// </summary>
    /// <param name="quoteId">ID del preventivo da aggiornare.</param>
    /// <param name="nuovoStato">
    /// Nuovo valore dello stato. I valori attesi dal dominio sono
    /// <c>Bozza</c>, <c>Inviato</c>, <c>Accettato</c>, <c>Rifiutato</c>,
    /// ma non viene eseguita nessuna validazione: qualsiasi stringa viene accettata e salvata.
    /// </param>
    /// <returns><c>true</c> se il preventivo esiste ed è stato aggiornato; <c>false</c> se non trovato.</returns>
    /// <remarks>
    /// Usa <c>FindAsync</c> che cerca prima nella cache EF prima di andare al DB.
    /// Non esiste una macchina a stati: è possibile passare da qualsiasi stato a qualsiasi altro
    /// (es. da <c>Accettato</c> a <c>Bozza</c>) senza vincoli.
    /// </remarks>
    public async Task<bool> AggiornaStato(int quoteId, string nuovoStato)
    {
        var quote = await _db.Quotes.FindAsync(quoteId);
        if (quote == null) return false;
        quote.Stato = nuovoStato;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Ricalcola il totale IVA inclusa a partire da un oggetto <see cref="Quote"/> già in memoria,
    /// senza accedere al DB.
    /// </summary>
    /// <param name="quote">Preventivo con <c>Righe</c> già valorizzate.</param>
    /// <returns>Somma dei subtotali netti per riga moltiplicata per 1.22 (IVA 22%).</returns>
    /// <remarks>
    /// <b>Incoerenza con <see cref="CreaPreventivo"/>:</b> questo metodo ignora
    /// <c>quote.ScontoTotale</c> — lo sconto globale non viene applicato.
    /// Il valore restituito sarà quindi diverso da <c>quote.Totale</c> ogni volta che
    /// è presente uno sconto globale maggiore di zero.
    /// Il risultato non viene arrotondato e non usa <see cref="PricingHelper"/>.
    /// </remarks>
    public decimal CalculateTotal(Quote quote)
    {
        decimal total = 0;
        foreach (var r in quote.Righe)
        {
            var sub = r.PrezzoUnitario * r.Quantita;
            total += sub - (sub * r.ScontoPercentuale / 100m);
        }
        return total * 1.22m;
    }
}
