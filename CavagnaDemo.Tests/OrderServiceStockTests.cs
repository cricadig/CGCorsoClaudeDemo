using CavagnaDemo.Data;
using CavagnaDemo.Models;
using CavagnaDemo.Services;
using Microsoft.EntityFrameworkCore;

namespace CavagnaDemo.Tests;

public class OrderServiceStockTests
{
    private CavagnaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CavagnaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CavagnaDbContext(options);
    }

    [Fact]
    public async Task CreateOrder_ScalaStockDelProdotto()
    {
        // Arrange
        var db = CreateDb();
        var product = new Product { Codice = "RG-ARG-200", Nome = "Riduttore argon", Prezzo = 145f, Stock = 22, Attivo = true };
        db.Products.Add(product);
        var customer = new Customer { RagioneSociale = "GasTech SpA", PartitaIVA = "09876543210", Email = "ordini@gastech.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var service = new OrderService(db);
        var items = new List<OrderItem>
        {
            new() { ProductId = product.Id, Quantity = 5, UnitPrice = 145m }
        };

        // Act
        await service.CreateOrder(customer.Id, items);

        // Assert: lo stock deve essere sceso di 5
        var updated = await db.Products.FindAsync(product.Id);
        Assert.Equal(17, updated!.Stock);
    }

    [Fact]
    public async Task CreateOrder_RifiutaSeQuantitaSuperaStock()
    {
        // Arrange
        var db = CreateDb();
        var product = new Product { Codice = "RG-ARG-200", Nome = "Riduttore argon", Prezzo = 145f, Stock = 5, Attivo = true };
        db.Products.Add(product);
        var customer = new Customer { RagioneSociale = "GasTech SpA", PartitaIVA = "09876543210", Email = "ordini@gastech.com" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var service = new OrderService(db);
        var items = new List<OrderItem>
        {
            new() { ProductId = product.Id, Quantity = 1000, UnitPrice = 145m }
        };

        // Act & Assert: deve lanciare eccezione perché 1000 > 5
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrder(customer.Id, items)
        );
    }
}
