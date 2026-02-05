FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BionicPRO.Api/BionicPRO.Api.csproj BionicPRO.Api/
RUN dotnet restore BionicPRO.Api/BionicPRO.Api.csproj

COPY BionicPRO.Api/ BionicPRO.Api/
WORKDIR /src/BionicPRO.Api
RUN dotnet build BionicPRO.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish BionicPRO.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Установка необходимых зависимостей
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/reports/health || exit 1

ENTRYPOINT ["dotnet", "BionicPRO.Api.dll"]
