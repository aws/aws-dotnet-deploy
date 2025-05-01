FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["ConsoleAppArmDeployment.csproj", "ConsoleAppArmDeployment/"]
RUN dotnet restore "ConsoleAppArmDeployment/ConsoleAppArmDeployment.csproj" -a $TARGETARCH
COPY . "ConsoleAppArmDeployment/"

WORKDIR "/src/ConsoleAppArmDeployment"
RUN dotnet build "ConsoleAppArmDeployment.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
RUN dotnet publish "ConsoleAppArmDeployment.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppArmDeployment.dll"]
