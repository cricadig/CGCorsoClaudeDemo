# Spec: Fix SearchByName — validazione parametro q

## Obiettivo

Aggiungere validazione sul parametro `q` nell'endpoint `GET /api/products/search`. Senza validazione, una query vuota restituisce l'intero catalogo e una query null può causare `NullReferenceException`.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Api/ProductEndpoints.cs` | Endpoint `GET /search` (riga 30): aggiungere guard su `q` null o whitespace prima di chiamare il service |

---

## Impatti sui chiamanti

Il controller `ProductsController.Index` non è impattato — gestisce già il caso null/vuoto tramite `IsNullOrWhiteSpace(search)` alla riga 19, che in quel caso chiama `GetAllProducts` invece di `SearchByName`.

| Scenario | Prima | Dopo |
|---|---|---|
| `q` assente o vuota | restituisce tutti i prodotti (comportamento silenzioso) | `400 BadRequest` con messaggio descrittivo |
| `q` con soli spazi | restituisce tutti i prodotti | `400 BadRequest` con messaggio descrittivo |
| `q` con valore valido | risultati ricerca | risultati ricerca (invariato) |

---

## Comportamento atteso

**Prima (attuale):**
- Nessuna validazione su `q`.
- `q = ""` → `Nome.Contains("")` → vero per tutti i prodotti → restituisce l'intero catalogo.
- `q = null` → `NullReferenceException` in `Nome.Contains(null)`.

**Dopo:**
1. Se `q` è null, vuota o composta da soli spazi → restituire `400 BadRequest` con messaggio: `"Il parametro q è obbligatorio."`.
2. Se `q` è valida → chiamare `svc.SearchByName(q)` come prima.

---

## Vincoli

- La validazione va nell'endpoint, non nel service — il boundary di validazione è l'API.
- Pattern di riferimento: endpoint `GET /low-stock` in `ProductEndpoints.cs` che usa già `Results.BadRequest("messaggio")` per validare `threshold`.
- Non modificare `Program.cs`.

---

## Verifica

1. `dotnet build` — 0 errori.
2. `GET /api/products/search` (senza `q`) → `400 BadRequest`.
3. `GET /api/products/search?q=` (q vuota) → `400 BadRequest`.
4. `GET /api/products/search?q=riduttore` → lista prodotti filtrati.
