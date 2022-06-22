FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["WebAppDifferentTargetFramework.csproj", ""]
RUN dotnet restore "WebAppDifferentTargetFramework.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "WebAppDifferentTargetFramework.csproj" -c Release -o /app/build

FROM build AS publish
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_14.x | bash \
    && apt-get install nodejs -yq
RUN dotnet publish "WebAppDifferentTargetFramework.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppDifferentTargetFramework.dll"]
