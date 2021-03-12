FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ContosoUniversityBackendService/ContosoUniversityBackendService.csproj", "ContosoUniversityBackendService/"]
RUN dotnet restore "ContosoUniversityBackendService/ContosoUniversityBackendService.csproj"
COPY . .
WORKDIR "/src/ContosoUniversityBackendService"
RUN dotnet build "ContosoUniversityBackendService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContosoUniversityBackendService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContosoUniversityBackendService.dll"]