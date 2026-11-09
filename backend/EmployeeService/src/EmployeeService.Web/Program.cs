using EmployeeService.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProgramDependencies();

var app = builder.Build();

app.Configure();

await app.RunAsync();
