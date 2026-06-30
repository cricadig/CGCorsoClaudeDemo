# SPEC — BUG-01: Dereference su null in POST /api/quotes

## Obiettivo

Aggiungere una guardia esplicita su `productSvc.RetrieveById()` nell'endpoint
`POST /api/quotes` per evitare `NullReferenceException` quando un `ProductId`
nella richiesta non corrisponde a nessun prodotto nel DB.

---

## Contesto del bug

`Api/QuoteEndpoints.cs:32-38` costruisce le righe del preventivo chiamando
`productSvc.RetrieveById(r.ProductId)` per ogni riga della richiesta.
Il metodo restituisce `Product?` (nullable), ma il risultato viene usato
direttamente senza controllo:

```csharp
var product = productSvc.RetrieveById(r.ProductId);   // può essere null
righe.Add(new QuoteItem
{
    ProductId  = r.ProductId,
    PrezzoUnitario = (decimal)product.Prezzo,          // NullReferenceException
    ...
});
```

Il compilatore segnala già il warning CS8602 su questa riga.

---

## File da modificare

| File | Riga | Modifica |
|---|---|---|
| `Api/QuoteEndpoints.cs` | 32-38 | Aggiungere guardia null dopo `RetrieveById` |

---

## Comportamento atteso

| Scenario | Risposta HTTP attuale | Risposta HTTP attesa |
|---|---|---|
| Tutti i `ProductId` esistono | 201 Created | 201 Created (invariato) |
| Almeno un `ProductId` non esiste | 500 (crash) | 400 Bad Request con messaggio esplicito |

Il messaggio di errore deve indicare quale `ProductId` non è stato trovato,
ad esempio: `"Prodotto 99 non trovato."`.

---

## Impatto

- Nessuna modifica a `ProductService`, `QuoteService`, modelli o seed data.
- Nessuna modifica a `Program.cs`.
- Il comportamento per richieste valide è invariato.

---

## Vincoli

- Rispettare le convenzioni del progetto (CLAUDE.md): no FluentValidation,
  usare `Results.BadRequest()` per input non validi.
- Non modificare la firma di `RetrieveById` — la guardia va nell'endpoint.

---

## Verifica

1. `dotnet build` — il warning CS8602 su `QuoteEndpoints.cs:37` deve scomparire.
2. `POST /api/quotes` con un `ProductId` inesistente (es. 9999)
   → risposta `400 Bad Request` con messaggio `"Prodotto 9999 non trovato."`.
3. `POST /api/quotes` con dati validi
   → risposta `201 Created` invariata.
