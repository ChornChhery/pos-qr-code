# Build context is the repository root (this service references shared/Pos.Shared).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/Pos.Shared/Pos.Shared.csproj shared/Pos.Shared/
COPY services/OrderService/OrderService.csproj services/OrderService/
RUN dotnet restore services/OrderService/OrderService.csproj
COPY shared/ shared/
COPY services/OrderService/ services/OrderService/
RUN dotnet publish services/OrderService/OrderService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002
USER $APP_UID
ENTRYPOINT ["dotnet", "OrderService.dll"]
