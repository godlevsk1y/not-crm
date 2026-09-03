namespace DirectoryService.Contracts.Locations.QueryContracts;

public record LocationWithDepartmentCountDto(LocationDto Location, long DepartmentCount);