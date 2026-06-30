namespace CavagnaDemo.Models;

public class QuoteItem
{
    public int Id { get; set; }

    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantita { get; set; }

    public decimal PrezzoUnitario { get; set; }

    // Sconto in percentuale (0-100)
    public decimal ScontoPercentuale { get; set; }

    public decimal Subtotale { get; set; }
}
