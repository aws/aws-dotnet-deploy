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
RUN apt-get update -yq \
    && apt-get install -yq ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update -yq \
    && apt-get install nodejs -yq
RUN dotnet publish "WebAppNet8WithCustomDockerFile.csproj" -c Release -o /app/publish

FROM base AS final
ENV ASPNETCORE_HTTP_PORTS=80
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppNet8WithCustomDockerFile.dll"]
