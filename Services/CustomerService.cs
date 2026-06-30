using CavagnaDemo.Data;
using CavagnaDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CavagnaDemo.Services;

public class CustomerService
{
    private readonly CavagnaDbContext _db;

    public CustomerService(CavagnaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _db.Customers.ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _db.Customers
            .Include(c => c.Orders)
            .Include(c => c.Quotes)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.DataRegistrazione = DateTime.Now;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> FindByPartitaIVAAsync(string piva)
    {
        return await _db.Customers.FirstOrDefaultAsync(c => c.PartitaIVA == piva);
    }
}
