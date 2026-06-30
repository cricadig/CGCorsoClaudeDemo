# Spec: Fix LoadActiveProducts e SearchByName — Include(Category) mancante

## Obiettivo

Aggiungere `.Include(p => p.Category)` a `LoadActiveProducts` e `SearchByName` in `ProductService`. Entrambi i metodi restituiscono oggetti `Product` con `Category = null`, mentre tutti gli altri metodi di lettura del service includono sempre la navigazione.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Services/ProductService.cs` | `LoadActiveProducts` (riga 28): aggiungere `.Include(p => p.Category)` |
| `Services/ProductService.cs` | `SearchByName` (riga 78): aggiungere `.Include(p => p.Category)` |

---

## Impatti sui chiamanti

| Chiamante | Prima | Dopo |
|---|---|---|
| `GET /api/products/active` | `Category = null` in ogni prodotto | `Category` popolata |
| `GET /api/products/search?q=...` | `Category = null` in ogni prodotto | `Category` popolata |
| `Controllers/ProductsController` — `Index` (search) | `p.Category?.Name` restituisce sempre `"—"` (fallback) | nome categoria reale |
| `Controllers/ProductsController` — `Index` (lista completa) | usa `GetAllProducts` — già corretto | invariato |

---

## Comportamento atteso

**Prima (attuale):**
- `LoadActiveProducts` e `SearchByName` non chiamano `.Include(p => p.Category)`.
- La proprietà `Category` è `null` su tutti i prodotti restituiti.
- Il ViewModel del controller usa già `p.Category?.Name ?? "—"` come fallback difensivo, quindi non si rompe ma mostra `"—"` come categoria per tutti i prodotti nelle ricerche e nella lista attivi.

**Dopo:**
1. Entrambi i metodi aggiungono `.Include(p => p.Category)` prima del `.Where`.
2. Gli endpoint `/active` e `/search` restituiscono prodotti con la categoria popolata.
3. Il controller mostra il nome categoria reale nella lista prodotti (ricerca e vista prodotti attivi).

---

## Vincoli

- Non modificare `Program.cs`.
- Pattern di riferimento: `GetAllProducts` (`Services/ProductService.cs:18`) e `GetProductsBelowStock` (`Services/ProductService.cs:85`), che usano già `.Include(p => p.Category)`.

---

## Verifica

1. `dotnet build` — 0 errori.
2. `GET /api/products/active` — ogni prodotto nella risposta ha `category` popolata (non null).
3. `GET /api/products/search?q=riduttore` — ogni prodotto ha `category` popolata.
4. UI: `/Products` con ricerca → la colonna Categoria mostra il nome reale invece di `"—"`.
