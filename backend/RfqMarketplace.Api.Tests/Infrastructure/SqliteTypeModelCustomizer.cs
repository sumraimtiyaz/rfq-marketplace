using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RfqMarketplace.Api.Tests.Infrastructure;

/// <summary>
/// SQLite has no native decimal or DateTimeOffset type. Left alone, EF Core stores both as TEXT,
/// which makes ORDER BY lexicographic for decimals ("900.00" would sort after "1250.00") and
/// unsupported outright for DateTimeOffset.
///
/// Production runs on PostgreSQL, where numeric(18,2) and timestamptz both sort correctly. These
/// converters give the SQLite test database the same ordering semantics, so the suite exercises
/// the real queries instead of forcing the production code to dodge a test-only limitation.
/// </summary>
public class SqliteTypeModelCustomizer(ModelCustomizerDependencies dependencies)
    : RelationalModelCustomizer(dependencies)
{
    private static readonly ValueConverter<decimal, double> DecimalConverter =
        new(value => (double)value, value => (decimal)value);

    private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetConverter =
        new(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));

    private static readonly ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetConverter =
        new(value => value == null ? null : value.Value.UtcTicks,
            value => value == null ? null : new DateTimeOffset(value.Value, TimeSpan.Zero));

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        var properties = modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties());

        foreach (var property in properties)
        {
            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                Apply(property, DecimalConverter);
            else if (property.ClrType == typeof(DateTimeOffset))
                Apply(property, DateTimeOffsetConverter);
            else if (property.ClrType == typeof(DateTimeOffset?))
                Apply(property, NullableDateTimeOffsetConverter);
        }
    }

    private static void Apply(IMutableProperty property, ValueConverter converter)
    {
        property.SetValueConverter(converter);
        // The PostgreSQL-specific facets no longer describe the stored column.
        property.SetColumnType(null);
        property.SetPrecision(null);
        property.SetScale(null);
    }
}
