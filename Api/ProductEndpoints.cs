using CavagnaDemo.Models;
using CavagnaDemo.Services;

namespace CavagnaDemo.Api;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", async (ProductService svc) =>
        {
            var products = await svc.GetAllProducts();
            return Results.Ok(products);
        });

        group.MapGet("/{id:int}", (int id, ProductService svc) =>
        {
            var p = svc.RetrieveById(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        group.MapGet("/active", async (ProductService svc) =>
        {
            var list = await svc.LoadActiveProducts();
            return Results.Ok(list);
        });

        group.MapGet("/search", async (string q, ProductService svc) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Il parametro q è obbligatorio e non può essere vuoto.");

            var results = await svc.SearchByName(q);
            return Results.Ok(results);
        });

        group.MapGet("/low-stock", async (int? threshold, ProductService svc) =>
        {
            if (threshold is null or <= 0)
                return Results.BadRequest("Il parametro threshold deve essere un intero positivo.");
            if (threshold > 999)
                return Results.BadRequest("Il parametro threshold non può superare 999.");

            var products = await svc.GetProductsBelowStock(threshold.Value);
            return Results.Ok(products);
        });

        group.MapPost("/", async (Product product, ProductService svc) =>
        {
            var created = await svc.CreateProduct(product);
            return Results.Created($"/api/products/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, Product product, ProductService svc) =>
        {
            product.Id = id;
            var ok = await svc.UpdateProduct(product);
            return ok ? Results.NoContent() : Results.BadRequest();
        });

        group.MapDelete("/{id:int}", async (int id, ProductService svc) =>
        {
            var ok = await svc.DeleteProduct(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        // Endpoint speciale che calcola il prezzo IVA inclusa
        group.MapGet("/{id:int}/price-with-vat", (int id, ProductService svc) =>
        {
            var p = svc.RetrieveById(id);
            if (p is null) return Results.NotFound();
            var prezzoIva = PricingHelper.AddVat((decimal)p.Prezzo);
            return Results.Ok(new { p.Id, p.Codice, p.Nome, Prezzo = p.Prezzo, PrezzoIvaInclusa = prezzoIva });
        });
    }
}
