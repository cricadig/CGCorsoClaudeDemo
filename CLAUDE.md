# CLAUDE.md — CavagnaDemo

---

## Panoramica

Gestionale web per il settore **gas industriale** (regolatori, valvole, raccordi, bombole).
Gestisce catalogo prodotti, anagrafica clienti, preventivi e ordini.

| | |
|---|---|
| Framework | ASP.NET Core 8.0 — ibrido MVC + Minimal APIs |
| ORM / DB | Entity Framework Core 8.0 + SQLite (`cavagna.db`) |
| Docs API | Swagger/Swashbuckle 6.5 — solo in Development (`/swagger`) |
| UI | Razor Views |

Al primo avvio, il DB viene creato automaticamente (`EnsureCreated`) e popolato con seed (4 categorie, 13 prodotti, 3 clienti).

---

## Architettura

```
CavagnaDemo/
├── Api/              # Minimal API — extension methods su WebApplication
│   ├── ProductEndpoints.cs   → /api/products
│   ├── QuoteEndpoints.cs     → /api/quotes
│   └── OrderEndpoints.cs     → /api/orders
├── Controllers/      # MVC Controllers — usano solo i Service
├── Models/           # Entità EF Core con DataAnnotations
│   └── ViewModels/   # ViewModel usati solo dai Controller MVC
├── Services/         # Logica di business — registrati come Scoped
│   └── PricingHelper.cs  # Classe static — NON registrare nel container
├── Data/
│   ├── CavagnaDbContext.cs
│   └── SeedData.cs
├── Views/            # Razor Views (.cshtml)
└── Program.cs        # Entry point — configurazione DI e middleware
```

**Pattern da rispettare:**
- I Controller non accedono mai al `DbContext` direttamente — passano sempre per un Service.
- Le Minimal API in `Api/` si registrano in `Program.cs` tramite `app.MapXxxEndpoints()`.
- Usa `AddScoped<T>()` per ogni nuovo Service in `Program.cs`.
- `PricingHelper` è `static` — invocalo direttamente, non iniettarlo.

**Routing:**
- UI: `/{controller=Home}/{action=Index}/{id?}`
- API: `/api/products`, `/api/quotes`, `/api/orders`

---

## Convenzioni di naming e stile

### Lingua mista — regola del team

Usa **italiano per il dominio di business**, **inglese per l'infrastruttura tecnica**.
Non uniformare senza discussione con il team.

| Contesto | Lingua | Esempi |
|---|---|---|
| Proprietà di dominio sui modelli | Italiano | `RagioneSociale`, `PartitaIVA`, `Codice`, `Attivo`, `Righe` |
| Metodi di servizio di dominio | Italiano | `CreaPreventivo`, `OttieniPreventivo`, `AggiornaStato` |
| Metodi tecnici / infrastrutturali | Inglese | `GetAllProducts`, `LoadActiveProducts`, `CheckStockForOrder` |
| Classi, namespace, cartelle | Inglese | `ProductService`, `CavagnaDbContext`, `SeedData` |

### Stile C\#

- Usa **file-scoped namespace**: `namespace CavagnaDemo.Services;` (non il blocco con `{}`)
- `ImplicitUsings` è abilitato — non aggiungere `using` già coperti dai global usings
- `Nullable` è abilitato — usa `?` per i nullable, inizializza le stringhe required con `= string.Empty`
- Applica `[Required]` e `[EmailAddress]` da `DataAnnotations` sui modelli — non introdurre FluentValidation
- Scrivi metodi async con `Task<T>` e `await` — non usare `.Result` o `.Wait()`
- Per nuovi calcoli monetari usa **`decimal`**, non `float`

---

## Comandi utili

```bash
# Ripristina pacchetti NuGet
dotnet restore

# Compila
dotnet build

# Avvia in Development (Swagger attivo)
dotnet run

# Avvia con hot reload
dotnet watch run

# Pulisci bin/ e obj/
dotnet clean

# Elenca pacchetti con aggiornamenti disponibili
dotnet list package --outdated

# Aggiorna un pacchetto
dotnet add package <NomePacchetto> --version <versione>
```

**URL di default:** `http://localhost:5000` — Swagger: `http://localhost:5000/swagger`

> Il progetto usa `EnsureCreated()` — **non è compatibile con le migration EF**.
> Se introduci `dotnet ef migrations add`, rimuovi prima `EnsureCreated()` da `Program.cs`
> e allineati con il team.

---

## Non fare senza conferma esplicita

### `Program.cs`
- Non aggiungere, rimuovere o cambiare il lifetime dei Service nel container DI
- Non modificare la sequenza dei middleware
- Non aggiungere middleware nuovi (auth, CORS, rate limiting, ecc.)
- Non cambiare la connection string o il provider del database
- Non toccare `EnsureCreated()` o `SeedData.Initialize()`
- Non aggiungere o rimuovere `app.MapXxxEndpoints()`

### `CavagnaDemo.csproj`
- Non aggiungere, rimuovere o aggiornare pacchetti NuGet
- Non modificare `TargetFramework`
- Non cambiare `Nullable` o `ImplicitUsings`

### `Data/CavagnaDbContext.cs`
- Non modificare lo schema (DbSet, relazioni, configurazioni Fluent API)

### Database (`cavagna.db`)
- Non eliminare il file `cavagna.db`
- Non eseguire `dotnet ef database drop` o equivalenti

### `Data/SeedData.cs`
- Non modificare né eliminare i dati di seed — sono usati per test manuali e sviluppo
