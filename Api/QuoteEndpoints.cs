using CavagnaDemo.Models;
using CavagnaDemo.Services;

namespace CavagnaDemo.Api;

public record CreateQuoteRequest(int CustomerId, List<CreateQuoteLine> Righe);
public record CreateQuoteLine(int ProductId, int Quantita, decimal? ScontoPercentuale);

public static class QuoteEndpoints
{
    public static void MapQuoteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/quotes").WithTags("Quotes");

        group.MapGet("/", async (QuoteService svc) =>
        {
            return Results.Ok(await svc.GetAll());
        });

        group.MapGet("/{id:int}", async (int id, QuoteService svc) =>
        {
            var q = await svc.OttieniPreventivo(id);
            return q is null ? Results.NotFound() : Results.Ok(q);
        });

        group.MapPost("/", async (CreateQuoteRequest req, QuoteService svc, ProductService productSvc) =>
        {
            // Convertiamo request in QuoteItem
            var righe = new List<QuoteItem>();
            foreach (var r in req.Righe)
            {
                var product = productSvc.RetrieveById(r.ProductId);
                if (product is null)
                    return Results.BadRequest($"Prodotto {r.ProductId} non trovato.");

                righe.Add(new QuoteItem
                {
                    ProductId = r.ProductId,
                    Quantita = r.Quantita,
                    PrezzoUnitario = (decimal)product.Prezzo,
                    ScontoPercentuale = r.ScontoPercentuale ?? 0
                });
            }

            var quote = await svc.CreaPreventivo(req.CustomerId, righe);
            return Results.Created($"/api/quotes/{quote.Id}", quote);
        });

        group.MapPatch("/{id:int}/stato", async (int id, string nuovoStato, QuoteService svc) =>
        {
            var ok = await svc.AggiornaStato(id, nuovoStato);
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}
