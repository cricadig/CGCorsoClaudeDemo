# Spec: Fix SearchByName — filtrare prodotti inattivi

## Obiettivo

`SearchByName` restituisce anche prodotti con `Attivo = false`, esponendo all'utente articoli fuori catalogo. Tutti gli altri metodi di lettura che operano su prodotti visibili (`LoadActiveProducts`, `GetProductsBelowStock`) filtrano già su `Attivo == true`.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Services/ProductService.cs` | `SearchByName` (riga 79): aggiungere `p.Attivo &&` nella condizione `Where` |

---

## Impatti sui chiamanti

| Chiamante | Prima | Dopo |
|---|---|---|
| `GET /api/products/search?q=...` | restituisce attivi e inattivi | solo prodotti attivi |
| `Controllers/ProductsController` — `Index` (ricerca) | mostra attivi e inattivi nei risultati | solo prodotti attivi |

---

## Comportamento atteso

**Prima (attuale):**
- `Where(p => p.Nome.Contains(query))` — nessun filtro su `Attivo`.
- Una ricerca per nome può restituire prodotti disattivati.

**Dopo:**
1. Condizione aggiornata: `Where(p => p.Attivo && p.Nome.Contains(query))`.
2. La ricerca restituisce solo prodotti attivi, coerentemente con `LoadActiveProducts` e `GetProductsBelowStock`.

---

## Vincoli

- Non modificare `Program.cs`.
- Se in futuro serve una ricerca admin che includa i prodotti inattivi, va creato un metodo separato (es. `SearchByNameAdmin`) — non aggiungere un parametro booleano a questo metodo.
- Pattern di riferimento: `LoadActiveProducts` (`.Where(p => p.Attivo)`) e `GetProductsBelowStock` (`.Where(p => p.Attivo && p.Stock < threshold)`).

---

## Verifica

1. `dotnet build` — 0 errori.
2. Impostare un prodotto come `Attivo = false` nel DB (tramite Swagger o direttamente).
3. `GET /api/products/search?q=<nome del prodotto inattivo>` → lista vuota o senza il prodotto inattivo.
4. `GET /api/products/search?q=<nome prodotto attivo>` → risultati corretti.
5. UI: ricerca in `/Products` → non mostra prodotti inattivi.
