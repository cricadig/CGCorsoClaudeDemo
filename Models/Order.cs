namespace CavagnaDemo.Models;

public class Order
{
    public int Id { get; set; }

    public string NumeroOrdine { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public DateTime DataOrdine { get; set; } = DateTime.Now;

    public DateTime? DataSpedizione { get; set; }

    public string Stato { get; set; } = "Confermato"; // Confermato, InPreparazione, Spedito, Consegnato, Annullato

    public decimal Totale { get; set; }

    public string? IndirizzoSpedizione { get; set; }

    public string? Note { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
