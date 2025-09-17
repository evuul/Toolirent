using Microsoft.AspNetCore.Identity;
using TooliRent.Infrastructure.Auth;
using TooliRent.Infrastructure.Data;
using TooliRent.Core.Models;

namespace TooliRent.WebAPI.IdentitySeed;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var cfg        = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var roleMgr    = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr    = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var domainDb   = scope.ServiceProvider.GetRequiredService<TooliRentDbContext>();   // domänen
        var authDb     = scope.ServiceProvider.GetRequiredService<AuthDbContext>();        // säkerställ att auth-db finns

        // Se till att databaserna finns (om du kör EnsureCreated i dev; migrations är annars att föredra)
        // await authDb.Database.EnsureCreatedAsync();
        // await domainDb.Database.EnsureCreatedAsync();

        // 1) Roller
        string[] roles = { "Admin", "Member" };
        foreach (var role in roles)
        {
            if (!await roleMgr.RoleExistsAsync(role))
            {
                var r = await roleMgr.CreateAsync(new IdentityRole(role));
                if (!r.Succeeded)
                    throw new Exception("Failed creating role '" + role + "': " + string.Join("; ", r.Errors.Select(e => e.Description)));
            }
        }

        // 2) Admin user (från appsettings)
        var adminEmail = cfg["Seed:AdminEmail"]    ?? "admin@toolirent.local";
        var adminPass  = cfg["Seed:AdminPassword"] ?? "Admin123!";

        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName        = adminEmail,
                Email           = adminEmail,
                EmailConfirmed  = true
            };

            var create = await userMgr.CreateAsync(admin, adminPass);
            if (!create.Succeeded)
                throw new Exception("Failed creating admin user: " + string.Join("; ", create.Errors.Select(e => e.Description)));
        }

        // 3) Sätt Admin-roll
        if (!await userMgr.IsInRoleAsync(admin, "Admin"))
        {
            var addRole = await userMgr.AddToRoleAsync(admin, "Admin");
            if (!addRole.Succeeded)
                throw new Exception("Failed adding admin to Admin role: " + string.Join("; ", addRole.Errors.Select(e => e.Description)));
        }

        // 4) (Valfritt) Skapa domänmedlem kopplad till admin
        // Styr via appsettings: "Seed:CreateAdminMember": true/false
        var createAdminMember = bool.TryParse(cfg["Seed:CreateAdminMember"], out var flag) && flag;
        if (createAdminMember)
        {
            // Finns redan en Member med denna IdentityUserId?
            var exists = domainDb.Members.Any(m => m.IdentityUserId == admin.Id);
            if (!exists)
            {
                var name = cfg["Seed:AdminMemberName"] ?? "System Admin";
                var first = name.Split(' ').FirstOrDefault() ?? "System";
                var last  = string.Join(' ', name.Split(' ').Skip(1));
                domainDb.Members.Add(new Member
                {
                    FirstName      = first,
                    LastName       = string.IsNullOrWhiteSpace(last) ? "Admin" : last,
                    Email          = adminEmail,
                    IdentityUserId = admin.Id
                });
                await domainDb.SaveChangesAsync();
            }
        }

        // 5) (Valfritt) Seed en demouser med Member-roll
        var seedMember = bool.TryParse(cfg["Seed:CreateDemoMemberUser"], out var flag2) && flag2;
        if (seedMember)
        {
            var memberEmail = cfg["Seed:MemberEmail"]    ?? "member@toolirent.local";
            var memberPass  = cfg["Seed:MemberPassword"] ?? "Member123!";
            var memberUser  = await userMgr.FindByEmailAsync(memberEmail);

            if (memberUser is null)
            {
                memberUser = new IdentityUser
                {
                    UserName       = memberEmail,
                    Email          = memberEmail,
                    EmailConfirmed = true
                };
                var create = await userMgr.CreateAsync(memberUser, memberPass);
                if (!create.Succeeded)
                    throw new Exception("Failed creating demo member: " + string.Join("; ", create.Errors.Select(e => e.Description)));
            }

            if (!await userMgr.IsInRoleAsync(memberUser, "Member"))
            {
                var add = await userMgr.AddToRoleAsync(memberUser, "Member");
                if (!add.Succeeded)
                    throw new Exception("Failed adding demo member to Member role: " + string.Join("; ", add.Errors.Select(e => e.Description)));
            }

            // Skapa domain Member om saknas
            var dmExists = domainDb.Members.Any(m => m.IdentityUserId == memberUser.Id);
            if (!dmExists)
            {
                domainDb.Members.Add(new Member
                {
                    FirstName      = "Demo",
                    LastName       = "Member",
                    Email          = memberEmail,
                    IdentityUserId = memberUser.Id
                });
                await domainDb.SaveChangesAsync();
            }
        }
    }
}