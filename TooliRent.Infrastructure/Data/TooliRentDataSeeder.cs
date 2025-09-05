using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TooliRent.Core.Models;

namespace TooliRent.Infrastructure.Data;

public static class TooliRentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TooliRentDbContext>();

        await ctx.Database.MigrateAsync();

        var now = DateTime.UtcNow;

        // Fixed ids så vi kan köra om seedern utan dubletter
        var catHandId = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        var catElId   = Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

        var toolHammerId = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111");
        var toolDrillId  = Guid.Parse("22222222-bbbb-bbbb-bbbb-222222222222");
        var toolSawId    = Guid.Parse("33333333-cccc-cccc-cccc-333333333333");

        // --- Categories ---
        if (!await ctx.ToolCategories.AnyAsync(c => c.Id == catHandId))
        {
            ctx.ToolCategories.Add(new ToolCategory { Id = catHandId, Name = "Handverktyg", CreatedAtUtc = now });
        }
        if (!await ctx.ToolCategories.AnyAsync(c => c.Id == catElId))
        {
            ctx.ToolCategories.Add(new ToolCategory { Id = catElId, Name = "Elverktyg", CreatedAtUtc = now });
        }
        await ctx.SaveChangesAsync();

        // --- Tools ---
        if (!await ctx.Tools.AnyAsync(t => t.Id == toolHammerId))
        {
            ctx.Tools.Add(new Tool
            {
                Id = toolHammerId,
                Name = "Hammare",
                Description = "Standard hammare för snickeri",
                RentalPricePerDay = 25,
                CategoryId = catHandId,
                IsAvailable = true,
                CreatedAtUtc = now
            });
        }
        if (!await ctx.Tools.AnyAsync(t => t.Id == toolDrillId))
        {
            ctx.Tools.Add(new Tool
            {
                Id = toolDrillId,
                Name = "Borrmaskin",
                Description = "Slagborrmaskin 18V",
                RentalPricePerDay = 120,
                CategoryId = catElId,
                IsAvailable = true,
                CreatedAtUtc = now
            });
        }
        if (!await ctx.Tools.AnyAsync(t => t.Id == toolSawId))
        {
            ctx.Tools.Add(new Tool
            {
                Id = toolSawId,
                Name = "Tigersåg",
                Description = "El-tigersåg för grovkapning",
                RentalPricePerDay = 150,
                CategoryId = catElId,
                IsAvailable = true,
                CreatedAtUtc = now
            });
        }
        await ctx.SaveChangesAsync();

        // --- Members (identifiera via email) ---
        if (!await ctx.Members.AnyAsync(m => m.Email == "egzon@example.com"))
        {
            ctx.Members.Add(new Member
            {
                Id = Guid.Parse("44444444-dddd-dddd-dddd-444444444444"),
                FirstName = "Egzon",
                LastName = "Demo",
                Email = "egzon@example.com",
                CreatedAtUtc = now
            });
        }
        if (!await ctx.Members.AnyAsync(m => m.Email == "alexander@example.com"))
        {
            ctx.Members.Add(new Member
            {
                Id = Guid.Parse("55555555-eeee-eeee-eeee-555555555555"),
                FirstName = "Alexander",
                LastName = "Demo",
                Email = "alexander@example.com",
                CreatedAtUtc = now
            });
        }
        await ctx.SaveChangesAsync();
    }
}