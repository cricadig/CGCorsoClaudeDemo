---
name: scaffold-endpoint
description: >
  Scaffolds a new Minimal API endpoint file for CavagnaDemo, following the established
  project pattern (MapGroup, WithTags, Results.*), and registers it in Program.cs.
  Use this skill whenever the user asks to add, create, or scaffold a new API endpoint,
  a new route, a new Minimal API, or a new resource under /api/ — even if they do not
  use the word "endpoint" explicitly. Trigger on phrases like "voglio un endpoint per X",
  "aggiungi una rotta per Y", "crea le API per Z", "mi serve un endpoint che...",
  "add an endpoint for", "new api route", "scaffold a controller", or any description
  of a new resource that needs HTTP operations. When in doubt, invoke this skill.
---

# scaffold-endpoint

Genera un nuovo file `Api/XxxEndpoints.cs` nel formato del progetto e, con conferma
esplicita, aggiunge la registrazione in `Program.cs`.

---

## Convenzioni obbligatorie (da CLAUDE.md)

Prima di scrivere qualsiasi codice, applica queste regole senza eccezioni:

| Regola | Dettaglio |
|--------|-----------|
| **Lingua mista** | Nomi di dominio in italiano (`Righe`, `Totale`, `Codice`), infrastruttura in inglese (`MapGet`, `GetAllProducts`, `WithTags`) |
| **File-scoped namespace** | `namespace CavagnaDemo.Api;` — mai il blocco con `{}` |
| **Service layer** | Gli endpoint non accedono mai a `CavagnaDbContext` direttamente — sempre attraverso un `XxxService` |
| **Async** | Usa `async` + `await` su ogni lambda che fa I/O; mai `.Result` o `.Wait()` |
| **Nullable** | `string` required → `= string.Empty`; usa `?` per i nullable |
| **PricingHelper** | Per calcoli IVA usa `PricingHelper.AddVat(decimal)` o `PricingHelper.CalcolaConIVA(float)` — mai `* 1.22` hardcoded |
| **Program.cs** | NON aggiungere `app.MapXxxEndpoints()` senza conferma esplicita dell'utente |

---

## Step 1 — Capisci la richiesta

Prima di scrivere codice, ricava dal contesto (o chiedi) queste informazioni:

1. **Nome della risorsa** — singolare, PascalCase (es. `Category`, `Warehouse`, `Supplier`)
2. **Route prefix** — default: `/api/{nome-risorsa-plurale-lowercase}` (es. `/api/categories`)
3. **Operazioni richieste** — GET all, GET by id, POST, PUT, DELETE, o custom
4. **Esiste già un Service?** — controlla `Services/` — se non esiste, segnalalo dopo aver generato il file endpoint

---

## Step 2 — Leggi il pattern di riferimento

Leggi `Api/ProductEndpoints.cs` per confrontare il pattern esatto prima di generare.
Nota in particolare:

- Come si concatenano `MapGroup(...)` + `WithTags(...)`
- Come si usano `Results.Ok`, `Results.NotFound`, `Results.Created`, `Results.NoContent`, `Results.BadRequest`
- I constraint di route: `{id:int}`
- Come i service vengono iniettati come parametri della lambda (non come costruttore)

---

## Step 3 — Genera il file endpoint

Crea `Api/{ResourceName}Endpoints.cs`. Parti da questo template e adattalo:

```csharp
using CavagnaDemo.Models;
using CavagnaDemo.Services;

namespace CavagnaDemo.Api;

public static class {ResourceName}Endpoints
{
    public static void Map{ResourceName}Endpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/{route-prefix}").WithTags("{ResourceName}s");

        group.MapGet("/", async ({ResourceName}Service svc) =>
            Results.Ok(await svc.GetAll()));

        group.MapGet("/{id:int}", async (int id, {ResourceName}Service svc) =>
        {
            var item = await svc.GetById(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async ({ResourceName} item, {ResourceName}Service svc) =>
        {
            var created = await svc.Create(item);
            return Results.Created($"/api/{route-prefix}/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, {ResourceName} item, {ResourceName}Service svc) =>
        {
            item.Id = id;
            var ok = await svc.Update(item);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (int id, {ResourceName}Service svc) =>
        {
            var ok = await svc.Delete(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}
```

**Adatta il template:**
- Rimuovi le operazioni non richieste
- Aggiungi route custom se necessarie (es. `/{id}/activate`, `/search?q=`)
- Usa i nomi di metodo del Service esistente se disponibile — altrimenti usa nomi plausibili
  e lascia un commento `// TODO: implementare in {ResourceName}Service`
- Se la risorsa ha calcoli monetari, usa `PricingHelper.AddVat()` — mai `* 1.22`

**Esempi di route custom comuni:**

```csharp
// Ricerca per nome
group.MapGet("/search", async (string q, {ResourceName}Service svc) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest("Il parametro q non può essere vuoto.");
    return Results.Ok(await svc.SearchByName(q));
});

// Azione di stato (es. attiva/disattiva)
group.MapPost("/{id:int}/activate", async (int id, {ResourceName}Service svc) =>
{
    var ok = await svc.Activate(id);
    return ok ? Results.Ok() : Results.NotFound();
});
```

---

## Step 4 — Conferma prima di toccare Program.cs

CLAUDE.md vieta esplicitamente di aggiungere `app.MapXxxEndpoints()` senza conferma.

1. Mostra all'utente la riga esatta da aggiungere:
   ```csharp
   app.Map{ResourceName}Endpoints();
   ```
   Specifica che va inserita dopo `app.MapOrderEndpoints();` nella sezione "Minimal API".

2. Chiedi conferma esplicita: **"Vuoi che aggiunga anche la registrazione in `Program.cs`?"**

3. Solo dopo conferma: aggiungi la riga in `Program.cs` nella posizione corretta.

---

## Step 5 — Segnala il Service mancante (se necessario)

Se `Services/{ResourceName}Service.cs` non esiste, avvisa l'utente con un messaggio chiaro:

> Il file endpoint è pronto, ma `{ResourceName}Service` non esiste ancora.
> Vuoi che lo crei? Servirà anche registrarlo con `builder.Services.AddScoped<{ResourceName}Service>();`
> in `Program.cs` — anche questa modifica richiede conferma esplicita (CLAUDE.md).

Non creare il Service automaticamente. L'endpoint può essere scritto subito; il Service è un secondo passo.

---

## Non fare mai

- Accedere a `CavagnaDbContext` direttamente negli endpoint
- Usare `.Result` o `.Wait()` nelle lambda async
- Scrivere `* 1.22` o `* 1.20` — usa sempre `PricingHelper`
- Modificare `Program.cs` senza conferma esplicita dell'utente
- Aggiungere `using` già coperti dai global usings del progetto (`System`, `Microsoft.AspNetCore.Builder`, ecc.)
- Usare il blocco namespace `namespace X { ... }` — solo file-scoped
