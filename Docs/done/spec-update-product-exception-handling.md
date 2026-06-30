# Spec: Fix UpdateProduct — gestione eccezioni

## Obiettivo

Eliminare il `catch (Exception)` in `UpdateProduct` che maschera qualsiasi errore restituendo silenziosamente `false`. Il chiamante deve poter distinguere "prodotto non trovato" da "errore di sistema".

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Services/ProductService.cs` | Metodo `UpdateProduct` — aggiunta verifica esistenza, rimozione catch generico |
| `Api/ProductEndpoints.cs` | Endpoint `PUT /{id:int}` — nessuna modifica al codice, ma cambia il comportamento HTTP in caso di errore di sistema (vedi sezione Impatti) |

---

## Impatti sui chiamanti

L'unico chiamante è `Api/ProductEndpoints.cs:56` (`PUT /{id:int}`).

| Scenario | Prima | Dopo |
|---|---|---|
| Prodotto non trovato | `400 BadRequest` | `400 BadRequest` (invariato) |
| Update riuscito | `204 No Content` | `204 No Content` (invariato) |
| Errore di sistema (DB, constraint) | `400 BadRequest` | `500 Internal Server Error` |

Il cambio su errori di sistema è **intenzionalmente corretto** ma costituisce un breaking change nel contratto HTTP: qualsiasi client che oggi gestisce `400` come catch-all dovrà gestire anche `500`.

---

## Comportamento atteso

**Prima (attuale):**
- `UpdateProduct` prova ad aggiornare qualsiasi ID senza verificare se esiste.
- Qualsiasi errore (prodotto non trovato, DB down, violazione constraint) restituisce `false`.
- Impossibile distinguere le cause.

**Dopo:**
1. Il metodo verifica l'esistenza del prodotto con `FindAsync(product.Id)`.
2. Se il prodotto **non esiste** → restituisce `false` (semantica invariata per il chiamante, endpoint risponde `400 BadRequest` come prima).
3. Se il prodotto **esiste** → esegue l'update, salva e restituisce `true`.
4. Il blocco `try/catch` viene rimosso: errori di sistema (DB non raggiungibile, violazione di constraint) propagano naturalmente come eccezione non gestita, visibile nei log e gestita dal middleware globale di ASP.NET Core.

---

## Vincoli

- La firma del metodo rimane `async Task<bool>` — nessun breaking change verso `ProductEndpoints.cs`.
- Non introdurre eccezioni custom né nuovi tipi.
- Non modificare `Program.cs`.
- Usare `FindAsync` (già presente in `DeleteProduct` alla riga 69 come pattern di riferimento).

---

## Verifica

1. `dotnet build` — 0 errori.
2. `PUT /api/products/{id}` con ID esistente → `204 No Content`.
3. `PUT /api/products/9999` con ID inesistente → `400 BadRequest`.
4. Errore di sistema simulabile rimuovendo temporaneamente il DB: l'eccezione deve essere visibile nel log, non silenziata.
