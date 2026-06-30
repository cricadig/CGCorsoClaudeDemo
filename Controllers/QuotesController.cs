using CavagnaDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CavagnaDemo.Controllers;

public class QuotesController : Controller
{
    private readonly QuoteService _service;

    public QuotesController(QuoteService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var quotes = await _service.GetAll();
        return View(quotes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var q = await _service.OttieniPreventivo(id);
        if (q == null) return NotFound();
        return View(q);
    }
}
