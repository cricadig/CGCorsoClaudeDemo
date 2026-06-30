# SPEC — Fix robusto: centralizzazione aliquota IVA

## Obiettivo

Eliminare la duplicazione dell'aliquota IVA hardcodata in più punti del codice.
Definire un'unica costante autorevole in `PricingHelper` e fare in modo che tutti
i calcoli IVA la usino, rendendo futuri aggiornamenti dell'aliquota una modifica
in un solo posto.

---

## File da modificare

### 1. `Services/PricingHelper.cs`

Aggiungere una costante pubblica:

```csharp
public const decimal AliquotaIVA = 0.22m;
```

Aggiornare i metodi esistenti per usarla:

```csharp
public static decimal AddVat(decimal prezzo)       => prezzo * (1 + AliquotaIVA);
public static float   CalcolaConIVA(float importo) => importo * (1 + (float)AliquotaIVA);
```

### 2. `Services/QuoteService.cs`

- Riga 57: sostituire `* 1.20m` con `* (1 + PricingHelper.AliquotaIVA)`
- Riga 82: sostituire `* 1.22m` con `* (1 + PricingHelper.AliquotaIVA)`

### 3. `Services/OrderService.cs`

- Riga 52: sostituire `* 1.22m` con `* (1 + PricingHelper.AliquotaIVA)`

---

## Impatti

- Tutti i nuovi preventivi e ordini useranno automaticamente l'aliquota definita
  in `PricingHelper.AliquotaIVA`.
- I record già salvati nel DB non vengono ricalcolati (stesso limite del fix minimo).
- Una futura modifica dell'aliquota richiederà l'aggiornamento di **una sola riga**
  (`PricingHelper.AliquotaIVA`).
- `PricingHelper` è una classe `static`: nessuna modifica alla registrazione DI
  in `Program.cs`.
- `CalcolaConIVA(float)` effettua un cast esplicito `(float)AliquotaIVA`: il
  comportamento è invariato rispetto all'attuale `1.22f`, nessuna perdita di
  precisione rilevante per i valori in gioco.

---

## Comportamento atteso

Identico al fix minimo: `Quote.Totale` e `Order.Totale` coincidono a parità di
righe e sconti. In aggiunta, `QuoteService.CalculateTotal()` è garantito coerente
perché legge la stessa costante.

Esempio con imponibile 1 000 €:

| Campo | Valore |
|---|---|
| `Quote.Totale` | 1 220,00 € |
| `Order.Totale` | 1 220,00 € |
| `QuoteService.CalculateTotal()` | 1 220,00 € |

---

## Vincoli

- Non aggiungere pacchetti NuGet, non modificare `Program.cs`, il DbContext o i
  dati di seed (rispettare i vincoli di `CLAUDE.md`).
- `PricingHelper` rimane `static` — non registrarla nel container DI.
- Non introdurre FluentValidation o altri framework di validazione.
- La costante deve essere `decimal` per rispettare la convenzione sui calcoli
  monetari del progetto.

---

## Verifica

1. `dotnet build` — nessun errore o warning di compilazione.
2. Avviare con `dotnet run`.
3. Creare un preventivo e un ordine con le stesse righe; confrontare i totali.
4. Chiamare `QuoteService.CalculateTotal()` sullo stesso preventivo e verificare
   che il risultato sia uguale a `Quote.Totale`.
5. Modificare temporaneamente `AliquotaIVA` a `0.10m`, ripetere i test: tutti i
   totali devono aggiornarsi coerentemente senza toccare altri file.
6. Ripristinare `AliquotaIVA = 0.22m` prima del commit.
