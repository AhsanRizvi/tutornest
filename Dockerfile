# ── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layer for caching
COPY TutorNest.API/TutorNest.API.csproj TutorNest.API/
RUN dotnet restore TutorNest.API/TutorNest.API.csproj

# Copy everything else and publish
COPY TutorNest.API/ TutorNest.API/
RUN dotnet publish TutorNest.API/TutorNest.API.csproj -c Release -o /app/publish --no-restore

# ── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TutorNest.API.dll"]
