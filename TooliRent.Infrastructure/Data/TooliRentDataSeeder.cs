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

        // Kör migrationer
        await ctx.Database.MigrateAsync();

        // Kolla om det redan finns data
        if (await ctx.ToolCategories.AnyAsync() || await ctx.Tools.AnyAsync() || await ctx.Members.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // --- Categories ---
        var catHand = new ToolCategory { Id = Guid.NewGuid(), Name = "Handverktyg", CreatedAtUtc = now };
        var catEl   = new ToolCategory { Id = Guid.NewGuid(), Name = "Elverktyg", CreatedAtUtc = now };

        ctx.ToolCategories.AddRange(catHand, catEl);

        // --- Tools ---
        var hammer = new Tool
        {
            Id = Guid.NewGuid(),
            Name = "Hammare",
            Description = "Standard hammare för snickeri",
            RentalPricePerDay = 25,
            CategoryId = catHand.Id,
            CreatedAtUtc = now
        };

        var drill = new Tool
        {
            Id = Guid.NewGuid(),
            Name = "Borrmaskin",
            Description = "Slagborrmaskin 18V",
            RentalPricePerDay = 120,
            CategoryId = catEl.Id,
            CreatedAtUtc = now
        };

        var saw = new Tool
        {
            Id = Guid.NewGuid(),
            Name = "Tigersåg",
            Description = "El-tigersåg för grovkapning",
            RentalPricePerDay = 150,
            CategoryId = catEl.Id,
            CreatedAtUtc = now
        };

        ctx.Tools.AddRange(hammer, drill, saw);

        // --- Members ---
        var egzon = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Egzon",
            LastName = "Demo",
            Email = "egzon@example.com",
            CreatedAtUtc = now
        };

        var alex = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alexander",
            LastName = "Demo",
            Email = "alexander@example.com",
            CreatedAtUtc = now
        };

        ctx.Members.AddRange(egzon, alex);

        await ctx.SaveChangesAsync();
    }
}