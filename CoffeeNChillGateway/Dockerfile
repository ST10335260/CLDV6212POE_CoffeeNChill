# ---- Stage 1: build the gateway ----
# Multi-stage build pattern (Docker, n.d.).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CoffeeNChillGateway.csproj ./
RUN dotnet restore "CoffeeNChillGateway.csproj"

COPY . .
RUN dotnet publish "CoffeeNChillGateway.csproj" -c Release -o /app/publish --no-restore

# ---- Stage 2: run on the ASP.NET Core runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoffeeNChillGateway.dll"]
