using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TooliRent.Infrastructure.Auth;

public class AuthDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) {}
    
    public DbSet<Models.RefreshToken> RefreshTokens => Set<Models.RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        
        b.Entity<Models.RefreshToken>(e =>
        {
            e.HasIndex(r => r.UserId);
            e.HasIndex(r => r.TokenHash).IsUnique();
        });
    }
}