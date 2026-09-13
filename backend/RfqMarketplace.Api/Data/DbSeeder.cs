using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.Models;

namespace RfqMarketplace.Api.Data;

/// <summary>
/// Ensures the two roles exist, then - only when the marketplace is completely empty - loads a
/// small demo dataset so an evaluator can sign in and see a populated app straight away.
/// Re-running is safe: existing data short-circuits the seed.
/// </summary>
public class DbSeeder(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IConfiguration configuration,
    ILogger<DbSeeder> logger)
{
    private const string DemoPassword = "Demo@1234";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync();

        if (!configuration.GetBool("Database:SeedDemoData", true))
        {
            logger.LogInformation("Demo data seeding is disabled");
            return;
        }

        if (await db.Rfqs.AnyAsync(ct))
        {
            logger.LogInformation("Database already contains RFQs; skipping demo seed");
            return;
        }

        var buyers = new[]
        {
            await EnsureUserAsync("buyer@demo.com", "Rahul Mehta", "Meridian Office Solutions", UserRole.Buyer),
            await EnsureUserAsync("buyer2@demo.com", "Priya Shah", "Northline Manufacturing", UserRole.Buyer)
        };

        var suppliers = new[]
        {
            await EnsureUserAsync("supplier@demo.com", "Amit Patel", "ABC Furniture Works", UserRole.Supplier),
            await EnsureUserAsync("supplier2@demo.com", "Neha Desai", "XYZ Industrial Supplies", UserRole.Supplier),
            await EnsureUserAsync("supplier3@demo.com", "Karan Joshi", "OfficePro Traders", UserRole.Supplier)
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        var rfqs = new List<Rfq>
        {
            NewRfq(buyers[0], "Ergonomic Office Chairs",
                "Need 500 ergonomic office chairs with adjustable lumbar support, breathable mesh backs and 5-year warranty for a new office floor.",
                500, "Ahmedabad, Gujarat", today.AddDays(21), now.AddDays(-6)),

            NewRfq(buyers[0], "Business Laptops",
                "Dell or HP business-class laptops, 16GB RAM, 512GB SSD, minimum i5 12th gen. On-site warranty required.",
                100, "Vadodara, Gujarat", today.AddDays(14), now.AddDays(-4)),

            NewRfq(buyers[0], "Corrugated Packaging Boxes",
                "5-ply corrugated boxes, 40x30x30 cm, printed with our logo in two colours. Recurring monthly requirement.",
                10000, "Surat, Gujarat", today.AddDays(30), now.AddDays(-2)),

            NewRfq(buyers[1], "Industrial Seamless Steel Pipes",
                "Seamless steel pipes conforming to ASTM A106 Grade B, 6 inch diameter, schedule 40. Mill test certificates required.",
                750, "Vadodara, Gujarat", today.AddDays(18), now.AddDays(-3)),

            NewRfq(buyers[1], "CNC Machined Aluminium Brackets",
                "Custom aluminium 6061-T6 brackets machined to supplied drawings. Tolerance +/- 0.05 mm. Sample approval before bulk.",
                2500, "Rajkot, Gujarat", today.AddDays(25), now.AddDays(-1)),

            NewRfq(buyers[1], "Safety Helmets and PPE Kits",
                "ISI-marked safety helmets, high-visibility jackets and safety shoes for plant staff. Sizes to be confirmed on award.",
                300, "Ahmedabad, Gujarat", today.AddDays(10), now.AddDays(-8))
        };

        // One already-closed RFQ so the closed state is visible in the buyer's list from the start.
        rfqs[5].Status = RfqStatus.Closed;

        db.Rfqs.AddRange(rfqs);

        db.Quotations.AddRange(
            NewQuotation(rfqs[0], suppliers[0], 1_250_000m, 20, "Premium ergonomic chairs with 5-year warranty and free installation."),
            NewQuotation(rfqs[0], suppliers[1], 1_180_000m, 30, "Competitive pricing, free delivery across Gujarat."),
            NewQuotation(rfqs[0], suppliers[2], 1_320_000m, 15, "Top-of-the-range models, fastest lead time available."),
            NewQuotation(rfqs[1], suppliers[1], 6_450_000m, 25, "Dell Latitude 5540 with 3-year on-site warranty."),
            NewQuotation(rfqs[1], suppliers[2], 6_190_000m, 35, "HP ProBook 450 G10, includes docking stations."),
            NewQuotation(rfqs[3], suppliers[1], 3_375_000m, 28, "ASTM A106 Gr B with full mill test certificates."),
            NewQuotation(rfqs[5], suppliers[0], 435_000m, 12, "Complete PPE kits, ISI certified.")
        );

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded {Rfqs} demo RFQs and {Quotations} quotations", rfqs.Count, 7);
        logger.LogInformation("Demo accounts: buyer@demo.com / supplier@demo.com (password {Password})", DemoPassword);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in UserRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(string email, string name, string company, string role)
    {
        if (await userManager.FindByEmailAsync(email) is { } existing)
            return existing;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Name = name,
            CompanyName = company,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Could not seed user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static Rfq NewRfq(
        ApplicationUser buyer, string product, string description,
        int quantity, string location, DateOnly deadline, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        BuyerId = buyer.Id,
        ProductName = product,
        Description = description,
        Quantity = quantity,
        DeliveryLocation = location,
        Deadline = deadline,
        Status = RfqStatus.Open,
        CreatedAt = createdAt,
        UpdatedAt = createdAt
    };

    private static Quotation NewQuotation(
        Rfq rfq, ApplicationUser supplier, decimal price, int days, string message) => new()
    {
        Id = Guid.NewGuid(),
        RfqId = rfq.Id,
        SupplierId = supplier.Id,
        QuotedPrice = price,
        EstimatedDeliveryDays = days,
        Message = message,
        CreatedAt = rfq.CreatedAt.AddHours(6),
        UpdatedAt = rfq.CreatedAt.AddHours(6)
    };
}
