# Spec: Endpoint ricerca prodotti sotto soglia stock

## Contesto

Il gestionale non espone alcuna API per individuare i prodotti con scorte insufficienti.
L'obiettivo è aggiungere un endpoint Minimal API che, dato un valore soglia, restituisca
tutti i prodotti il cui `Stock` è strettamente inferiore a quel valore — utile per
riordini o alert operativi.

---

## Obiettivo

`GET /api/products/low-stock?threshold={n}` → lista di `Product` con `Stock < threshold`.

---

## File da modificare

| File | Modifica |
|---|---|
| `Services/ProductService.cs` | Aggiungere metodo `GetProductsBelowStock(int threshold)` |
| `Api/ProductEndpoints.cs` | Aggiungere endpoint GET `/low-stock` nel gruppo esistente |

Nessun altro file va toccato (in particolare `Program.cs`, `CavagnaDbContext.cs`, `.csproj`).

---

## Comportamento atteso

### Service — `ProductService.GetProductsBelowStock`

```
Task<List<Product>> GetProductsBelowStock(int threshold)
```

- Query: `WHERE Stock < threshold`
- Include la navigation property `Category` (coerente con `GetAllProducts`)
- Ordine: per `Stock` crescente (i più critici prima)
- Filtra solo prodotti con `Attivo == true` — i prodotti disattivati non compaiono nel risultato

### Endpoint GET `/api/products/low-stock`

- **Query param:** `threshold` (int, obbligatorio)
- **Validazione — regole in ordine:**
  | Condizione | Risposta |
  |---|---|
  | Parametro assente o non parsabile come int | `400` — `"Il parametro threshold deve essere un intero positivo."` |
  | `threshold ≤ 0` | `400` — `"Il parametro threshold deve essere un intero positivo."` |
  | `threshold > 999` | `400` — `"Il parametro threshold non può superare 999."` |
  | `1 ≤ threshold ≤ 999` | elabora la query e restituisce `200` |
- **Risposta 200:** array JSON di prodotti (stesso shape usato dagli altri GET)
- **Risposta 200 con lista vuota:** `[]` — non è un errore
- **Tag Swagger:** `"Products"` (gruppo già esistente)

---

## Esempio di chiamata

```
GET /api/products/low-stock?threshold=10
```

```json
[
  { "id": 3, "codice": "RG-200", "nome": "Regolatore DN20", "stock": 2, ... },
  { "id": 7, "codice": "VL-050", "nome": "Valvola manuale 1/2\"", "stock": 8, ... }
]
```

---

## Vincoli e convenzioni da rispettare

- Metodo service **async** (`Task<List<Product>>`) con `await` — no `.Result`/`.Wait()`
- Nome metodo service in **inglese** (infrastruttura tecnica) — `GetProductsBelowStock`
- `decimal` non necessario qui (Stock è `int`)
- File-scoped namespace già presente nei file — non alterarlo
- Non registrare nulla di nuovo nel container DI (il `ProductService` è già Scoped)
- Non aggiungere pacchetti NuGet

---

## Verifica

1. `dotnet build` — zero warning/errori
2. `dotnet run` → aprire `http://localhost:5000/swagger`
3. Eseguire `GET /api/products/low-stock?threshold=50` → lista non vuota (seed ha prodotti con stock < 50)
4. Eseguire `GET /api/products/low-stock?threshold=0` → `400 Bad Request`
5. Eseguire `GET /api/products/low-stock?threshold=1` → `[]` o lista con soli prodotti a stock 0
6. Eseguire `GET /api/products/low-stock` (senza param) → `400 Bad Request`
7. Eseguire `GET /api/products/low-stock?threshold=1000` → `400 Bad Request` ("non può superare 999")
8. Eseguire `GET /api/products/low-stock?threshold=-5` → `400 Bad Request`
