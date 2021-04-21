# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Dockerfile paths are adjusted to allow docker build from the root of the repository
# This is required for integration tests

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["testapps/ConsoleAppTask/ConsoleAppTask.csproj", "testapps/ConsoleAppTask/"]
RUN dotnet restore "testapps/ConsoleAppTask/ConsoleAppTask.csproj"
COPY "testapps/ConsoleAppTask/" "testapps/ConsoleAppTask/"
WORKDIR "/src/testapps/ConsoleAppTask"
RUN dotnet build "ConsoleAppTask.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppTask.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppTask.dll"]
