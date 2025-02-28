ARG DOTNET_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}.0 AS build

WORKDIR /build

RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | sh
ARG FEED_ACCESSTOKEN
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS \
    "{\"endpointCredentials\": [{\"endpoint\":\"https://pkgs.dev.azure.com/layerswaphq/Layerswap/_packaging/Layerswap/nuget/v3/index.json\", \"password\":\"${FEED_ACCESSTOKEN}\"}]}"
COPY csharp/src/WorkflowRunner/ csharp/src/WorkflowRunner/
COPY csharp/src/Core/ csharp/src/Core/
COPY csharp/src/Data/ csharp/src/Data/
COPY csharp/nuget.config /
COPY csharp/Directory.Build.props /

FROM build AS publish
RUN dotnet publish "csharp/src/WorkflowRunner/WorkflowRunner.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Train.Solver.WorkflowRunner.dll"]