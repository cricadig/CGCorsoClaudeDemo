using CavagnaDemo.Data;
using CavagnaDemo.Models;
using CavagnaDemo.Services;
using Microsoft.EntityFrameworkCore;

namespace CavagnaDemo.Tests;

/// <summary>
/// Verifica che CreaPreventivo applichi correttamente sconto di riga e sconto totale.
///
/// Formula:
///   subtotale_riga   = PrezzoUnitario × Quantita
///   dopo_sconto_riga = subtotale_riga × (1 − ScontoPercentuale/100)
///   totale_righe     = Σ dopo_sconto_riga
///   dopo_sconto_tot  = totale_righe × (1 − ScontoTotale/100)
///   Totale           = dopo_sconto_tot × 1.22  (IVA 22%)
/// </summary>
public class QuoteServiceScontoTests
{
    private CavagnaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CavagnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CavagnaDbContext(options);
    }

    /// <summary>
    /// Solo sconto di riga, nessuno sconto totale.
    /// 2 pz × 100 € − 10% = 180 € → IVA 22% = 219,60 €
    /// </summary>
    [Fact]
    public async Task CreaPreventivo_SoloScontoRiga()
    {
        var db = CreateDb();
        var customer = new Customer { RagioneSociale = "GasTech SpA", PartitaIVA = "09876543210", Email = "ordini@gastech.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var quote = await new QuoteService(db).CreaPreventivo(
            customer.Id,
            new List<QuoteItem> { new() { PrezzoUnitario = 100m, Quantita = 2, ScontoPercentuale = 10m } }
        );

        // 100×2 = 200 → −10% = 180 → ×1.22 = 219,60
        Assert.Equal(219.60m, quote.Totale);
    }

    /// <summary>
    /// Sconto di riga + sconto totale combinati.
    /// 2 pz × 100 € − 10% riga = 180 € − 5% totale = 171 € → IVA 22% = 208,62 €
    /// </summary>
    [Fact]
    public async Task CreaPreventivo_ScontoRigaEScontoTotale()
    {
        var db = CreateDb();
        var customer = new Customer { RagioneSociale = "GasTech SpA", PartitaIVA = "09876543210", Email = "ordini@gastech.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var quote = await new QuoteService(db).CreaPreventivo(
            customer.Id,
            new List<QuoteItem> { new() { PrezzoUnitario = 100m, Quantita = 2, ScontoPercentuale = 10m } },
            scontoTotale: 5m
        );

        // 180 − 5% = 171 → ×1.22 = 208,62
        Assert.Equal(208.62m, quote.Totale);
    }
}
