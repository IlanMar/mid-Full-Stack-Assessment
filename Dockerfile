FROM node:22-alpine AS frontend
WORKDIR /src
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Monitor.Api/Monitor.Api.csproj ./
RUN dotnet restore
COPY backend/Monitor.Api/ ./
RUN dotnet publish -c Release -o /publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /publish/ ./
COPY --from=frontend /src/dist/ ./wwwroot/
USER $APP_UID
ENTRYPOINT ["dotnet", "Monitor.Api.dll"]
