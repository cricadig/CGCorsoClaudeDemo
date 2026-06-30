using CavagnaDemo.Models;
using CavagnaDemo.Services;

namespace CavagnaDemo.Api;

public record CreateOrderRequest(int CustomerId, List<CreateOrderLine> Items, string? ShippingAddress);
public record CreateOrderLine(int ProductId, int Quantity, decimal? Discount);

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", async (OrderService svc) => Results.Ok(await svc.GetAllOrders()));

        group.MapGet("/{id:int}", async (int id, OrderService svc) =>
        {
            var o = await svc.GetById(id);
            return o is null ? Results.NotFound() : Results.Ok(o);
        });

        group.MapPost("/", async (CreateOrderRequest req, OrderService svc, ProductService productSvc) =>
        {
            var items = new List<OrderItem>();
            foreach (var line in req.Items)
            {
                var p = productSvc.RetrieveById(line.ProductId);
                if (p == null) return Results.BadRequest($"Prodotto {line.ProductId} non trovato");
                items.Add(new OrderItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = (decimal)p.Prezzo,
                    Discount = line.Discount ?? 0
                });
            }
            var order = await svc.CreateOrder(req.CustomerId, items, req.ShippingAddress);
            return Results.Created($"/api/orders/{order.Id}", order);
        });

        group.MapPost("/{id:int}/cancel", async (int id, OrderService svc) =>
        {
            var ok = await svc.CancelOrder(id);
            return ok ? Results.Ok() : Results.NotFound();
        });

        group.MapPost("/{id:int}/ship", async (int id, OrderService svc) =>
        {
            var ok = await svc.MarkAsShipped(id);
            return ok ? Results.Ok() : Results.NotFound();
        });
    }
}
