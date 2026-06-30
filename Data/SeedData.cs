using CavagnaDemo.Models;

namespace CavagnaDemo.Data;

public static class SeedData
{
    public static void Initialize(CavagnaDbContext db)
    {
        if (db.Products.Any()) return;

        var cats = new[]
        {
            new Category { Name = "Regolatori di pressione", Description = "Regolatori per gas tecnici e GPL" },
            new Category { Name = "Valvole", Description = "Valvole industriali per gas" },
            new Category { Name = "Raccordi", Description = "Raccordi e adattatori in ottone" },
            new Category { Name = "Bombole", Description = "Bombole e accessori" }
        };
        db.Categories.AddRange(cats);
        db.SaveChanges();

        var products = new List<Product>
        {
            new() { Codice = "RG-25-3B", Nome = "Regolatore GPL 3 bar", Descrizione = "Regolatore di pressione GPL 1.5 kg/h, uscita 30 mbar", Prezzo = 24.50f, Stock = 120, CategoryId = cats[0].Id, Attivo = true },
            new() { Codice = "RG-50-5B", Nome = "Regolatore alta portata 5 bar", Descrizione = "Regolatore industriale 8 kg/h uscita regolabile", Prezzo = 89.90f, Stock = 35, CategoryId = cats[0].Id, Attivo = true },
            new() { Codice = "RG-MET-15", Nome = "Regolatore metano 1.5 bar", Descrizione = "Regolatore per metano uso civile", Prezzo = 32.00f, Stock = 200, CategoryId = cats[0].Id, Attivo = true },
            new() { Codice = "RG-ARG-200", Nome = "Riduttore argon 200 bar", Descrizione = "Per saldatura TIG/MIG, manometri inclusi", Prezzo = 145.00f, Stock = 22, CategoryId = cats[0].Id, Attivo = true },

            new() { Codice = "VL-SF-12", Nome = "Valvola a sfera 1/2\"", Descrizione = "Valvola in ottone PN16, attacco maschio-femmina", Prezzo = 8.40f, Stock = 500, CategoryId = cats[1].Id, Attivo = true },
            new() { Codice = "VL-SF-34", Nome = "Valvola a sfera 3/4\"", Descrizione = "Valvola in ottone PN16, attacco maschio-femmina", Prezzo = 12.10f, Stock = 380, CategoryId = cats[1].Id, Attivo = true },
            new() { Codice = "VL-SIC-30", Nome = "Valvola di sicurezza 30 mbar", Descrizione = "Per impianti GPL domestici", Prezzo = 18.50f, Stock = 90, CategoryId = cats[1].Id, Attivo = true },
            new() { Codice = "VL-INT-OBS", Nome = "Valvola intercettazione (obsoleta)", Descrizione = "Modello superato, vendita esaurimento", Prezzo = 5.00f, Stock = 0, CategoryId = cats[1].Id, Attivo = false },

            new() { Codice = "RC-NIPL-12", Nome = "Nipplo ottone 1/2\"", Descrizione = "Raccordo doppio maschio in ottone", Prezzo = 1.80f, Stock = 1500, CategoryId = cats[2].Id, Attivo = true },
            new() { Codice = "RC-CURV-90", Nome = "Curva 90° ottone", Descrizione = "Curva 90° femmina-femmina", Prezzo = 3.20f, Stock = 800, CategoryId = cats[2].Id, Attivo = true },
            new() { Codice = "RC-TEE-34", Nome = "Raccordo a T 3/4\"", Descrizione = "Raccordo a tre vie in ottone", Prezzo = 4.50f, Stock = 420, CategoryId = cats[2].Id, Attivo = true },

            new() { Codice = "BMB-10KG", Nome = "Bombola GPL 10 kg vuota", Descrizione = "Bombola in acciaio omologata", Prezzo = 65.00f, Stock = 75, CategoryId = cats[3].Id, Attivo = true },
            new() { Codice = "BMB-CAP-27", Nome = "Cappellotto bombola 27mm", Descrizione = "Cappellotto protettivo per bombole industriali", Prezzo = 2.10f, Stock = 300, CategoryId = cats[3].Id, Attivo = true }
        };
        db.Products.AddRange(products);
        db.SaveChanges();

        var customers = new[]
        {
            new Customer { RagioneSociale = "Officina Rossi SRL", PartitaIVA = "01234567890", Email = "info@officinarossi.it", Telefono = "0331123456", Indirizzo = "Via Garibaldi 12", Citta = "Busto Arsizio", CAP = "21052", Nazione = "Italia" },
            new Customer { RagioneSociale = "GasTech SpA", PartitaIVA = "09876543210", Email = "ordini@gastech.com", Telefono = "0264789123", Indirizzo = "Via Industriale 45", Citta = "Milano", CAP = "20100", Nazione = "Italia" },
            new Customer { RagioneSociale = "Bianchi Impianti", PartitaIVA = "11223344556", Email = "bianchi@impianti.it", Indirizzo = "Corso Italia 8", Citta = "Varese", CAP = "21100", Nazione = "Italia" }
        };
        db.Customers.AddRange(customers);
        db.SaveChanges();
    }
}
