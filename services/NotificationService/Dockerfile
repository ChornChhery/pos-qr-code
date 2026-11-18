# Build context is the repository root (this service references shared/Pos.Shared).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/Pos.Shared/Pos.Shared.csproj shared/Pos.Shared/
COPY services/NotificationService/NotificationService.csproj services/NotificationService/
RUN dotnet restore services/NotificationService/NotificationService.csproj
COPY shared/ shared/
COPY services/NotificationService/ services/NotificationService/
RUN dotnet publish services/NotificationService/NotificationService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5005
EXPOSE 5005
USER $APP_UID
ENTRYPOINT ["dotnet", "NotificationService.dll"]
