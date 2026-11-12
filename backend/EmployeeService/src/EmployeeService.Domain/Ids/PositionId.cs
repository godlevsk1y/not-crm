namespace EmployeeService.Domain.Ids;

public record PositionId(Guid Value)
{
    public Guid ToGuid() => Value;

    public static implicit operator Guid(PositionId id) => id.Value;
}
