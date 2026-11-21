# Frontend build: npm ci uses the package-lock.json already created by Vite.
FROM node:24-bookworm-slim AS web-build
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ASP.NET Core API build.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY backend/TechHRMS.Api.csproj backend/
RUN dotnet restore backend/TechHRMS.Api.csproj
COPY backend/ backend/
RUN dotnet publish backend/TechHRMS.Api.csproj -c Release --no-restore -o /out

# One public origin serves both API and React SPA.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api-build /out ./
COPY --from=web-build /app/frontend/dist ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet TechHRMS.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
