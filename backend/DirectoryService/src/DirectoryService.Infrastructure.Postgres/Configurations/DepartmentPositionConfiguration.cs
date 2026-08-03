using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class DepartmentPositionConfiguration : IEntityTypeConfiguration<DepartmentPosition>
{
    public void Configure(EntityTypeBuilder<DepartmentPosition> builder)
    {
        builder.ToTable("department_positions");

        builder.HasKey(dp => dp.Id)
            .HasName("pk_department_positions");

        builder.Property(dp => dp.Id)
            .HasColumnName("id");
        
        builder.Property(dp => dp.DepartmentId)
            .IsRequired()
            .HasConversion(dp => dp.Value, id => new DepartmentId(id))
            .HasColumnName("department_id");
        
        builder.Property(dp => dp.PositionId)
            .IsRequired()
            .HasConversion(dp => dp.Value, id => new PositionId(id))
            .HasColumnName("position_id");

        builder.HasIndex(dp => new { dp.DepartmentId, dp.PositionId })
            .IsUnique()
            .HasDatabaseName("uq_department_positions_department_id_position_id");
        
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(dp => dp.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_department_positions_departments_department_id");
        
        builder.HasIndex(dp => dp.DepartmentId)
            .HasDatabaseName("ix_department_positions_department_id");
        
        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(dp => dp.PositionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_department_positions_positions_position_id");
        
        builder.HasIndex(dp => dp.PositionId)
            .HasDatabaseName("ix_department_positions_position_id");
    }
}