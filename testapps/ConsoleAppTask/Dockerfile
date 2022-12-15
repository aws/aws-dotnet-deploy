# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Dockerfile paths are adjusted to allow docker build from the root of the repository
# This is required for integration tests

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ConsoleAppTask.csproj", "ConsoleAppTask/"]
RUN dotnet restore "ConsoleAppTask/ConsoleAppTask.csproj"
COPY . "ConsoleAppTask/"
WORKDIR "/src/ConsoleAppTask"
RUN dotnet build "ConsoleAppTask.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppTask.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppTask.dll"]
