using CavagnaDemo.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace CavagnaDemo.Tests;

/// <summary>
/// BUG-04: GET /api/products/search non valida il parametro q.
/// - q assente  → crasha (500) invece di 400 Bad Request
/// - q vuota    → restituisce l'intero catalogo invece di 400 Bad Request
///
/// I test FALLIRANNO finché il bug non viene corretto.
/// </summary>
public class ProductSearchValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductSearchValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Sostituisce SQLite con InMemory per i test
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CavagnaDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<CavagnaDbContext>(opt =>
                    opt.UseInMemoryDatabase("test-search-" + Guid.NewGuid()));
            });
        }).CreateClient();
    }

    /// <summary>
    /// Parametro q assente: deve restituire 400 Bad Request.
    /// Attualmente il framework non trova il parametro obbligatorio e restituisce 400
    /// solo se il parametro è dichiarato required — ma con q stringa non nullable
    /// il comportamento reale è un crash o una risposta inattesa.
    /// </summary>
    [Fact]
    public async Task Search_SenzaParametroQ_Restituisce400()
    {
        var response = await _client.GetAsync("/api/products/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Parametro q vuoto: deve restituire 400 Bad Request.
    /// Attualmente restituisce 200 con l'intero catalogo (anche vuoto in questo caso).
    /// </summary>
    [Fact]
    public async Task Search_ConQVuota_Restituisce400()
    {
        var response = await _client.GetAsync("/api/products/search?q=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
