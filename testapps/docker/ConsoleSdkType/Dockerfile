FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ConsoleSdkType.csproj", ""]
RUN dotnet restore "ConsoleSdkType.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "ConsoleSdkType.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleSdkType.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleSdkType.dll"]
