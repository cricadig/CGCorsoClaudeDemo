# Spec: Fix RetrieveById — da sincrono ad async

## Obiettivo

Convertire `RetrieveById` da metodo sincrono ad async, eliminando il blocco del thread su I/O DB. Documentato come BUG-3 in `BUGS.md`. Il metodo ha 5 chiamanti in 4 file diversi, tutti da aggiornare.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Services/ProductService.cs` | Rinomina in `RetrieveByIdAsync`, firma diventa `async Task<Product?>`, usa `FirstOrDefaultAsync` |
| `Api/ProductEndpoints.cs` | 3 chiamate: lambda `/{id:int}` (riga 20), lambda `/{id:int}/price-with-vat` (riga 69) → aggiungere `async` + `await` |
| `Api/OrderEndpoints.cs` | 1 chiamata: `POST /` (riga 28), già in lambda async → aggiungere `await` |
| `Api/QuoteEndpoints.cs` | 1 chiamata: `POST /` (riga 32), già in lambda async → aggiungere `await` |
| `Controllers/ProductsController.cs` | `Details` (riga 42): `IActionResult` → `async Task<IActionResult>`, aggiungere `await` |

---

## Impatti sui chiamanti

| Chiamante | Prima | Dopo |
|---|---|---|
| `ProductEndpoints` — `GET /{id:int}` | lambda sincrona | lambda `async` con `await` |
| `ProductEndpoints` — `GET /{id}/price-with-vat` | lambda sincrona | lambda `async` con `await` |
| `OrderEndpoints` — `POST /` | chiamata sincrona nel `foreach` | `await` nel `foreach` (lambda già `async`) |
| `QuoteEndpoints` — `POST /` | chiamata sincrona nel `foreach` | `await` nel `foreach` (lambda già `async`) |
| `ProductsController.Details` | `IActionResult` sincrono | `Task<IActionResult>` async |

---

## Comportamento atteso

**Prima (attuale):**
- `RetrieveById` usa `FirstOrDefault` sincrono, bloccando il thread mentre attende la query DB.
- I chiamanti nei lambda Minimal API e nel controller non possono awaittare la chiamata.

**Dopo:**
1. Il metodo è rinominato `RetrieveByIdAsync` (convenzione .NET per metodi async).
2. Usa `FirstOrDefaultAsync` — non blocca il thread.
3. Tutti i chiamanti aggiungono `await` e, dove necessario, diventano `async`.
4. Il comportamento funzionale (restituisce `Product?`, `null` se non trovato) rimane invariato.

---

## Vincoli

- Rinominare in `RetrieveByIdAsync` — la convenzione del progetto usa il suffisso `Async` per i metodi asincroni (es. `FindAsync`, `SaveChangesAsync`).
- Non modificare `Program.cs`.
- Pattern di riferimento per la struttura async: `GetAllProducts` (`Services/ProductService.cs:16`) e `DeleteProduct` (`Services/ProductService.cs:67`).
- **Attenzione:** `QuoteEndpoints.cs:37` accede a `product.Prezzo` senza null-check — se il prodotto non esiste la riga lancia `NullReferenceException`. Questo è un bug separato; non correggerlo in questo spec ma segnalarlo al team.

---

## Verifica

1. `dotnet build` — 0 errori.
2. `GET /api/products/1` — restituisce il prodotto correttamente.
3. `GET /api/products/9999` — restituisce `404 Not Found`.
4. `GET /api/products/1/price-with-vat` — restituisce il prodotto con prezzo IVA inclusa.
5. `POST /api/orders` con prodotto valido — crea l'ordine correttamente.
6. `GET /Products/Details/1` (UI MVC) — mostra i dettagli del prodotto.
