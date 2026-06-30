# Spec: Fix MapPost /api/products — validazione DataAnnotations

## Obiettivo

Attivare la validazione delle `DataAnnotations` sull'endpoint `POST /api/products`. Nelle Minimal API di ASP.NET Core 8, a differenza dei Controller MVC, la validazione automatica del model non è attiva: un payload senza `Codice` o `Nome` (entrambi `[Required]`) viene accettato e salvato nel DB con valori nulli o vuoti.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Api/ProductEndpoints.cs` | Endpoint `POST /` (riga 47): aggiungere validazione esplicita con `Validator.TryValidateObject` prima di chiamare `svc.CreateProduct` |

---

## Impatti sui chiamanti

| Scenario | Prima | Dopo |
|---|---|---|
| `POST` senza `Codice` | `201 Created` (dato corrotto salvato) | `400 BadRequest` con lista errori |
| `POST` senza `Nome` | `201 Created` (dato corrotto salvato) | `400 BadRequest` con lista errori |
| `POST` con tutti i campi validi | `201 Created` | `201 Created` (invariato) |

---

## Comportamento atteso

**Prima (attuale):**
- Il body viene deserializzato direttamente in `Product`.
- Le `DataAnnotations` (`[Required]` su `Codice` e `Nome`) non vengono valutate.
- Il prodotto viene salvato anche con dati mancanti.

**Dopo:**
1. Prima di chiamare `svc.CreateProduct`, il prodotto ricevuto viene validato con `Validator.TryValidateObject`.
2. Se la validazione fallisce → `400 BadRequest` con la lista degli errori di validazione.
3. Se la validazione passa → `CreateProduct` e `201 Created` come prima.

---

## Vincoli

- Non introdurre FluentValidation (vietato da CLAUDE.md).
- Non modificare `Program.cs` per aggiungere middleware globale di validazione.
- Usare `System.ComponentModel.DataAnnotations.Validator.TryValidateObject` — già disponibile senza nuovi pacchetti.
- Non introdurre ViewModel o DTO separati — validare il `Product` ricevuto direttamente.

---

## Verifica

1. `dotnet build` — 0 errori.
2. `POST /api/products` senza `Codice` → `400 BadRequest` con messaggio di errore.
3. `POST /api/products` senza `Nome` → `400 BadRequest` con messaggio di errore.
4. `POST /api/products` con payload completo → `201 Created` e prodotto salvato correttamente.
