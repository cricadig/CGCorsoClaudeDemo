# CLAUDE.md — CavagnaDemo

Guida di riferimento per lavorare su questo progetto con Claude Code.

---

## Panoramica del progetto

Applicazione web gestionale per il settore **gas industriale** (regolatori, valvole, raccordi, bombole).
Gestisce catalogo prodotti, anagrafica clienti, preventivi e ordini.

**Stack:**
- ASP.NET Core 8.0 — architettura ibrida MVC + Minimal APIs
- Entity Framework Core 8.0 con SQLite (`cavagna.db`)
- Swagger/OpenAPI (Swashbuckle 6.5) — solo in Development
- Razor Views per l'interfaccia web

Il database viene creato automaticamente al primo avvio (`EnsureCreated`) e popolato con dati di seed (4 categorie, 13 prodotti, 3 clienti).

---

## Architettura

```
CavagnaDemo/
├── Api/                  # Minimal API endpoints (estensioni su WebApplication)
│   ├── ProductEndpoints.cs   → /api/products
│   ├── QuoteEndpoints.cs     → /api/quotes
│   └── OrderEndpoints.cs     → /api/orders
├── Controllers/          # MVC Controllers (Razor Views)
│   ├── HomeController.cs
│   ├── ProductsController.cs
│   ├── QuotesController.cs
│   └── CustomersController.cs
├── Models/               # Entità EF Core + DataAnnotations
│   └── ViewModels/       # ViewModel usati solo dai Controller MVC
├── Services/             # Logica di business, iniettata come Scoped
│   └── PricingHelper.cs  # Classe statica per calcoli IVA e sconti
├── Data/
│   ├── CavagnaDbContext.cs
│   └── SeedData.cs
├── Views/                # Razor Views (.cshtml)
├── Program.cs            # Entry point e configurazione DI
└── appsettings.json
```

**Pattern architetturali:**
- I **Controller MVC** dipendono dai Service tramite DI nel costruttore; non accedono mai al DbContext direttamente.
- Le **Minimal API** (`Api/*.cs`) sono definite come extension methods su `WebApplication` e si registrano in `Program.cs`.
- I **Service** sono registrati come `Scoped` in `Program.cs`.
- `PricingHelper` è una classe `static` — non va registrata nel container.

**Routing:**
- UI web: `/{controller=Home}/{action=Index}/{id?}`
- API REST: `/api/products`, `/api/quotes`, `/api/orders`
- Swagger UI (Development): `/swagger`

---

## Convenzioni di naming e stile

### Lingua mista — regola del team
Il progetto usa deliberatamente **nomi italiani per il dominio di business** e **inglese per l'infrastruttura tecnica**. Non uniformare senza discussione.

| Contesto | Lingua | Esempi |
|---|---|---|
| Proprietà di dominio sui modelli | Italiano | `RagioneSociale`, `PartitaIVA`, `Codice`, `Nome`, `Attivo`, `Righe` |
| Metodi di servizio di dominio | Italiano | `CreaPreventivo`, `OttieniPreventivo`, `AggiornaStato` |
| Metodi tecnici/infrastrutturali | Inglese | `GetAllProducts`, `LoadActiveProducts`, `CheckStockForOrder` |
| Classi, namespace, cartelle | Inglese | `ProductService`, `CavagnaDbContext`, `SeedData` |

### Stile C#
- File-scoped namespace: `namespace CavagnaDemo.Services;` (non il blocco con `{}`)
- `ImplicitUsings` abilitato — non aggiungere `using` già coperti dai global
- `Nullable` abilitato — usare `?` per i nullable, `= string.Empty` per i required string
- Modelli con `[Required]` e `[EmailAddress]` da `DataAnnotations` — non usare FluentValidation senza allineamento del team
- I metodi async devono restituire `Task<T>` e usare `await` — non mischiare `.Result` o `.Wait()`

### Tipi numerici per i prezzi
`Product.Prezzo` è `float` (scelta legacy). `PricingHelper` espone sia `AddVat(decimal)` che `CalcolaConIVA(float)` per coprire entrambi i casi. Per nuovi calcoli monetari **preferire `decimal`**.

---

## Note e problemi noti nel codice

### Bug confermato — IVA inconsistente
`QuoteService.CreaPreventivo` usa **`* 1.20m` (20%)** mentre tutto il resto del progetto usa **22%**:
- `PricingHelper.AddVat` → `* 1.22m`
- `PricingHelper.CalcolaConIVA` → `* 1.22f`
- `QuoteService.CalculateTotal` → `* 1.22m`
- `ProductsController` → `* 1.22f`
- `ProductEndpoints` (price-with-vat) → `* 1.22f`

