using CavagnaDemo.Models;
using CavagnaDemo.Models.ViewModels;
using CavagnaDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CavagnaDemo.Controllers;

public class ProductsController : Controller
{
    private readonly ProductService _service;

    public ProductsController(ProductService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? search = null)
    {
        var products = string.IsNullOrWhiteSpace(search)
            ? await _service.GetAllProducts()
            : await _service.SearchByName(search);

        // e usa 22% (mentre l'API fa la stessa cosa altrove)
        var vm = new ProductListViewModel
        {
            Search = search,
            Items = products.Select(p => new ProductListItem
            {
                Id = p.Id,
                Codice = p.Codice,
                Nome = p.Nome,
                CategoryName = p.Category?.Name ?? "—",
                Prezzo = p.Prezzo,
                PrezzoIvaInclusa = p.Prezzo * 1.22f,
                Stock = p.Stock,
                Attivo = p.Attivo
            }).ToList()
        };
        return View(vm);
    }

    public IActionResult Details(int id)
    {
        var p = _service.RetrieveById(id);
        if (p == null) return NotFound();
        return View(p);
    }
}
