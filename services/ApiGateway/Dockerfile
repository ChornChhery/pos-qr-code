FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ApiGateway.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish ApiGateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
USER $APP_UID
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
