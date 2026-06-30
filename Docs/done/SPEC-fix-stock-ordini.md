# SPEC — Fix gestione stock su creazione e annullamento ordine

## Obiettivo

Correggere `OrderService` in modo che:

1. La creazione di un ordine verifichi la disponibilità dello stock e lo scali.
2. L'annullamento di un ordine ripristini lo stock (segno corretto).
3. Nessuna delle due operazioni venga eseguita se l'ordine è in uno stato
   che non lo consente (guardie sulle transizioni di stato).

---

## Stato attuale dei bug

| Metodo | Bug |
|---|---|
| `CreateOrder()` | Non verifica né scala lo stock |
| `CancelOrder()` | Usa `-=` invece di `+=` (segno invertito) |
| `CancelOrder()` | Non verifica lo stato corrente: invocabile più volte |
| `MarkAsShipped()` | Non verifica lo stato corrente |

---

## Macchina a stati degli ordini

Gli stati validi sono già documentati in `Models/Order.cs:19`:

```
Confermato → InPreparazione → Spedito → Consegnato
                                      ↘
                              Annullato  (solo da Confermato o InPreparazione)
```

Regole di transizione da far rispettare:

| Transizione | Consentita da | Metodo |
|---|---|---|
| → Spedito | Confermato, InPreparazione | `MarkAsShipped()` |
| → Annullato | Confermato, InPreparazione | `CancelOrder()` |
| → Consegnato | Spedito | (futuro — fuori scope) |

---

## File da modificare

Un solo file: `Services/OrderService.cs`

---

## Modifiche richieste

### `CreateOrder()` — verifica disponibilità e scala stock

Per ogni riga dell'ordine, prima del salvataggio:

1. Caricare il prodotto con `FindAsync(item.ProductId)`.
2. Se il prodotto non esiste → lanciare `InvalidOperationException`.
3. Se `product.Stock < item.Quantity` → lanciare `InvalidOperationException`
   con messaggio che indica il prodotto e le quantità in gioco.
4. Scalare lo stock: `product.Stock -= item.Quantity`.
5. Salvare ordine e aggiornamenti stock in un'unica chiamata a `SaveChangesAsync()`.

### `CancelOrder()` — correggi segno e aggiungi guardia di stato

1. Dopo aver caricato l'ordine, verificare che `order.Stato` sia
   `"Confermato"` o `"InPreparazione"`. Se è qualsiasi altro valore
   (`"Spedito"`, `"Consegnato"`, `"Annullato"`) → restituire `false`
   senza modificare nulla.
2. Correggere il segno: `product.Stock += item.Quantity` (era `-=`).
3. Aggiornare `order.Stato = "Annullato"`.
4. Salvare tutto in un'unica chiamata a `SaveChangesAsync()`.

### `MarkAsShipped()` — aggiungi guardia di stato

1. Caricare l'ordine con `Include(o => o.Items)` per coerenza.
2. Verificare che `order.Stato` sia `"Confermato"` o `"InPreparazione"`.
   Se è qualsiasi altro valore → restituire `false`.
3. Impostare `order.Stato = "Spedito"` e `order.DataSpedizione = DateTime.Now`.
4. Salvare e restituire `true`.

---

## Impatti

- Gli ordini già salvati nel DB con stato incoerente non vengono toccati.
- Lo stock esistente non viene ricalcolato retroattivamente (fuori scope).
- Nessuna modifica a `Program.cs`, DbContext, modelli o seed data.
- Nessuna nuova dipendenza NuGet.

---

## Vincoli

- Rispettare le convenzioni del progetto (CLAUDE.md): async/await, no `.Result`,
  `decimal` per i monetari, file-scoped namespace.
- Non introdurre una classe enum esterna per gli stati: usare le stringhe costanti
  già definite nel commento di `Order.cs:19`.
- Non modificare la firma pubblica di `CreateOrder`, `CancelOrder`, `MarkAsShipped`.
  `CreateOrder` può lanciare eccezione invece di restituire `null` — verificare
  che i caller (Controller e Minimal API) gestiscano l'eccezione.

---

## Comportamento atteso dopo il fix

| Scenario | Risultato atteso |
|---|---|
| Ordine 1 000 pz, stock 5 | Eccezione — ordine non creato, stock invariato |
| Ordine 3 pz, stock 5 | Ordine creato, stock scende a 2 |
| Annulla ordine Confermato | Stock ripristinato, stato → Annullato |
| Annulla ordine già Annullato | `false`, stock invariato |
| Annulla ordine Spedito | `false`, stock invariato |
| Spedisci ordine Confermato | Stato → Spedito, data impostata |
| Spedisci ordine Annullato | `false`, nessuna modifica |

---

## Verifica

1. `dotnet build` — nessun errore.
2. Avviare con `dotnet run`.
3. Creare un ordine con quantità superiore allo stock → risposta errore (500
   o 400 a seconda di come il caller gestisce l'eccezione).
4. Creare un ordine con quantità valida → `Product.Stock` scende del corretto
   numero di unità (verificabile via `GET /api/products/{id}`).
5. Annullare l'ordine → `Product.Stock` torna al valore precedente.
6. Annullare lo stesso ordine una seconda volta → `false`, stock invariato.
7. Spedire un ordine annullato → `false`, stato non cambia.
