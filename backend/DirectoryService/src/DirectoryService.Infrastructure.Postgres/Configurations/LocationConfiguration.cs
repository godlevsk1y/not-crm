using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        
        builder.HasKey(l => l.Id)
            .HasName("pk_locations");

        builder.Property(l => l.Id)
            .HasConversion(l => l.Value, id => new LocationId(id))
            .HasColumnName("id");

        builder.ComplexProperty(l => l.Name, nb =>
        {
            nb.Property(n => n.Value)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
        });
        
        // unique index on Name is created manually in the migration
        
        builder.ComplexProperty(l => l.Address, ab =>
        {
            ab.Property(a => a.Country)
                .HasMaxLength(60)
                .IsRequired()
                .HasColumnName("country");
            
            ab.Property(a => a.Region)
                .HasMaxLength(60)
                .IsRequired(false)
                .HasColumnName("region");
            
            ab.Property(a => a.City)
                .HasMaxLength(60)
                .IsRequired()
                .HasColumnName("city");
            
            ab.Property(a => a.District)
                .HasMaxLength(60)
                .IsRequired(false)
                .HasColumnName("district");
            
            ab.Property(a => a.Street)
                .HasMaxLength(60)
                .IsRequired()
                .HasColumnName("street");
            
            ab.Property(a => a.HouseNumber)
                .HasMaxLength(60)
                .IsRequired()
                .HasColumnName("house_number");
            
            ab.Property(a => a.PostalCode)
                .HasMaxLength(10)
                .IsRequired(false)
                .HasColumnName("postal_code");
        });
        
        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}