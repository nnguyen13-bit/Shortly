FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files for restore
COPY Shortly.slnx .
COPY src/Shortly.Domain/Shortly.Domain.csproj src/Shortly.Domain/
COPY src/Shortly.Application/Shortly.Application.csproj src/Shortly.Application/
COPY src/Shortly.Infrastructure/Shortly.Infrastructure.csproj src/Shortly.Infrastructure/
COPY src/Shortly.Api/Shortly.Api.csproj src/Shortly.Api/

RUN dotnet restore Shortly.slnx --runtime linux-x64

# Copy source and build
COPY src/ src/
RUN dotnet publish src/Shortly.Api/Shortly.Api.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --no-self-contained \
    --no-restore \
    --output /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Shortly.Api.dll"]
