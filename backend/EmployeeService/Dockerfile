FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config .globalconfig ./

COPY EmployeeService/src/EmployeeService.Web/EmployeeService.Web.csproj \
     EmployeeService/src/EmployeeService.Web/

COPY EmployeeService/src/EmployeeService.Infrastructure.Postgres/EmployeeService.Infrastructure.Postgres.csproj \
     EmployeeService/src/EmployeeService.Infrastructure.Postgres/

RUN --mount=type=secret,id=github_nuget_credentials,env=NuGetPackageSourceCredentials_github,required=true \
    dotnet restore \
    EmployeeService/src/EmployeeService.Web/EmployeeService.Web.csproj \
    --configfile nuget.config

COPY EmployeeService/src/ EmployeeService/src/

RUN dotnet publish \
    EmployeeService/src/EmployeeService.Web/EmployeeService.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /out/app \
    --disable-build-servers \
    -m:1 \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

USER $APP_UID

COPY --from=build /out/app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "EmployeeService.Web.dll"]
