FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Build on host first: dotnet publish src/Shortly.Api -c Release -o ./publish
# Then: docker compose up --build
COPY publish/ .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Shortly.Api.dll"]
