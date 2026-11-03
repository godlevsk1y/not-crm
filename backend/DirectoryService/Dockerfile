FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

RUN dotnet tool install dotnet-ef \
    --tool-path /tools \
    --version 10.0.11 

COPY Directory.Build.props Directory.Packages.props nuget.config .globalconfig ./

COPY DirectoryService/src/DirectoryService.Web/DirectoryService.Web.csproj \
     DirectoryService/src/DirectoryService.Web/

COPY DirectoryService/src/DirectoryService.Core/DirectoryService.Core.csproj \
     DirectoryService/src/DirectoryService.Core/

COPY DirectoryService/src/DirectoryService.Domain/DirectoryService.Domain.csproj \
     DirectoryService/src/DirectoryService.Domain/

COPY DirectoryService/src/DirectoryService.Contracts/DirectoryService.Contracts.csproj \
     DirectoryService/src/DirectoryService.Contracts/

COPY DirectoryService/src/DirectoryService.Infrastructure.Postgres/DirectoryService.Infrastructure.Postgres.csproj \
     DirectoryService/src/DirectoryService.Infrastructure.Postgres/
     
     
RUN --mount=type=secret,id=github_nuget_credentials,env=NuGetPackageSourceCredentials_github,required=true \
    dotnet restore \
    DirectoryService/src/DirectoryService.Web/DirectoryService.Web.csproj \
    --configfile nuget.config 

COPY DirectoryService/src/ DirectoryService/src/

RUN dotnet publish \
    DirectoryService/src/DirectoryService.Web/DirectoryService.Web.csproj \
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
    ConnectionStrings__DirectoryServiceDbContext="Host=localhost;Port=5432;Database=design_time;Username=design_time;Password=unused" \
    /tools/dotnet-ef migrations bundle \
    --project DirectoryService/src/DirectoryService.Infrastructure.Postgres \
    --startup-project DirectoryService/src/DirectoryService.Web \
    --context DirectoryServiceDbContext \
    --configuration Release \
    --no-build \
    --output /out/efbundle 


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-base

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_ENVIRONMENT=Development \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle
    
USER $APP_UID

FROM runtime-base AS migrator

COPY --from=bundle --chmod=755 /out/efbundle ./efbundle
COPY --from=build /out/app/appsettings.json ./appsettings.json

ENTRYPOINT ["./efbundle"]

FROM runtime-base AS final

COPY --from=build /out/app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "DirectoryService.Web.dll"]
