# Audit — Services/ e Api/

**Data:** 2026-06-03  
**Scope:** `Services/*.cs`, `Api/*.cs`  
**Ultimo aggiornamento:** 2026-06-03  
**Stato:** 2 fix applicati (BUG-01, BUG-03) — BUG-04 coperto da test failing

---

## 🔴 Bug — Impatto funzionale diretto

### ~~BUG-01 — Dereference su `null` senza guardia~~ ✅ RISOLTO
**File:** `Api/QuoteEndpoints.cs:34`  
**Fix:** aggiunta guardia `if (product is null) return Results.BadRequest(...)` prima del cast.
Warning CS8602 eliminato. `POST /api/quotes` con `ProductId` inesistente restituisce ora `400 Bad Request`.

---

### BUG-02 — Race condition sullo stock
**File:** `Api/OrderEndpoints.cs:28-38`  
Il prezzo viene letto da `ProductService.RetrieveById()` e lo stock viene controllato/scalato
da `OrderService.CreateOrder()` in due query separate e non atomiche.
Sotto carico concorrente due richieste possono leggere lo stesso stock disponibile,
passare entrambe il controllo e sovra-allocare (stock negativo).  
**Fix corretto:** aggiungere `[ConcurrencyCheck]` su `Product.Stock` e gestire
`DbUpdateConcurrencyException` in `CreateOrder`.

---

### ~~BUG-03 — IVA calcolata con `float` invece di `decimal`~~ ✅ RISOLTO
**File:** `Api/ProductEndpoints.cs:71`  
**Fix:** sostituito `p.Prezzo * 1.22f` con `PricingHelper.AddVat((decimal)p.Prezzo)`.

---

### BUG-04 — Parametro di ricerca non validato ⚠️ TEST FAILING
**File:** `Api/ProductEndpoints.cs:30-34`  
```csharp
group.MapGet("/search", async (string q, ProductService svc) =>
```
Se `q` è stringa vuota, `SearchByName("")` restituisce l'intero catalogo (200 OK).  
Se `q` è assente, ASP.NET restituisce già 400 automaticamente.  
Test failing: `ProductSearchValidationTests.Search_ConQVuota_Restituisce400`.

---

## 🟠 Bug potenziale — Comportamento silenziosamente sbagliato

### BUG-05 — `RetrieveById` sincrono blocca il thread
**File:** `Services/ProductService.cs:21-23`  
```csharp
public Product? RetrieveById(int id)
{
    return _db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
}
```
Unico metodo non-async della classe. Blocca il thread del pool durante l'I/O sul DB.
Chiamato da entrambi gli endpoint POST (quotes e orders), i più lenti per definizione.

---

### BUG-06 — `CheckStockForOrder` sincrono e mai chiamato
**File:** `Services/ProductService.cs:39-44`  
```csharp
public bool CheckStockForOrder(int productId, int requestedQty)
{
    var product = _db.Products.Find(productId);
```
Stesso problema di BUG-05. In più non viene chiamato da nessuna parte:
`OrderService.CreateOrder` esegue il controllo autonomamente.
Codice morto che può ingannare chi legge.

---

### BUG-07 — `GetOrdersByCustomer` usa `.Result` (rischio deadlock)
**File:** `Services/OrderService.cs:87-94`  
```csharp
return _db.Orders...ToListAsync().Result;
```
`.Result` su una `Task` in ASP.NET Core può causare deadlock.
Viola la convenzione esplicita del progetto (CLAUDE.md: "non usare `.Result` o `.Wait()`").

---

### BUG-08 — `cancel` restituisce 404 anche quando lo stato non lo consente
**File:** `Api/OrderEndpoints.cs:45`  
```csharp
return ok ? Results.Ok() : Results.NotFound();
```
`CancelOrder` restituisce `false` sia se l'ordine non esiste, sia se è in uno stato
non annullabile (`Spedito`, `Annullato`). Il client non può distinguere i due casi
e riceve sempre 404, anche quando l'ordine esiste.

---

## 🟡 Code smell

### SMELL-01 — `CalcolaConIVA` usa `float` ed è codice morto
**File:** `Services/PricingHelper.cs:12-15`  
```csharp
public static float CalcolaConIVA(float importo)
{
    return importo * 1.22f;
}
```
Duplica `AddVat` con tipo impreciso. Nessun chiamante trovato nel progetto.

---

### SMELL-02 — `RoundPrice` ignora il proprio commento
**File:** `Services/PricingHelper.cs:28-30`  
```csharp
// ma il sistema contabile si aspetta AwayFromZero
return Math.Round(price, 2);   // usa MidpointRounding.ToEven (default)
```
Il commento avverte che serve `AwayFromZero`, ma il codice usa il default `ToEven`.
O il commento è sbagliato, o l'arrotondamento è sbagliato.

---

### SMELL-03 — `ApplicaScontoVolume` non viene mai chiamata
**File:** `Services/PricingHelper.cs:17-24`  
Logica di sconto volume definita ma non usata né da `CreaPreventivo` né da `CreateOrder`.

---

### SMELL-04 — `AggiornaStato` accetta qualsiasi stringa
**File:** `Api/QuoteEndpoints.cs:46-49`  
```csharp
group.MapPatch("/{id:int}/stato", async (int id, string nuovoStato, QuoteService svc) =>
```
Nessuna validazione sui valori ammessi (`Bozza`, `Inviato`, `Accettato`, `Rifiutato`).
Si può impostare qualsiasi stringa arbitraria come stato.

---

## Riepilogo

| ID | Gravità | File | Riga |
|---|---|---|---|
| BUG-01 | ✅ | `Api/QuoteEndpoints.cs` | 34 |
| BUG-02 | 🔴 | `Api/OrderEndpoints.cs` | 28-38 |
| BUG-03 | ✅ | `Api/ProductEndpoints.cs` | 71 |
| BUG-04 | ❌ | `Api/ProductEndpoints.cs` | 30 |
| BUG-05 | 🟠 | `Services/ProductService.cs` | 21-23 |
| BUG-06 | 🟠 | `Services/ProductService.cs` | 39-44 |
| BUG-07 | 🟠 | `Services/OrderService.cs` | 87-94 |
| BUG-08 | 🟠 | `Api/OrderEndpoints.cs` | 45 |
| SMELL-01 | 🟡 | `Services/PricingHelper.cs` | 12-15 |
| SMELL-02 | 🟡 | `Services/PricingHelper.cs` | 28-30 |
| SMELL-03 | 🟡 | `Services/PricingHelper.cs` | 17-24 |
| SMELL-04 | 🟡 | `Api/QuoteEndpoints.cs` | 46-49 |
