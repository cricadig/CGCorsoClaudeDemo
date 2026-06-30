# SPEC — Fix minimo: aliquota IVA in CreaPreventivo

## Obiettivo

Correggere il valore hardcodato errato (20%) usato per il calcolo dell'IVA in
`QuoteService.CreaPreventivo()`, allineandolo al 22% usato nel resto del sistema.

---

## File da modificare

| File | Riga | Modifica |
|---|---|---|
| `Services/QuoteService.cs` | 57 | `* 1.20m` → `* 1.22m` |

Nessun altro file coinvolto.

---

## Impatti

- I preventivi creati **dopo** il fix riporteranno il totale corretto (IVA 22%).
- I preventivi già salvati nel DB **non vengono ricalcolati**: i record esistenti
  mantengono il totale errato. Se necessario, allinearli è un'operazione separata
  (fuori scope di questo fix).
- `QuoteService.CalculateTotal()` usa già `* 1.22m` e non richiede modifiche.

---

## Comportamento atteso

A parità di righe e sconti, il campo `Totale` di un `Quote` appena creato deve
essere identico al `Totale` del corrispondente `Order`.

Esempio con imponibile 1 000 €:

| | Prima | Dopo |
|---|---|---|
| `Quote.Totale` | 1 200,00 € | **1 220,00 €** |
| `Order.Totale` | 1 220,00 € | 1 220,00 € |

---

## Vincoli

- Non introdurre nuove dipendenze o astrazioni.
- Non modificare `Program.cs`, il DbContext o i dati di seed.
- La modifica deve essere limitata alla singola riga indicata.

---

## Verifica

1. `dotnet build` — nessun errore di compilazione.
2. Avviare con `dotnet run`.
3. Creare un preventivo tramite UI o API (`POST /api/quotes`).
4. Creare un ordine con le stesse righe (`POST /api/orders`).
5. Confrontare `Quote.Totale` e `Order.Totale`: devono coincidere.
6. Verificare che `QuoteService.CalculateTotal()` restituisca lo stesso valore
   di `Quote.Totale` per lo stesso preventivo.
