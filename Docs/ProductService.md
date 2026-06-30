# ProductService — Guida per neoassunti

File sorgente: [`Services/ProductService.cs`](../Services/ProductService.cs)  
Modello principale: [`Models/Product.cs`](../Models/Product.cs)

---

## Panoramica

`ProductService` è il layer di accesso ai dati per i prodotti del catalogo. Viene registrato come `Scoped` in `Program.cs` e iniettato nei controller e negli endpoint che lo richiedono. Non contiene logica di business complessa: è principalmente un wrapper su Entity Framework Core con qualche filtro.

Riceve un `CavagnaDbContext` via costruttore — questo è il **solo** punto in cui deve essere fatto l'accesso al DB per i prodotti.

---

## Modello `Product`

| Proprietà | Tipo | Note |
|---|---|---|
| `Id` | `int` | Chiave primaria, generata dal DB |
| `Codice` | `string` | Codice prodotto, obbligatorio (`[Required]`) |
| `Nome` | `string` | Nome prodotto, obbligatorio (`[Required]`) |
| `Descrizione` | `string?` | Nullable — può essere assente |
| `Prezzo` | `float` | Prezzo di listino — vedi stranezze |
| `Stock` | `int` | Giacenza attuale in magazzino |
| `CategoryId` / `Category` | FK + nav. | Categoria di appartenenza |
| `Attivo` | `bool` | Flag logico: `true` = prodotto visibile/attivo |

---

## Metodi pubblici

### `GetAllProducts()` — [riga 16](../Services/ProductService.cs)

```csharp
public async Task<List<Product>> GetAllProducts()
```

Restituisce tutti i prodotti presenti nel DB, attivi e non, con la categoria inclusa (`Include(p => p.Category)`).  
Nessun filtro, nessun ordinamento. Se il catalogo cresce molto, questa query può diventare pesante — al momento non c'è paginazione.

---

### `RetrieveById(int id)` — [riga 21](../Services/ProductService.cs)

```csharp
public Product? RetrieveById(int id)
```

Recupera un singolo prodotto per ID, con la categoria inclusa. Restituisce `null` se non trovato.

**Stranezza — vedi sezione sotto:** è l'unico metodo **sincrono** del service. Non usa `async`/`await`, blocca il thread durante la query al DB.

---

### `LoadActiveProducts()` — [riga 26](../Services/ProductService.cs)

```csharp
public async Task<List<Product>> LoadActiveProducts()
```

Restituisce solo i prodotti con `Attivo == true`. **Non** carica la categoria (`Include` assente) — se hai bisogno di `product.Category.Nome` con questo metodo, trovi `null`.

---

### `GetProductsForQuote(List<int> productIds)` — [riga 31](../Services/ProductService.cs)

```csharp
public async Task<List<Product>> GetProductsForQuote(List<int> productIds)
```

Dato un elenco di ID, restituisce i prodotti corrispondenti con la categoria. Viene usato tipicamente durante la creazione di un preventivo per recuperare le anagrafiche dei prodotti scelti.

**Assunto:** se passi un ID che non esiste nel DB, non viene sollevata nessuna eccezione — quel prodotto semplicemente non compare nella lista restituita. Il chiamante non riceve segnalazione degli ID mancanti.

---

### `CheckStockForOrder(int productId, int requestedQty)` — [riga 39](../Services/ProductService.cs)

```csharp
public bool CheckStockForOrder(int productId, int requestedQty)
```

Verifica se la giacenza di un prodotto è sufficiente per una quantità richiesta.

**Flusso:**
1. Cerca il prodotto con `_db.Products.Find(productId)` (cerca prima nella cache EF, poi al DB).
2. Se il prodotto non esiste, restituisce `false`.
3. Confronta `product.Stock >= requestedQty` e restituisce il risultato booleano.

**Assunto:** `false` significa sia "prodotto non trovato" che "stock insufficiente" — il chiamante non può distinguere i due casi.

**Stranezza — vedi sezione sotto:** è sincrono, come `RetrieveById`.

---

### `CreateProduct(Product product)` — [riga 46](../Services/ProductService.cs)

