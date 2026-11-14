FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

RUN dotnet tool install dotnet-ef \
    --tool-path /tools \
    --version 10.0.11

COPY Directory.Build.props Directory.Packages.props nuget.config .globalconfig ./

COPY EmployeeService/src/EmployeeService.Web/EmployeeService.Web.csproj \
     EmployeeService/src/EmployeeService.Web/

COPY EmployeeService/src/EmployeeService.Core/EmployeeService.Core.csproj \
     EmployeeService/src/EmployeeService.Core/

COPY EmployeeService/src/EmployeeService.Domain/EmployeeService.Domain.csproj \
     EmployeeService/src/EmployeeService.Domain/

COPY EmployeeService/src/EmployeeService.Contracts/EmployeeService.Contracts.csproj \
     EmployeeService/src/EmployeeService.Contracts/

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

FROM build AS bundle

RUN --mount=type=secret,id=github_nuget_credentials,env=NuGetPackageSourceCredentials_github,required=true \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_ENVIRONMENT=Development \
    Seq__ServerUrl=http://seq \
    ConnectionStrings__EmployeeServiceDbContext="Host=localhost;Port=5432;Database=design_time;Username=design_time;Password=unused" \
    /tools/dotnet-ef migrations bundle \
    --project EmployeeService/src/EmployeeService.Infrastructure.Postgres \
    --startup-project EmployeeService/src/EmployeeService.Web \
    --context EmployeeServiceDbContext \
    --configuration Release \
    --no-build \
    --output /out/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-base

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle

USER $APP_UID

FROM runtime-base AS migrator

COPY --from=bundle --chmod=755 /out/efbundle ./efbundle
COPY --from=build /out/app/appsettings.json ./appsettings.json

ENTRYPOINT ["./efbundle"]

FROM runtime-base AS final

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

COPY --from=build /out/app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "EmployeeService.Web.dll"]
