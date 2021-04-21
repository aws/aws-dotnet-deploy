# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Dockerfile paths are adjusted to allow docker build from the root of the repository
# This is required for integration tests

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["testapps/WebAppWithDockerFile/WebAppWithDockerFile.csproj", "testapps/WebAppWithDockerFile/"]
RUN dotnet restore "testapps/WebAppWithDockerFile/WebAppWithDockerFile.csproj"
COPY "testapps/WebAppWithDockerFile/" "testapps/WebAppWithDockerFile/"
WORKDIR "/src/testapps/WebAppWithDockerFile"
RUN dotnet build "WebAppWithDockerFile.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAppWithDockerFile.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppWithDockerFile.dll"]
