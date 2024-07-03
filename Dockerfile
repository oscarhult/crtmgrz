#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Ensure the /app/data directory exists and is writable
RUN mkdir -p /app/data && chown -R app:app /app/data

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["crtmgrz.csproj", "."]
RUN dotnet restore "./crtmgrz.csproj" -r linux-musl-x64
COPY . .
WORKDIR "/src/."
RUN dotnet build "./crtmgrz.csproj" -c $BUILD_CONFIGURATION -o /app/build -r linux-musl-x64

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./crtmgrz.csproj" -c $BUILD_CONFIGURATION -o /app/publish -r linux-musl-x64 /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "crtmgrz.dll"]