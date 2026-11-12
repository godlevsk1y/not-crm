namespace EmployeeService.Domain.Ids;

public record DepartmentId(Guid Value)
{
    public Guid ToGuid() => Value;

    public static implicit operator Guid(DepartmentId id) => id.Value;
}
