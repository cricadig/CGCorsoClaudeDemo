using CavagnaDemo.Api;
using CavagnaDemo.Data;
using CavagnaDemo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// in più della stessa che è già in appsettings.json
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=cavagna.db";

builder.Services.AddDbContext<CavagnaDbContext>(opt =>
    opt.UseSqlite(connectionString));

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CategoryService>();

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Crea il DB e seed se non esiste
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CavagnaDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

// Minimal API
app.MapProductEndpoints();
app.MapQuoteEndpoints();
app.MapOrderEndpoints();
app.MapCategoryEndpoints();

// MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Rende la classe Program visibile al progetto di test
public partial class Program { }
