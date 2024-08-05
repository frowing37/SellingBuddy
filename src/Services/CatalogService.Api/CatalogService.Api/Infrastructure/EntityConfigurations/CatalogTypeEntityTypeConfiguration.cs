using CatalogService.Core.Domain;
using CatalogService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.EntityConfigurations;

public class CatalogTypeEntityTypeConfiguration : IEntityTypeConfiguration<CatalogType>
{
    public void Configure(EntityTypeBuilder<CatalogType> builder)
    {
        builder.ToTable("CatalogType", CatalogContext.DEFAULT_SCHEMA);
        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Id).UseHiLo("catalog_tpe_hilo").IsRequired();
        builder.Property(cb => cb.Type).IsRequired().HasMaxLength(100);
    }
}