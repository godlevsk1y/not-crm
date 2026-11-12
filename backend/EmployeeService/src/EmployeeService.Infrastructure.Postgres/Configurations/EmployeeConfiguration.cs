using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeService.Infrastructure.Postgres.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id).HasName("pk_employees");

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EmployeeId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.ComplexProperty(e => e.Name, name =>
        {
            name.IsRequired();
            name.Property(n => n.FirstName).IsRequired().HasColumnName("first_name");
            name.Property(n => n.LastName).IsRequired().HasColumnName("last_name");
        });

        builder.Property(e => e.PositionId)
            .HasConversion(id => id.Value, value => new PositionId(value))
            .IsRequired().HasColumnName("position_id");
        builder.Property(e => e.DepartmentId)
            .HasConversion(id => id.Value, value => new DepartmentId(value))
            .IsRequired().HasColumnName("department_id");
        builder.Property(e => e.HiredAt).HasColumnName("hired_at");
        builder.Property(e => e.DismissedAt).HasColumnName("dismissed_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
