FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /build
COPY csharp/ csharp/

FROM build AS publish
RUN dotnet publish "csharp/src/API/API.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Train.Solver.API.dll"]
EXPOSE 8080