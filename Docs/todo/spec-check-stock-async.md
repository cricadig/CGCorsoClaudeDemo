# Spec: Fix CheckStockForOrder — da sincrono ad async

## Obiettivo

Convertire `CheckStockForOrder` da metodo sincrono ad async, eliminando il blocco del thread su I/O DB. La modifica è isolata al solo service — il metodo non ha chiamanti attivi nel codebase.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Services/ProductService.cs` | Rinomina in `CheckStockForOrderAsync`, firma diventa `async Task<bool>`, usa `FindAsync` |

---

## Impatti sui chiamanti

Nessun chiamante attivo nel codebase — la modifica non richiede aggiornamenti su altri file.

| Scenario | Prima | Dopo |
|---|---|---|
| Chiamata al metodo | sincrona, blocca il thread | `await`-able, non blocca il thread |
| Comportamento funzionale | invariato | invariato |

---

## Comportamento atteso

**Prima (attuale):**
- `CheckStockForOrder` usa `_db.Products.Find(productId)` sincrono.
- Blocca il thread durante la query DB.

**Dopo:**
1. Metodo rinominato `CheckStockForOrderAsync`.
2. Usa `await _db.Products.FindAsync(productId)` — non blocca il thread.
3. Firma: `public async Task<bool> CheckStockForOrderAsync(int productId, int requestedQty)`.
4. La logica di confronto `product.Stock >= requestedQty` rimane invariata.

---

## Vincoli

- Rinominare in `CheckStockForOrderAsync` — convenzione .NET per metodi async.
- Non modificare `Program.cs`.
- Pattern di riferimento: `DeleteProduct` (`Services/ProductService.cs:67`) che usa già `FindAsync`.

---

## Verifica

1. `dotnet build` — 0 errori.
2. Nessun test funzionale automatico necessario (nessun endpoint lo chiama) — verificare solo che la build sia pulita.
