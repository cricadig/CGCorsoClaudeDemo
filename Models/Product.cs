using System.ComponentModel.DataAnnotations;

namespace CavagnaDemo.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    public string Codice { get; set; } = string.Empty;

    [Required]
    public string Nome { get; set; } = string.Empty;

    public string? Descrizione { get; set; }

    public float Prezzo { get; set; }

    public int Stock { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // TODO: aggiungere data ultimo aggiornamento prezzo
    public bool Attivo { get; set; } = true;
}
