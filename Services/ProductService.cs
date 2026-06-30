using CavagnaDemo.Data;
using CavagnaDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CavagnaDemo.Services;

public class ProductService
{
    private readonly CavagnaDbContext _db;

    public ProductService(CavagnaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllProducts()
    {
        return await _db.Products.Include(p => p.Category).ToListAsync();
    }

    public Product? RetrieveById(int id)
    {
        return _db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
    }

    public async Task<List<Product>> LoadActiveProducts()
    {
        return await _db.Products.Where(p => p.Attivo).ToListAsync();
    }

    public async Task<List<Product>> GetProductsForQuote(List<int> productIds)
    {
        return await _db.Products
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
    }

    public bool CheckStockForOrder(int productId, int requestedQty)
    {
        var product = _db.Products.Find(productId);
        if (product == null) return false;
        return product.Stock >= requestedQty;
    }

    public async Task<Product> CreateProduct(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateProduct(Product product)
    {
        var existing = await _db.Products.FindAsync(product.Id);
        if (existing == null) return false;
        _db.Entry(existing).State = EntityState.Detached;
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return false;
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Product>> SearchByName(string query)
    {
        return await _db.Products
            .Where(p => p.Nome.Contains(query))
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsBelowStock(int threshold)
    {
        return await _db.Products
            .Include(p => p.Category)
            .Where(p => p.Attivo && p.Stock < threshold)
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }
}
