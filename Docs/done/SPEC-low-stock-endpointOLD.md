# Spec: Endpoint ricerca prodotti sotto soglia stock

## Contesto

Il gestionale ha già un campo `Stock` (int) sul modello `Product` e un metodo
`CheckStockForOrder()` nel service per verificare la disponibilità prima di un ordine.
Manca però un endpoint per interrogare il catalogo e ottenere tutti i prodotti il
cui stock è inferiore a una soglia configurabile — utile per riordino e reportistica.

---

## Obiettivo

Aggiungere un endpoint `GET /api/products/low-stock?threshold={n}` che restituisce la
lista dei prodotti attivi con `Stock < threshold`, ordinati per stock crescente.

---

## File da modificare

| File | Modifica |
|---|---|
| `Services/ProductService.cs` | Aggiungere metodo `GetProductsBelowStock(int threshold)` |
| `Api/ProductEndpoints.cs` | Aggiungere handler per la nuova route |

**Nessuna modifica** a `Program.cs` (la registrazione `app.MapProductEndpoints()` è già presente),
né a `Models/Product.cs`, `Data/CavagnaDbContext.cs`, `Data/SeedData.cs`.

---

## Comportamento atteso

### Route
```
GET /api/products/low-stock?threshold={n}
```

### Parametro query string
| Parametro | Tipo | Obbligatorio | Default | Descrizione |
|---|---|---|---|---|
| `threshold` | int | **Sì** | — | Soglia esclusiva: restituisce prodotti con `Stock < threshold` |

### Filtri applicati
- Solo prodotti **attivi** (`Attivo == true`) — coerente con `LoadActiveProducts()`
- `Stock` strettamente **minore di** `threshold` (non minore o uguale)
- Include la navigation property `Category` (`.Include(p => p.Category)`) — coerente con tutti gli altri endpoint di lista
- Ordinati per `Stock` **crescente** (i più critici prima)

### Risposta — 200 OK
Stessa shape usata dal resto delle GET di lista: entità `Product` con `Category` inclusa.
Lista vuota `[]` se nessun prodotto soddisfa la condizione (non 404).

```json
[
  {
    "id": 7,
    "codice": "RG-200",
    "nome": "Regolatore bassa pressione",
    "descrizione": null,
    "prezzo": 48.50,
    "stock": 1,
    "attivo": true,
    "categoryId": 2,
    "category": { "id": 2, "nome": "Regolatori" }
  }
]
```

### Risposta — 400 Bad Request
Nei casi seguenti, nell'ordine in cui l'handler li verifica:

| Caso | Controllo | Messaggio |
|---|---|---|
| `threshold` assente | `threshold == null` | `"Il parametro 'threshold' è obbligatorio."` |
| `threshold` non intero (es. `?threshold=abc`) | binding ASP.NET Core | risposta automatica del framework |
| `threshold <= 0` | `threshold < 1` | `"Il parametro 'threshold' deve essere un intero positivo (>= 1)."` |

**Nota sui valori non realistici:**
- `threshold = 0`: `Stock` non può essere negativo, quindi la query restituirebbe sempre lista vuota — trattato come input non valido (`threshold < 1` → 400)
- `threshold < 0`: stesso motivo — 400
- `threshold` molto grande (es. `999999`): tecnicamente valido, restituisce tutti i prodotti attivi — nessun cap superiore

---

## Convenzioni da rispettare (da CLAUDE.md)

- Metodo service tecnico → nome **inglese**: `GetProductsBelowStock`
- Route path → **inglese** (coerente con `/active`, `/search`, `/price-with-vat`)
- Metodo **async** con `Task<List<Product>>` e `await`
- File-scoped namespace
- Nessun commento a meno che il WHY non sia non ovvio

---

## Vincoli

- **Non** modificare `Program.cs` — la registrazione endpoint è già presente
- **Non** aggiungere pacchetti NuGet
- **Non** alterare lo schema del DbContext o i seed data
- Il parametro `threshold` è **obbligatorio** — nessun default nascosto, il chiamante deve essere sempre esplicito
- `threshold` deve essere **>= 1**: valori ≤ 0 sono strutturalmente privi di significato (lo stock non è mai negativo) e restituiscono 400
- Non introdurre paginazione o sorting configurabile: semplicità > flessibilità prematura

---

## Verifica

1. `dotnet build` — nessun errore di compilazione
2. `dotnet run` → aprire `http://localhost:5000/swagger`
3. `GET /api/products/low-stock` *(threshold assente)* — deve restituire **400**
4. `GET /api/products/low-stock?threshold=abc` — deve restituire **400**
5. `GET /api/products/low-stock?threshold=0` — deve restituire **400** (valore non realistico)
6. `GET /api/products/low-stock?threshold=-5` — deve restituire **400** (valore non realistico)
7. `GET /api/products/low-stock?threshold=5` — verifica che la risposta contenga solo prodotti con Stock < 5 e tutti `Attivo == true`
8. `GET /api/products/low-stock?threshold=100` — deve restituire tutti i prodotti attivi (seed: 13 prodotti)
9. `GET /api/products/low-stock?threshold=999999` — deve restituire tutti i prodotti attivi senza errore
