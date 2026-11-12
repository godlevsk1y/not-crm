namespace EmployeeService.IntegrationTests;

[CollectionDefinition("Employee database", DisableParallelization = true)]
public class EmployeeDatabaseFixtureDefinition : ICollectionFixture<EmployeeServiceTestWebFactory>;
