# ---- Stage 1: build the isolated-worker Functions project ----
# Multi-stage build pattern (Docker, n.d.).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CoffeeNChillFunctions.csproj ./
RUN dotnet restore "CoffeeNChillFunctions.csproj"

COPY . .
RUN dotnet publish "CoffeeNChillFunctions.csproj" -c Release -o /app/publish --no-restore

# ---- Stage 2: run on the official Azure Functions isolated-worker base image ----
# Dockerfile for a Functions container (Microsoft, n.d.j).
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
WORKDIR /home/site/wwwroot

ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY --from=build /app/publish .