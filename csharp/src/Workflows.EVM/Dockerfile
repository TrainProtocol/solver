ARG DOTNET_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}.0 AS build
WORKDIR /build

COPY csharp/ csharp/

FROM build AS publish
RUN dotnet publish "csharp/src/Workflows.EVM/Workflows.EVM.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Train.Solver.Workflows.EVM.dll"]