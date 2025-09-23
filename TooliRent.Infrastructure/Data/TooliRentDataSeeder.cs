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

        // ---------- CATEGORY IDS ----------
        var catHandId    = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa"); // Handverktyg
        var catElId      = Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb"); // Elverktyg
        var catGardenId  = Guid.Parse("cccccccc-3333-3333-3333-cccccccccccc"); // Trädgårdsverktyg
        var catMeasureId = Guid.Parse("dddddddd-4444-4444-4444-dddddddddddd"); // Mätverktyg
        var catPaintId   = Guid.Parse("eeeeeeee-5555-5555-5555-eeeeeeeeeeee"); // Måleri
        var catSafetyId  = Guid.Parse("ffffffff-6666-6666-6666-ffffffffffff"); // Skyddsutrustning

        // ---------- Kategorier ----------
        await AddCategoryIfMissing(ctx, catHandId,    "Handverktyg",      now);
        await AddCategoryIfMissing(ctx, catElId,      "Elverktyg",        now);
        await AddCategoryIfMissing(ctx, catGardenId,  "Trädgårdsverktyg", now);
        await AddCategoryIfMissing(ctx, catMeasureId, "Mätverktyg",       now);
        await AddCategoryIfMissing(ctx, catPaintId,   "Måleri",           now);
        await AddCategoryIfMissing(ctx, catSafetyId,  "Skyddsutrustning", now);

        // ---------- Verktyg (5 per kategori) ----------

        // Handverktyg
        await AddToolIfMissing(ctx, Guid.Parse("10000000-aaaa-aaaa-aaaa-100000000001"), "Hammare",        "Standard hammare",            25m,  catHandId,    now);
        await AddToolIfMissing(ctx, Guid.Parse("10000000-aaaa-aaaa-aaaa-100000000002"), "Skiftnyckel",    "Justerbar 200 mm",            20m,  catHandId,    now);
        await AddToolIfMissing(ctx, Guid.Parse("10000000-aaaa-aaaa-aaaa-100000000003"), "Skruvmejsel",    "Kryssmejsel PH2",             10m,  catHandId,    now);
        await AddToolIfMissing(ctx, Guid.Parse("10000000-aaaa-aaaa-aaaa-100000000004"), "Tång",           "Kombinationstång 180 mm",     15m,  catHandId,    now);
        await AddToolIfMissing(ctx, Guid.Parse("10000000-aaaa-aaaa-aaaa-100000000005"), "Hylsnyckelsats", "12 delar",                    40m,  catHandId,    now);

        // Elverktyg
        await AddToolIfMissing(ctx, Guid.Parse("20000000-bbbb-bbbb-bbbb-200000000001"), "Borrmaskin",   "Slagborrmaskin 18V",             120m, catElId,      now);
        await AddToolIfMissing(ctx, Guid.Parse("20000000-bbbb-bbbb-bbbb-200000000002"), "Tigersåg",     "För grovkapning",                150m, catElId,      now);
        await AddToolIfMissing(ctx, Guid.Parse("20000000-bbbb-bbbb-bbbb-200000000003"), "Cirkelsåg",    "230 mm klinga",                  170m, catElId,      now);
        await AddToolIfMissing(ctx, Guid.Parse("20000000-bbbb-bbbb-bbbb-200000000004"), "Skruvdragare", "Batteridriven 18V",              110m, catElId,      now);
        await AddToolIfMissing(ctx, Guid.Parse("20000000-bbbb-bbbb-bbbb-200000000005"), "Vinkelslip",   "125 mm skiva",                   130m, catElId,      now);

        // Trädgårdsverktyg
        await AddToolIfMissing(ctx, Guid.Parse("30000000-cccc-cccc-cccc-300000000001"), "Häcksax",       "Elektrisk 500W",                 90m,  catGardenId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("30000000-cccc-cccc-cccc-300000000002"), "Lövblås",       "Batteridriven 36V",              110m, catGardenId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("30000000-cccc-cccc-cccc-300000000003"), "Grästrimmer",   "Elektrisk 500W",                 85m,  catGardenId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("30000000-cccc-cccc-cccc-300000000004"), "Gräsklippare",  "Bensindriven",                   200m, catGardenId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("30000000-cccc-cccc-cccc-300000000005"), "Motorsåg",      "Bensindriven 40 cm",             250m, catGardenId,  now);

        // Mätverktyg
        await AddToolIfMissing(ctx, Guid.Parse("40000000-dddd-dddd-dddd-400000000001"), "Laseravståndsmätare", "50 m, ±2 mm",             95m,  catMeasureId, now);
        await AddToolIfMissing(ctx, Guid.Parse("40000000-dddd-dddd-dddd-400000000002"), "Multimeter",          "True RMS",                60m,  catMeasureId, now);
        await AddToolIfMissing(ctx, Guid.Parse("40000000-dddd-dddd-dddd-400000000003"), "Vattenpass",          "600 mm",                  25m,  catMeasureId, now);
        await AddToolIfMissing(ctx, Guid.Parse("40000000-dddd-dddd-dddd-400000000004"), "Skjutmått",           "Digitalt 150 mm",         55m,  catMeasureId, now);
        await AddToolIfMissing(ctx, Guid.Parse("40000000-dddd-dddd-dddd-400000000005"), "Fuktmätare",          "För trä/betong",          80m,  catMeasureId, now);

        // Måleri
        await AddToolIfMissing(ctx, Guid.Parse("50000000-eeee-eeee-eeee-500000000001"), "Färgspruta",   "Lågtryck HVLP",                    130m, catPaintId,   now);
        await AddToolIfMissing(ctx, Guid.Parse("50000000-eeee-eeee-eeee-500000000002"), "Färgblandare", "Blandarstav",                      15m,  catPaintId,   now);
        await AddToolIfMissing(ctx, Guid.Parse("50000000-eeee-eeee-eeee-500000000003"), "Penselsats",   "5 olika storlekar",                12m,  catPaintId,   now);
        await AddToolIfMissing(ctx, Guid.Parse("50000000-eeee-eeee-eeee-500000000004"), "Rollerset",    "Med bricka",                       20m,  catPaintId,   now);
        await AddToolIfMissing(ctx, Guid.Parse("50000000-eeee-eeee-eeee-500000000005"), "Tapetbord",    "3 m",                              60m,  catPaintId,   now);

        // Skyddsutrustning
        await AddToolIfMissing(ctx, Guid.Parse("60000000-ffff-ffff-ffff-600000000001"), "Skyddshjälm",   "Med visir",                        10m,  catSafetyId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("60000000-ffff-ffff-ffff-600000000002"), "Skyddsglasögon","Imskyddade",                        8m,  catSafetyId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("60000000-ffff-ffff-ffff-600000000003"), "Hörselkåpor",   "SNR 30 dB",                         12m,  catSafetyId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("60000000-ffff-ffff-ffff-600000000004"), "Arbetshandskar","Läder, strl 10",                    6m,   catSafetyId,  now);
        await AddToolIfMissing(ctx, Guid.Parse("60000000-ffff-ffff-ffff-600000000005"), "Knäskydd",      "För golvläggning",                  15m,  catSafetyId,  now);

        await ctx.SaveChangesAsync();
    }

    // ---------------- Helpers ----------------
    private static async Task AddCategoryIfMissing(TooliRentDbContext ctx, Guid id, string name, DateTime now)
    {
        if (!await ctx.ToolCategories.AnyAsync(c => c.Id == id))
        {
            ctx.ToolCategories.Add(new ToolCategory
            {
                Id = id,
                Name = name,
                CreatedAtUtc = now
            });
            await ctx.SaveChangesAsync();
        }
    }

    private static async Task AddToolIfMissing(
        TooliRentDbContext ctx,
        Guid id,
        string name,
        string description,
        decimal pricePerDay,
        Guid categoryId,
        DateTime now,
        bool isAvailable = true)
    {
        if (!await ctx.Tools.AnyAsync(t => t.Id == id))
        {
            ctx.Tools.Add(new Tool
            {
                Id = id,
                Name = name,
                Description = description,
                RentalPricePerDay = pricePerDay,
                CategoryId = categoryId,
                IsAvailable = isAvailable,
                CreatedAtUtc = now
                // Status lämnas till default (ToolStatus.Available)
            });
            await ctx.SaveChangesAsync();
        }
    }
}