FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WorkerServiceExample.csproj", ""]
RUN dotnet restore "WorkerServiceExample.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "WorkerServiceExample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WorkerServiceExample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkerServiceExample.dll"]