**Non correggere autonomamente** — verificare con il team se era intenzionale (es. IVA agevolata su certi prodotti).

### TODO aperti nel codice
- `Models/Product.cs:24` — aggiungere `DataUltimoAggiornamentoPrezzo`
- `Models/Customer.cs:25` — campo `Nazione` senza validazione, accetta qualunque stringa

### Arrotondamento prezzi
`PricingHelper.RoundPrice` usa `Math.Round` con la modalità default (BankersRounding), ma un commento nel codice avverte che **il sistema contabile si aspetta `MidpointRounding.AwayFromZero`**. Non è stato ancora corretto.

### Incoerenza async in ProductService
`RetrieveById` è sincrono (`FirstOrDefault`), tutti gli altri metodi sono async. Nei Controller MVC questo si nota perché `Details` non è `async` mentre `Index` lo è.

### Validazione Customer
Il modello `Customer` non ha validazione sul campo `Nazione`. Aggiungere validazione solo se esiste un ticket aperto — non farlo come miglioramento collaterale.

---

## Comandi utili

### Build e avvio
```bash
# Ripristina i pacchetti NuGet
dotnet restore

# Compila il progetto
dotnet build

# Avvia in modalità Development (con Swagger attivo)
dotnet run

# Avvia specificando l'environment
dotnet run --environment Development
dotnet run --environment Production

# Avvia con hot reload (utile durante lo sviluppo)
dotnet watch run
```

L'app è disponibile di default su `https://localhost:5001` e `http://localhost:5000`.
Swagger UI in Development: `http://localhost:5000/swagger`

### Database
```bash
# Il DB SQLite viene creato automaticamente al primo avvio in cavagna.db
# Per ricrearlo da zero, eliminare il file e riavviare:
Remove-Item cavagna.db   # PowerShell
# oppure
del cavagna.db           # cmd

# Aggiungere una migration EF (se si introduce schema change)
dotnet ef migrations add NomeMigration

# Applicare le migration al DB
dotnet ef database update
```

> Il progetto usa `EnsureCreated()` — non è compatibile con le migration EF. Se si introduce `dotnet ef migrations`, rimuovere `EnsureCreated()` da `Program.cs` e discuterlo con il team prima.

### Pulizia
```bash
# Elimina le cartelle bin/ e obj/
dotnet clean

# Pulizia completa manuale
Remove-Item -Recurse -Force bin, obj   # PowerShell
```

### Aggiornamento pacchetti NuGet
```bash
# Elenca i pacchetti con aggiornamenti disponibili
dotnet list package --outdated

# Aggiorna un pacchetto specifico
dotnet add package Microsoft.EntityFrameworkCore --version 8.x.x

# Dopo ogni aggiornamento verificare sempre che il progetto compili
dotnet build
```

### Test (se aggiunti in futuro)
```bash
dotnet test
```

---

## Cose da NON fare senza chiedere conferma

Le seguenti modifiche hanno impatto sull'intera applicazione e richiedono consenso esplicito prima di procedere.

### `Program.cs`
- Aggiungere o rimuovere registrazioni di servizi nel container DI
- Cambiare il lifetime dei servizi (`Scoped` → `Singleton` o `Transient`)
- Modificare la sequenza dei middleware (`UseStaticFiles`, `UseRouting`, ecc.)
- Aggiungere nuovi middleware (autenticazione, CORS, rate limiting, ecc.)
- Cambiare la connection string o il provider del database
- Rimuovere o spostare `EnsureCreated` / `SeedData.Initialize`
- Aggiungere o rimuovere blocchi `if (app.Environment.IsDevelopment())`
- Registrare nuovi endpoint group (`MapXxxEndpoints`)

### `CavagnaDemo.csproj`
- Aggiungere, rimuovere o aggiornare pacchetti NuGet
- Modificare il `TargetFramework`
- Cambiare le impostazioni `Nullable` o `ImplicitUsings`
- Aggiungere riferimenti a progetti esterni

### Generale
- Modificare `Data/CavagnaDbContext.cs` (schema del DB, relazioni, configurazioni Fluent API)
- Modificare `Data/SeedData.cs` (i dati di seed sono usati nei test manuali)
- Correggere il bug IVA 20%/22% in `QuoteService.CreaPreventivo` senza conferma esplicita
- Rinominare proprietà sui modelli (impatto su EF, view e API contemporaneamente)