```csharp
public async Task<Product> CreateProduct(Product product)
```

Aggiunge un nuovo prodotto al DB.

**Flusso:**
1. `_db.Products.Add(product)` — mette il prodotto in tracking EF con stato `Added`.
2. `await _db.SaveChangesAsync()` — esegue l'`INSERT` nel DB.
3. Restituisce lo stesso oggetto `product` ricevuto, ora con `Id` valorizzato dal DB.

**Assunto:** non viene fatto nessun controllo prima di salvare — niente verifica se esiste già un prodotto con lo stesso `Codice`, niente validazione aggiuntiva oltre ai `[Required]` del modello. Se mandi un oggetto con `Codice` duplicato, dipende dal DB se va in errore o meno (non c'è un vincolo `UNIQUE` esplicito nello schema).

---

### `UpdateProduct(Product product)` — [riga 53](../Services/ProductService.cs)

```csharp
public async Task<bool> UpdateProduct(Product product)
```

Aggiorna un prodotto esistente. Restituisce `false` se l'ID non esiste, `true` se ha salvato.

**Flusso in dettaglio:**
1. `await _db.Products.FindAsync(product.Id)` — cerca il prodotto esistente nel DB per verificare che esista.
2. `_db.Entry(existing).State = EntityState.Detached` — distacca l'entità appena caricata dal tracker EF. Se non si fa questo passaggio, EF traccia già un'entità con quell'ID e il successivo `Update` genererebbe un'eccezione ("istanza con stesso ID già tracciata").
3. `_db.Products.Update(product)` — attacca l'oggetto ricevuto come parametro e lo segna tutto come `Modified`.
4. `await _db.SaveChangesAsync()` — esegue l'`UPDATE` nel DB, sovrascrivendo **tutti** i campi.

**Assunto:** il pattern load→detach→update sovrascrive l'intera riga. Non è un update parziale: se il chiamante passa un oggetto con `Prezzo = 0` per errore, il prezzo viene azzerato nel DB senza warning.

---

### `DeleteProduct(int id)` — [riga 63](../Services/ProductService.cs)

```csharp
public async Task<bool> DeleteProduct(int id)
```

Elimina fisicamente un prodotto dal DB. Restituisce `false` se non trovato, `true` se eliminato.

**Flusso:**
1. `FindAsync(id)` — cerca il prodotto.
2. Se non esiste, restituisce `false`.
3. `_db.Products.Remove(product)` — segna per cancellazione.
4. `await _db.SaveChangesAsync()` — esegue il `DELETE`.

**Stranezza — vedi sezione sotto:** la cancellazione è **fisica**, non logica. Il modello ha già un campo `Attivo` che consentirebbe di "disattivare" un prodotto senza eliminarlo — ma questo metodo lo cancella dal DB per sempre. Se un prodotto è già presente in un preventivo o in un ordine, potrebbero restare righe orfane (dipende dai vincoli di FK nel DB).

---

### `SearchByName(string query)` — [riga 72](../Services/ProductService.cs)

```csharp
public async Task<List<Product>> SearchByName(string query)
```

Ricerca prodotti il cui `Nome` contiene la stringa `query` (sottostringa, case sensitivity dipende da SQLite).  
Non carica la categoria. Non filtra per `Attivo` — restituisce anche i prodotti disattivati.

**Stranezza — vedi sezione sotto:** se `query` è una stringa vuota `""`, `Contains("")` è sempre vero — restituisce l'intero catalogo. Se `query` è `null`, EF genera un'eccezione a runtime.

---

### `GetProductsBelowStock(int threshold)` — [riga 79](../Services/ProductService.cs)

```csharp
public async Task<List<Product>> GetProductsBelowStock(int threshold)
```

Restituisce i prodotti attivi con `Stock < threshold`, ordinati per stock crescente (prima quelli più critici). Carica anche la categoria. Utile per dashboard di riordino magazzino.

È il metodo più completo e corretto del service: filtra, ordina, include la navigation property e considera il flag `Attivo`.

---

## Assunti generali

- Le validation annotations (`[Required]`) sul modello vengono controllate dal framework MVC nei controller, ma **non** da `ProductService` internamente. Un oggetto `Product` con `Nome = ""` passato direttamente al service viene salvato senza errori.
- Nessun metodo applica soft-delete: `DeleteProduct` rimuove fisicamente.
- Nessun metodo gestisce la concorrenza ottimistica (no `RowVersion`, no `Timestamp`). Due utenti che salvano lo stesso prodotto contemporaneamente: vince l'ultimo.
- `LoadActiveProducts` e `SearchByName` non caricano `Category` — accedere a `product.Category` dopo averli usati restituisce `null` (lazy loading non è abilitato in questo progetto).

---

## Stranezze e problemi

### Gravità: Migliorabile (best practice)

**`LoadActiveProducts` non carica la categoria**  
[Riga 28](../Services/ProductService.cs) — a differenza di `GetAllProducts` e `GetProductsBelowStock`, non ha `Include(p => p.Category)`. Il comportamento incoerente tra metodi simili può sorprendere chi lo usa e causare `NullReferenceException` se si accede a `product.Category`.

**`SearchByName` non filtra per `Attivo`**  
[Riga 74](../Services/ProductService.cs) — una ricerca nel catalogo restituisce anche prodotti disattivati, che l'utente non dovrebbe vedere. Probabilmente un'omissione.

**`false` ambiguo in `CheckStockForOrder`**  
[Riga 42](../Services/ProductService.cs) — restituisce `false` sia per prodotto inesistente che per stock insufficiente. Il chiamante non può distinguere le due situazioni senza fare una seconda query.

**Nessun ordinamento di default in `GetAllProducts`**  
[Riga 18](../Services/ProductService.cs) — l'ordine dei risultati dipende dal DB e può cambiare. Per elenchi mostrati in UI è preferibile un `OrderBy` esplicito.

---

### Gravità: Violazione di best practice

**`RetrieveById` e `CheckStockForOrder` sono sincroni**  
[Righe 21](../Services/ProductService.cs) e [39](../Services/ProductService.cs) — tutti gli altri metodi sono `async`. Questi due bloccano il thread ASP.NET durante la query al DB, riducendo la capacità di gestire richieste concorrenti sotto carico. Andrebbero convertiti in `async Task<T>`.

**`Prezzo` è `float` invece di `decimal`**  
[`Models/Product.cs` riga 17](../Models/Product.cs) — `float` è un tipo a virgola mobile binaria: non rappresenta esattamente valori decimali come `0.10` o `19.99`. Moltiplicazioni e somme accumulano errori di arrotondamento. Per importi monetari il tipo corretto è `decimal`. Il CLAUDE.md del progetto specifica esplicitamente di usare `decimal` per i calcoli monetari — `Prezzo` sul modello è un'eccezione non allineata a questa convenzione. Il TODO nel modello segnala già che il campo è in lavorazione.

---

### Gravità: Bug potenziale

**`SearchByName(null)` lancia eccezione a runtime**  
[Riga 74](../Services/ProductService.cs) — `p.Nome.Contains(query)` con `query == null` genera `ArgumentNullException`. Non c'è nessun guard prima della chiamata. Se il chiamante passa `null` (es. da una form con campo vuoto non compilato), il server risponde con un errore 500.

**`DeleteProduct` ignora le relazioni**  
[Riga 63](../Services/ProductService.cs) — se il prodotto è già referenziato in righe di preventivo (`QuoteItem.ProductId`), la cancellazione può fallire con un errore di FK violation (SQLite in modalità strict) oppure lasciare righe orfane con `ProductId` non più valido. Non c'è nessun controllo preventivo né messaggio di errore comprensibile per l'utente.

**`SearchByName("")` restituisce tutto il catalogo**  
[Riga 74](../Services/ProductService.cs) — `Contains("")` è sempre vero. Se la ricerca viene chiamata con stringa vuota (campo form non compilato), si ottiene l'intero catalogo invece di zero risultati o un errore. Può avere impatti di performance su cataloghi grandi.
