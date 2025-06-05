FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebAppNet8WithCustomDockerFile.csproj", ""]
RUN dotnet restore "WebAppNet8WithCustomDockerFile.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "WebAppNet8WithCustomDockerFile.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAppNet8WithCustomDockerFile.csproj" -c Release -o /app/publish

FROM base AS final
ENV ASPNETCORE_HTTP_PORTS=80
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppNet8WithCustomDockerFile.dll"]
