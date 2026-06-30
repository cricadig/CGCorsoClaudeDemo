# Spec: Fix price-with-vat — calcolo IVA con decimal

## Obiettivo

Sostituire l'aritmetica `float` con `decimal` nel calcolo del prezzo IVA inclusa nell'endpoint `GET /api/products/{id}/price-with-vat`. CLAUDE.md prescrive `decimal` per tutti i calcoli monetari.

---

## File da modificare

| File | Cosa cambia |
|---|---|
| `Api/ProductEndpoints.cs` | Riga 71: `p.Prezzo * 1.22f` → `(decimal)p.Prezzo * 1.22m` |

---

## Impatti sui chiamanti

L'endpoint non ha chiamanti interni — la modifica impatta solo la risposta JSON di `GET /api/products/{id}/price-with-vat`.

| Scenario | Prima | Dopo |
|---|---|---|
| Prezzo con decimali (es. 10.50) | `PrezzoIvaInclusa` può avere imprecisioni float (es. 12.809999...) | `PrezzoIvaInclusa` preciso al centesimo (12.81) |
| Tipo JSON del campo | number (float) | number (decimal) — stesso tipo JSON, valore più preciso |

---

## Comportamento atteso

**Prima (attuale):**
- `p.Prezzo` è `float` sul modello (`Product.cs:17`).
- `p.Prezzo * 1.22f` esegue moltiplicazione float → errori di arrotondamento binario sui valori decimali.

**Dopo:**
1. Il valore `p.Prezzo` viene castato a `decimal`: `(decimal)p.Prezzo`.
2. La moltiplicazione avviene in aritmetica decimale: `* 1.22m`.
3. Il risultato `PrezzoIvaInclusa` nella risposta è preciso al centesimo.

---

## Vincoli

- Non modificare `Product.Prezzo` da `float` a `decimal` — cambio di tipo sul modello è una modifica allo schema DB (vietato da CLAUDE.md senza conferma).
- Il cast `(decimal)` è necessario perché `Prezzo` è `float` sul modello.
- Non toccare `PricingHelper.cs` — fuori scope.
- Non modificare `Program.cs`.

---

## Verifica

1. `dotnet build` — 0 errori.
2. `GET /api/products/1/price-with-vat` — verificare che `PrezzoIvaInclusa` sia un valore preciso (es. `12.81` e non `12.809999...`).
