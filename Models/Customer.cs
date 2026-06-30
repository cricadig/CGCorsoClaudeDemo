using System.ComponentModel.DataAnnotations;

namespace CavagnaDemo.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    public string RagioneSociale { get; set; } = string.Empty;

    public string? PartitaIVA { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string? Indirizzo { get; set; }

    public string? Citta { get; set; }

    public string? CAP { get; set; }

    // FIXME: non c'è validazione, accetta qualunque cosa
    public string? Nazione { get; set; }

    public DateTime DataRegistrazione { get; set; } = DateTime.Now;

    public List<Quote> Quotes { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}
