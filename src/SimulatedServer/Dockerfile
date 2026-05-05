FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/SimulatedServer/SimulatedServer.csproj src/SimulatedServer/
RUN dotnet restore src/SimulatedServer/SimulatedServer.csproj
COPY . .
RUN dotnet publish src/SimulatedServer/SimulatedServer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "SimulatedServer.dll"]
