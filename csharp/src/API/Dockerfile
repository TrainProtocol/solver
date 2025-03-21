ARG DOTNET_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}.0 AS build

WORKDIR /build
COPY csharp/src/API/ csharp/src/API/
COPY csharp/src/Core/ csharp/src/Core/
COPY csharp/src/Data/ csharp/src/Data/
COPY csharp/src/Data.Migrations/ csharp/src/Data.Migrations/
COPY csharp/nuget.config /
COPY csharp/Directory.Build.props /

FROM build AS publish
RUN dotnet publish "csharp/src/API/API.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Train.Solver.API.dll"]
EXPOSE 8080