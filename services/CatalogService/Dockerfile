# Build context is the repository root (this service references shared/Pos.Shared).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/Pos.Shared/Pos.Shared.csproj shared/Pos.Shared/
COPY services/CatalogService/CatalogService.csproj services/CatalogService/
RUN dotnet restore services/CatalogService/CatalogService.csproj
COPY shared/ shared/
COPY services/CatalogService/ services/CatalogService/
RUN dotnet publish services/CatalogService/CatalogService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
USER $APP_UID
ENTRYPOINT ["dotnet", "CatalogService.dll"]
