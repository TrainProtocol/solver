ARG DOTNET_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}.0 AS build

WORKDIR /build
COPY csharp/src/WorkflowRunner.Solana/ csharp/src/WorkflowRunner.Solana/
COPY csharp/src/Core/ csharp/src/Core/
COPY csharp/nuget.config /
COPY csharp/Directory.Build.props /

FROM build AS publish
RUN dotnet publish "csharp/src/WorkflowRunner.Solana/WorkflowRunner.Solana.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Train.Solver.WorkflowRunner.Solana.dll"]