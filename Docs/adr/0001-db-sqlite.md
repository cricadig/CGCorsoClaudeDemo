# ADR 0001 — Database: SQLite con EnsureCreated

**Data:** 2026-06-17  
**Stato:** Accepted

---

## Context

Il progetto è un gestionale web ASP.NET Core 8.0 per il settore gas industriale.
Gestisce un catalogo prodotti, un'anagrafica clienti, preventivi e ordini.

Il volume di dati atteso è limitato: al primo avvio il DB viene popolato con
4 categorie, 13 prodotti e 3 clienti (`Data/SeedData.cs`).
Non sono presenti requisiti di accesso concorrente multi-utente ad alto carico,
replica o failover.

Lo schema viene creato tramite `EnsureCreated()` in `Program.cs` all'avvio dell'applicazione.
Questo approccio non è compatibile con le migration EF Core: se si introduce
`dotnet ef migrations add`, occorre rimuovere prima `EnsureCreated()`.
La connection string (`Data Source=cavagna.db`) punta a un file locale nella
directory di lavoro, configurata in `appsettings.json`.

---

## Decision

> SQLite è stato scelto come database per questo progetto dimostrativo perché elimina qualsiasi dipendenza infrastrutturale esterna: l'applicazione è autonoma, avviabile con `dotnet run` senza installare né configurare un server DB.
>
> `EnsureCreated()` è stato preferito alle EF Core Migrations perché in un contesto demo lo schema è stabile e la priorità è la semplicità di setup, non la gestione di aggiornamenti incrementali in produzione.
>
> Questa scelta è consapevolmente limitata al contesto attuale: se il progetto evolvesse verso un deployment multi-utente o un ambiente produttivo, SQLite andrebbe sostituito con un DB server e `EnsureCreated()` rimosso in favore delle migration.

---

## Consequences

**Positive:**

- Zero configurazione infrastrutturale: il file `cavagna.db` viene creato automaticamente al primo avvio, senza installare né configurare un server DB esterno.
- Ambiente di sviluppo e test riproducibile: per azzerare lo stato è sufficiente eliminare `cavagna.db` e riavviare.
- I dati di seed in `SeedData.cs` garantiscono uno stato iniziale coerente per sviluppo e test manuali.

**Negative / Vincoli:**

- **Incompatibile con EF Core Migrations:** `EnsureCreated()` crea lo schema da zero ma non supporta aggiornamenti incrementali. Qualsiasi modifica allo schema richiede di eliminare e ricreare il DB, perdendo i dati.
- **Nessuna concorrenza in scrittura:** SQLite serializza le scritture; scenari multi-utente con scritture frequenti e simultanee possono diventare un collo di bottiglia.
- **File locale:** il DB risiede nel filesystem del server. Non è compatibile con deployment su infrastrutture stateless (container senza volume persistente, PaaS) senza configurazione aggiuntiva.
- **Migrazione futura costosa:** passare a un DB server (es. SQL Server, PostgreSQL) richiede di rimuovere `EnsureCreated()`, introdurre le migration e riscrivere la connection string — non è una modifica trasparente.
