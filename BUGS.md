# BUGS.md — Bug attivi in CavagnaDemo

Lista dei bug identificati nel codice. Da risolvere con ticket/discussione di team prima di correggere.

---

## BUG-1 — IVA errata in `CreaPreventivo`

**File:** `Services/QuoteService.cs:57`

`CreaPreventivo` applica IVA al **20%** (`* 1.20m`), mentre tutto il resto del progetto usa **22%**:
- `PricingHelper.AddVat` → `* 1.22m`
- `PricingHelper.CalcolaConIVA` → `* 1.22f`
- `QuoteService.CalculateTotal` → `* 1.22m`
- `Controllers/ProductsController.cs:34` → `* 1.22f`
- `Api/ProductEndpoints.cs:60` → `* 1.22f`

**Impatto:** I preventivi vengono calcolati con IVA sbagliata rispetto agli ordini e al catalogo.
**Azione:** Verificare con il team se intenzionale (IVA agevolata su alcune categorie?) o errore di battitura.

---

## BUG-2 — Arrotondamento errato in `RoundPrice`

**File:** `Services/PricingHelper.cs:29`

`RoundPrice` usa `Math.Round(price, 2)` con la modalità default (`MidpointRounding.ToEven` — BankersRounding).
Un commento nel codice stesso avvisa che **il sistema contabile si aspetta `MidpointRounding.AwayFromZero`**.

**Impatto:** Differenze di centesimi nei totali contabili su valori che finiscono esattamente a `.xx5`.
**Fix atteso:** `Math.Round(price, 2, MidpointRounding.AwayFromZero)`

---

## BUG-3 — `RetrieveById` sincrono in `ProductService`

**File:** `Services/ProductService.cs:22`

`RetrieveById` usa `FirstOrDefault` sincrono mentre tutti gli altri metodi del servizio sono async.
Il `ProductsController.Details` e `ProductEndpoints` (endpoint `/{id}` e `/price-with-vat`) sono di conseguenza sincroni.

**Impatto:** Blocco del thread sotto carico su query al DB — basso rischio con SQLite, alto rischio se si migra a SQL Server.
**Fix atteso:** Rinominare in `RetrieveByIdAsync`, usare `FirstOrDefaultAsync`, aggiornare i chiamanti.
