FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["WebAppWithSolutionParentLevel/WebAppWithSolutionParentLevel.csproj", "WebAppWithSolutionParentLevel/"]
RUN dotnet restore "WebAppWithSolutionParentLevel/WebAppWithSolutionParentLevel.csproj"
COPY . .
WORKDIR "/src/WebAppWithSolutionParentLevel"
RUN dotnet build "WebAppWithSolutionParentLevel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAppWithSolutionParentLevel.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppWithSolutionParentLevel.dll"]