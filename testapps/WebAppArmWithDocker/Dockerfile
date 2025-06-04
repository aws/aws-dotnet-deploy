FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["WebAppArmWithDocker.csproj", "WebAppArmWithDocker/"]
RUN dotnet restore "WebAppArmWithDocker/WebAppArmWithDocker.csproj" -a $TARGETARCH
COPY . "WebAppArmWithDocker/"
WORKDIR "/src/WebAppArmWithDocker"
RUN dotnet build "WebAppArmWithDocker.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
RUN dotnet publish "WebAppArmWithDocker.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppArmWithDocker.dll"]
