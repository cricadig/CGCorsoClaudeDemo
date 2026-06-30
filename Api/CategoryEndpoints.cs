using CavagnaDemo.Models;
using CavagnaDemo.Services;

namespace CavagnaDemo.Api;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", async (CategoryService svc) =>
            Results.Ok(await svc.GetAll()));

        group.MapGet("/{id:int}", async (int id, CategoryService svc) =>
        {
            var item = await svc.GetById(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async (Category category, CategoryService svc) =>
        {
            var created = await svc.Create(category);
            return Results.Created($"/api/categories/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, Category category, CategoryService svc) =>
        {
            category.Id = id;
            var ok = await svc.Update(category);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (int id, CategoryService svc) =>
        {
            var ok = await svc.Delete(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}
