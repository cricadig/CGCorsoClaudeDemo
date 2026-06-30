# CavagnaDemo

Gestionale web per il settore **gas industriale** (regolatori, valvole, raccordi, bombole).
Gestisce catalogo prodotti, anagrafica clienti, preventivi e ordini.

| | |
|---|---|
| Framework | ASP.NET Core 8.0 — ibrido MVC + Minimal API |
| ORM / DB | Entity Framework Core 8.0 + SQLite (`cavagna.db`) |
| Docs API | Swagger/Swashbuckle 6.5 — solo in Development |

---

## Avvio

```bash
dotnet restore
dotnet run
```

L'app si avvia su `http://localhost:5000`.
Swagger disponibile su `http://localhost:5000/swagger` (solo in ambiente Development).

Al primo avvio il DB viene creato automaticamente e popolato con dati di seed:
4 categorie, 13 prodotti, 3 clienti.

---

## Struttura cartelle

```
CavagnaDemo/
├── Api/              # Minimal API — extension methods su WebApplication
├── Controllers/      # MVC Controllers — usano solo i Service, mai il DbContext diretto
├── Models/           # Entità EF Core; Models/ViewModels/ per i ViewModel MVC
├── Services/         # Logica di business — registrati come Scoped in Program.cs
├── Data/             # CavagnaDbContext e SeedData
├── Views/            # Razor Views (.cshtml)
└── Program.cs        # Entry point — DI, middleware, registrazione endpoint
```

---

## Endpoint API (Minimal API)

### Prodotti — `/api/products`

| Metodo | Path | Descrizione |
|---|---|---|
| `GET` | `/api/products` | Tutti i prodotti con categoria |
| `GET` | `/api/products/{id}` | Singolo prodotto per ID |
| `GET` | `/api/products/active` | Solo prodotti con `Attivo = true` |
| `GET` | `/api/products/search?q=` | Ricerca per sottostringa sul nome |
| `GET` | `/api/products/low-stock?threshold=` | Prodotti attivi con stock sotto soglia, ordinati per stock crescente. `threshold` intero 1–999 |
| `GET` | `/api/products/{id}/price-with-vat` | Prezzo del prodotto con IVA al 22% |
| `POST` | `/api/products` | Crea prodotto — body: oggetto `Product` |
| `PUT` | `/api/products/{id}` | Aggiorna prodotto — sovrascrittura completa |
| `DELETE` | `/api/products/{id}` | Elimina prodotto fisicamente |

### Preventivi — `/api/quotes`

| Metodo | Path | Descrizione |
|---|---|---|
| `GET` | `/api/quotes` | Tutti i preventivi con cliente e righe |
| `GET` | `/api/quotes/{id}` | Singolo preventivo con cliente, righe e prodotti |
| `POST` | `/api/quotes` | Crea preventivo — body: `{ customerId, righe: [{ productId, quantita, scontoPercentuale? }] }` |
| `PATCH` | `/api/quotes/{id}/stato?nuovoStato=` | Aggiorna solo lo stato del preventivo |

### Ordini — `/api/orders`

| Metodo | Path | Descrizione |
|---|---|---|
| `GET` | `/api/orders` | Tutti gli ordini |
| `GET` | `/api/orders/{id}` | Singolo ordine per ID |
| `POST` | `/api/orders` | Crea ordine — body: `{ customerId, items: [{ productId, quantity, discount? }], shippingAddress? }` |
| `POST` | `/api/orders/{id}/cancel` | Annulla ordine |
| `POST` | `/api/orders/{id}/ship` | Segna ordine come spedito |

---

## Route MVC (UI Razor)

| Path | Controller / Action | Descrizione |
|---|---|---|
| `/` | `Home/Index` | Homepage |
| `/Products` | `Products/Index` | Lista prodotti; accetta `?search=` per filtrare per nome |
| `/Products/Details/{id}` | `Products/Details` | Dettaglio prodotto |
| `/Quotes` | `Quotes/Index` | Lista preventivi |
| `/Quotes/Details/{id}` | `Quotes/Details` | Dettaglio preventivo |
| `/Customers` | `Customers/Index` | Lista clienti |
| `/Customers/Details/{id}` | `Customers/Details` | Dettaglio cliente |

---

## Database SQLite

Il file `cavagna.db` viene creato nella directory di lavoro al primo avvio tramite `EnsureCreated()`.

- **Non è compatibile con le migration EF Core.** Se si introduce `dotnet ef migrations add`, occorre rimuovere prima `EnsureCreated()` da `Program.cs`.
- Per azzerare i dati è sufficiente eliminare `cavagna.db` e riavviare l'applicazione.
- La connection string è in `appsettings.json` sotto la chiave `ConnectionStrings:Default`.
