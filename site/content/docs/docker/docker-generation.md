# Dockerfile Generation

**IF YOUR PROJECT DOES NOT CONTAIN A DOCKERFILE, THE DEPLOYMENT TOOL WILL AUTOMATICALLY GENERATE IT,** otherwise an existing Dockerfile will be used for deployment.

The Dockerfile that deployment tools generates uses Docker's multistage build process. This allows efficient and smaller container images that only contain the bits that are required to run your application.

For a sample web application which has following directory structure:

    ┃MyWebApplication/
    ┣ MyClassLibrary/
    ┃ ┣ Class1.cs
    ┃ ┗ MyClassLibrary.csproj
    ┣ MyWebApplication/
    ┃ ┣ Controllers/
    ┃ ┃ ┗ WeatherForecastController.cs
    ┃ ┣ appsettings.Development.json
    ┃ ┣ appsettings.json
    ┃ ┣ Dockerfile
    ┃ ┣ MyWebApplication.csproj
    ┃ ┣ Program.cs
    ┃ ┗ WeatherForecast.cs
    ┗ MyWebApplication.sln

### Build

Build stage consists of copying the files from the host machine to the container, restoring the dependencies, and building the application. Deployment Tool uses .sln directory as build context and generates file paths relative to the .sln directory.

    FROM mcr.microsoft.com/dotnet/core/aspnet:6.0 AS base
    WORKDIR /app
    EXPOSE 80
    EXPOSE 443
    WORKDIR /src
    COPY ["MyWebApplication/MyWebApplication.csproj", "MyWebApplication/"]
    COPY ["MyClassLibrary/MyClassLibrary.csproj", "MyClassLibrary/"]
    RUN dotnet restore "MyWebApplication/MyWebApplication.csproj"
    COPY . .
    WORKDIR "/src/MyWebApplication"
    RUN dotnet build "MyWebApplication.csproj" -c Release -o /app/build

### Publish

Publish stage takes the build output and publishes .NET application to /app/publish directory.

    FROM build AS publish
    RUN dotnet publish "MyWebApplication.csproj" -c Release -o /app/publish

### Final

Final stage takes the publish output and copies it to the container which uses ASP.NET Core image as a base image.

    FROM base AS final
    WORKDIR /app
    COPY --from=publish /app/publish .
    ENTRYPOINT ["dotnet", "MyWebApplication.dll"]
