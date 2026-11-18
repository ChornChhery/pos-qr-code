FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY IdentityService.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish IdentityService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003
USER $APP_UID
ENTRYPOINT ["dotnet", "IdentityService.dll"]
