# ADR 0002 — Database: PostgreSQL in alternativa a SQLite

**Data:** 2026-06-17
**Stato:** Proposed

---

## Context

Il progetto è un gestionale web ASP.NET Core 8.0 per il settore gas industriale (regolatori,
valvole, raccordi, bombole). Gestisce catalogo prodotti, anagrafica clienti, preventivi e ordini
tramite sette entità relazionali con chiavi esterne (`Products`, `Categories`, `Customers`,
`Quotes`, `QuoteItems`, `Orders`, `OrderItems`).

Il database è attualmente SQLite, configurato in `Program.cs` tramite `UseSqlite()` e
inizializzato con `EnsureCreated()` all'avvio. La connection string punta a un file locale
(`cavagna.db`) definito in `appsettings.json`. Non sono presenti EF Core Migrations: lo schema
è creato da zero ad ogni prima esecuzione.

Lo schema gestisce dati finanziari: i campi `Totale` e `ScontoTotale` su `Quote` e `Order`
usano `decimal`, ma `Product.Prezzo` è dichiarato `float` — un'inconsistenza già segnalata
nel codice con un `TODO`.

L'approccio `EnsureCreated()` è esplicitamente documentato come incompatibile con le migration
EF Core (CLAUDE.md). I service layer accedono al DB esclusivamente tramite `CavagnaDbContext`,
quindi il punto di sostituzione del provider è circoscritto a `Program.cs` e a
`CavagnaDemo.csproj`.

---

## Decision

Adottare **PostgreSQL** come database per il progetto, sostituendo il provider
`Microsoft.EntityFrameworkCore.Sqlite` con `Npgsql.EntityFrameworkCore.PostgreSQL`.

La decisione è motivata da tre limiti strutturali dell'attuale setup:

1. `EnsureCreated()` non consente aggiornamenti incrementali dello schema — qualsiasi modifica
   richiede di eliminare e ricreare il DB, con perdita di dati.
2. SQLite serializza le scritture a livello di file: scenari con più utenti che creano
   preventivi o ordini in parallelo generano contesa.
3. Il file `cavagna.db` non è compatibile con deployment su infrastrutture stateless
   (container senza volume persistente, PaaS).

Le modifiche necessarie sono localizzate e non toccano il dominio applicativo:

- **`CavagnaDemo.csproj`**: sostituire `Microsoft.EntityFrameworkCore.Sqlite` con
  `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **`Program.cs`**: sostituire `UseSqlite()` con `UseNpgsql()` e rimuovere `EnsureCreated()`
  in favore di `MigrateAsync()`.
- **`appsettings.json`**: aggiornare la connection string al formato Npgsql
  (`Host=...;Database=cavagna;Username=...;Password=...`).
- Generare la migration iniziale con `dotnet ef migrations add InitialCreate`.
- Correggere `Product.Prezzo` da `float` a `decimal`, allineando lo schema ai campi
  finanziari già presenti su `Quote` e `Order`.

`CavagnaDbContext` e `SeedData.cs` non richiedono modifiche strutturali: operano tramite
EF Core e sono provider-agnostici.

---

## Consequences

**Positive:**

- **Schema versionato:** le EF Core Migrations consentono aggiornamenti incrementali dello
  schema senza perdita di dati, tracciati in git insieme al codice.
- **Concorrenza reale:** PostgreSQL gestisce scritture concorrenti con MVCC (Multi-Version
  Concurrency Control), eliminando il collo di bottiglia di SQLite in scenari multi-utente.
- **Compatibilità con infrastrutture moderne:** il DB è un processo separato, accessibile
  da container, PaaS e ambienti distribuiti senza volumi locali.
- **Coerenza dei tipi finanziari:** la correzione di `Product.Prezzo` da `float` a `decimal`
  allinea tutto lo schema monetario a un tipo preciso.
- **Funzionalità avanzate accessibili:** replica, backup point-in-time, full-text search,
  tipi JSON nativi — non necessari ora, ma disponibili senza cambiare architettura.

**Negative / Vincoli:**

- **Dipendenza infrastrutturale:** serve un server PostgreSQL attivo per avviare
  l'applicazione — in sviluppo locale tipicamente via Docker
  (`docker run --name cavagna-pg -e POSTGRES_PASSWORD=... -p 5432:5432 postgres`).
- **Modifica a `Program.cs` vincolata:** la rimozione di `EnsureCreated()` e l'aggiunta di
  `MigrateAsync()` ricadono nelle modifiche che richiedono conferma esplicita del team
  (CLAUDE.md).
- **Gestione delle migration:** le migration vanno generate, applicate e manutenute. Un
  `dotnet ef migrations add` errato può richiedere rollback manuali.
- **Differenze di comportamento tra provider:** i confronti di stringhe sono case-sensitive
  su PostgreSQL per default, case-insensitive su SQLite — i test esistenti vanno verificati.
- **`SeedData.cs` da rivedere:** attualmente chiamato a ogni avvio, con le migration è
  preferibile condizionare il seed o spostarlo in una migration dedicata per evitare
  conflitti con dati già presenti.
