using CavagnaDemo.Data;
using CavagnaDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CavagnaDemo.Services;

public class CategoryService
{
    private readonly CavagnaDbContext _db;

    public CategoryService(CavagnaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAll()
    {
        return await _db.Categories.ToListAsync();
    }

    public async Task<Category?> GetById(int id)
    {
        return await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category> Create(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> Update(Category category)
    {
        var existing = await _db.Categories.FindAsync(category.Id);
        if (existing == null) return false;
        existing.Name = category.Name;
        existing.Description = category.Description;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return false;
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }
}
