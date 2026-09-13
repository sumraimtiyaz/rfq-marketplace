using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<Quotation> Quotations => Set<Quotation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.Name).IsRequired().HasMaxLength(120);
            e.Property(u => u.CompanyName).IsRequired().HasMaxLength(160);
        });

        builder.Entity<Rfq>(e =>
        {
            e.Property(r => r.ProductName).IsRequired().HasMaxLength(200);
            e.Property(r => r.Description).IsRequired().HasMaxLength(5000);
            e.Property(r => r.DeliveryLocation).IsRequired().HasMaxLength(200);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Last line of defence behind FluentValidation: the database itself refuses bad data.
            e.ToTable(t => t.HasCheckConstraint("CK_Rfqs_Quantity_Positive", "\"Quantity\" > 0"));

            e.HasOne(r => r.Buyer)
                .WithMany(u => u.Rfqs)
                .HasForeignKey(r => r.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.BuyerId);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.Deadline);
        });

        builder.Entity<Quotation>(e =>
        {
            e.Property(q => q.QuotedPrice).HasPrecision(18, 2);
            e.Property(q => q.Message).HasMaxLength(2000);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Quotations_Price_Positive", "\"QuotedPrice\" > 0");
                t.HasCheckConstraint("CK_Quotations_Delivery_Positive", "\"EstimatedDeliveryDays\" > 0");
            });

            e.HasOne(q => q.Rfq)
                .WithMany(r => r.Quotations)
                .HasForeignKey(q => q.RfqId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(q => q.Supplier)
                .WithMany(u => u.Quotations)
                .HasForeignKey(q => q.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            // One quotation per supplier per RFQ. Enforced here so two concurrent
            // submissions cannot both win the "does one already exist?" race.
            e.HasIndex(q => new { q.RfqId, q.SupplierId }).IsUnique();
        });
    }
}
