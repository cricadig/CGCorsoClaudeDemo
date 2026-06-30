namespace CavagnaDemo.Models.ViewModels;

public class ProductListViewModel
{
    public string? Search { get; set; }
    public List<ProductListItem> Items { get; set; } = new();
}

public class ProductListItem
{
    public int Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public float Prezzo { get; set; }
    public float PrezzoIvaInclusa { get; set; }
    public int Stock { get; set; }
    public bool Attivo { get; set; }
}
