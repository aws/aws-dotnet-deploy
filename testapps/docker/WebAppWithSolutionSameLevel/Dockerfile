FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["WebAppWithSolutionSameLevel.csproj", ""]
RUN dotnet restore "WebAppWithSolutionSameLevel.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "WebAppWithSolutionSameLevel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAppWithSolutionSameLevel.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppWithSolutionSameLevel.dll"]