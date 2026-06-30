using CavagnaDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CavagnaDemo.Controllers;

public class CustomersController : Controller
{
    private readonly CustomerService _service;

    public CustomersController(CustomerService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _service.GetAllAsync();
        return View(customers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var c = await _service.GetByIdAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }
}
