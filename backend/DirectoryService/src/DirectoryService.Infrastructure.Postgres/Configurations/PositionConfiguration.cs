using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");
        
        builder.HasKey(p => p.Id)
            .HasName("pk_positions");

        builder.Property(p => p.Id)
            .HasConversion(p => p.Value, id => new PositionId(id))
            .HasColumnName("id");

        builder.ComplexProperty(p => p.Name, nb =>
        {
            nb.Property(n => n.Value)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("name");
        });
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");
        
        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}