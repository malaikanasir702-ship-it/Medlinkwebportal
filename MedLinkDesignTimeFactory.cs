using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MedLinkPortal.Models;
using MedLinkPortal.Services;

namespace MedLinkPortal;

/// <summary>
/// Used by EF Core tools (migrations) at design time.
/// </summary>
public class MedLinkDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=db.oaigzdrntnsyacceveno.supabase.co;Database=postgres;Username=postgres;Password=Ayesha@12349094;Port=5432;SSL Mode=Require;Trust Server Certificate=true";

        optionsBuilder.UseNpgsql(connStr);

        // Provide a no-op encryption service for design-time use
        return new ApplicationDbContext(optionsBuilder.Options, new NoOpEncryptionService());
    }
}

/// <summary>Design-time stub — no actual encryption needed for migrations.</summary>
internal class NoOpEncryptionService : IEncryptionService
{
    public string Encrypt(string plainText) => plainText;
    public string Decrypt(string cipherText) => cipherText;
}
