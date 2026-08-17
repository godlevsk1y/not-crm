using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        
        builder.HasKey(d => d.Id).HasName("pk_departments");
        
        builder.Property(d => d.Id)
            .HasConversion(d => d.Value, id => new DepartmentId(id))
            .HasColumnName("id");

        builder.ComplexProperty(d => d.Name, nb =>
        {
            nb.Property(n => n.Value)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
        });
        
        builder.ComplexProperty(d => d.Slug, sb =>
        {
            sb.Property(s => s.Value)
                .IsRequired()
                .HasMaxLength(60)
                .HasColumnName("slug");
        });

        builder.ComplexProperty(d => d.Path, pb =>
        {
            pb.Property(p => p.Value)
                .IsRequired()
                .HasMaxLength(600)
                .HasColumnName("path");
        });
        
        builder
            .HasOne<Department>(d => d.Parent)
            .WithMany()
            .IsRequired(false)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_departments_parent");

        builder.Property(d => d.ParentId)
            .HasColumnName("parent_id");
        
        builder.HasIndex(d => d.ParentId)
            .HasDatabaseName("ix_departments_parent_id");
        
        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");
        
        builder.Property(d => d.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
    }
}
