# ADR 0002 — Adozione di PostgreSQL in alternativa a SQLite

**Data:** 2026-06-17  
**Stato:** Proposed

---

## Context

Il progetto usa attualmente SQLite con `EnsureCreated()` (vedi [ADR 0001](0001-db-sqlite.md)).
Questa scelta è adeguata per il contesto dimostrativo attuale, ma presenta limiti strutturali
se il progetto evolvesse verso un uso produttivo:

- `EnsureCreated()` non supporta aggiornamenti incrementali dello schema: qualsiasi modifica
  richiede di eliminare e ricreare il DB, perdendo i dati.
- SQLite serializza le scritture: scenari multi-utente con operazioni concorrenti
  (più agenti che creano preventivi o ordini contemporaneamente) diventano un collo di bottiglia.
- Il file `cavagna.db` risiede nel filesystem locale del processo: non è compatibile
  con deployment su infrastrutture stateless (container senza volume persistente, PaaS).
- Non esistono oggi requisiti di replica, failover o backup automatico — ma l'assenza
  di un DB server rende queste funzionalità inaccessibili senza cambiare architettura.

I service layer (`QuoteService`, `ProductService`, `OrderService`) accedono al DB
esclusivamente tramite `CavagnaDbContext`, quindi il punto di sostituzione del provider
è circoscritto a `Program.cs` e al file `.csproj`.

---

## Decision

Adottare **PostgreSQL** come database sostituendo il provider SQLite con
`Npgsql.EntityFrameworkCore.PostgreSQL`.

Le modifiche necessarie sono minime e localizzate:

1. Nel `.csproj`: sostituire `Microsoft.EntityFrameworkCore.Sqlite` con
   `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. In `Program.cs`: sostituire `UseSqlite(connectionString)` con `UseNpgsql(connectionString)`.
3. In `appsettings.json`: aggiornare la connection string da
   `Data Source=cavagna.db` al formato PostgreSQL
   (`Host=localhost;Database=cavagna;Username=...;Password=...`).
4. Rimuovere `EnsureCreated()` da `Program.cs` e introdurre le EF Core Migrations
   (`dotnet ef migrations add InitialCreate` + `dotnet ef database update`).

`SeedData.cs` non richiede modifiche: opera tramite EF Core e rimane compatibile
con qualsiasi provider relazionale.

`CavagnaDbContext` non richiede modifiche: non contiene configurazioni specifiche SQLite.

**Nota:** Npgsql richiede che il database PostgreSQL esista già prima di
poter creare lo schema — a differenza di SQLite che crea il file da zero.
Il DB va quindi creato manualmente o tramite script di provisioning prima
del primo `dotnet ef database update`.

---

## Consequences

**Positive:**

- **Schema versionato:** le EF Core Migrations consentono aggiornamenti incrementali
  dello schema senza perdita di dati, tracciati in git insieme al codice.
- **Concorrenza reale:** PostgreSQL gestisce scritture concorrenti con MVCC
  (Multi-Version Concurrency Control), eliminando il collo di bottiglia di SQLite
  in scenari multi-utente.
- **Compatibilità con infrastrutture moderne:** il DB è un processo separato,
  accessibile da container, PaaS e ambienti distribuiti senza volumi locali.
- **Funzionalità avanzate disponibili:** replica, backup point-in-time, full-text search,
  tipi JSON nativi — non necessari ora, ma accessibili senza cambiare architettura.
- **Impatto sul codice applicativo minimo:** i service, i modelli e il `DbContext`
  non richiedono modifiche grazie all'astrazione EF Core.

**Negative / Vincoli:**

- **Dipendenza infrastrutturale:** l'applicazione non è più autonoma. Serve un server
  PostgreSQL attivo per avviarla — in sviluppo tipicamente tramite Docker
  (`docker run --name cavagna-pg -e POSTGRES_PASSWORD=... -p 5432:5432 postgres`).
- **Setup iniziale più complesso:** sparisce il "avvia e funziona" di SQLite.
  Ogni sviluppatore deve configurare un'istanza PostgreSQL e gestire le credenziali.
- **Gestione delle migration:** le migration vanno generate, applicate e manutenute.
  Un `dotnet ef migrations add` sbagliato può richiedere rollback manuali.
- **`SeedData.cs` da rivedere:** attualmente `SeedData.Initialize()` è chiamato
  a ogni avvio. Con le migration è preferibile spostare il seed in una migration
  dedicata o in un meccanismo condizionale, per evitare conflitti con dati già presenti.
- **Differenze di comportamento tra provider:** alcune query LINQ si traducono
  diversamente su PostgreSQL rispetto a SQLite (es. confronti di stringhe case-sensitive
  su Postgres per default, case-insensitive su SQLite). Vanno verificati i test esistenti.
