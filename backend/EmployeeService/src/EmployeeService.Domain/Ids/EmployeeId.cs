namespace EmployeeService.Domain.Ids;

public record EmployeeId(Guid Value)
{
    public Guid ToGuid() => Value;

    public static implicit operator Guid(EmployeeId id) => id.Value;
}
