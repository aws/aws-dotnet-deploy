# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Dockerfile paths are adjusted to allow docker build from the root of the repository
# This is required for integration tests

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WebAppWithDockerFile.csproj", "WebAppWithDockerFile/"]
RUN dotnet restore "WebAppWithDockerFile/WebAppWithDockerFile.csproj"
COPY . "WebAppWithDockerFile/"
WORKDIR "/src/WebAppWithDockerFile"
RUN dotnet build "WebAppWithDockerFile.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAppWithDockerFile.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppWithDockerFile.dll"]
