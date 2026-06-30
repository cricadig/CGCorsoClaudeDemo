namespace CavagnaDemo.Models;

public class Quote
{
    public int Id { get; set; }

    public string NumeroPreventivo { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime DataCreazione { get; set; } = DateTime.Now;

    public DateTime? DataScadenza { get; set; }

    public string Stato { get; set; } = "Bozza"; // Bozza, Inviato, Accettato, Rifiutato

    public decimal Totale { get; set; }

    public decimal ScontoTotale { get; set; }

    public string? Note { get; set; }

    public List<QuoteItem> Righe { get; set; } = new();
}
