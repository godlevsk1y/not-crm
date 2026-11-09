using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EmployeeService.IntegrationTests;

public class ApplicationSmokeTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task HealthLive_ShouldReturnOk(string environment)
    {
        await using var factory = new ApplicationFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task ApiDocumentation_ShouldBeAvailableOnlyInDevelopment(string path)
    {
        await using var developmentFactory = new ApplicationFactory("Development");
        using var developmentClient = developmentFactory.CreateClient();
        using var developmentResponse = await developmentClient.GetAsync(path, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, developmentResponse.StatusCode);

        await using var productionFactory = new ApplicationFactory("Production");
        using var productionClient = productionFactory.CreateClient();
        using var productionResponse = await productionClient.GetAsync(path, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, productionResponse.StatusCode);
    }

    private sealed class ApplicationFactory(string environment) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment(environment);
    }
}
