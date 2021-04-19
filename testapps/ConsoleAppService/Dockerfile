FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["testapps/ConsoleAppService/ConsoleAppService.csproj", "testapps/ConsoleAppService/"]
RUN dotnet restore "testapps/ConsoleAppService/ConsoleAppService.csproj"
COPY "testapps/ConsoleAppService/" "testapps/ConsoleAppService/"
WORKDIR "/src/testapps/ConsoleAppService"
RUN dotnet build "ConsoleAppService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppService.dll"]
