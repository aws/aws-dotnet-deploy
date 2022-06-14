# Deployment Settings File

You can select deployment options interactively or specify them in a [JSON configuration file](docs/features/config-file.md). You can also keep the default values that the tool selects for you.

This section defines the JSON definitions and syntax that you construct and use a deployment settings file. Here is the [JSON file definition](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions).

The Deployment Settings file is a JSON file that contains all of the settings that the deployment tool uses to drive the experience. The deployment tool uses this file to determine which deployment services to use, and how to configure them. The `--apply` option on deploy command allows you to specify a deployment settings file.

#### Creating a deployment settings file
TODO

#### Applying a deployment settings file

Deployment Settings file path is always relative to the `--project-path`. For a sample web application which has following directory structure:

    MyWebApplication/
    ┣ MyClassLibrary/
    ┃ ┣ Class1.cs
    ┃ ┗ MyClassLibrary.csproj
    ┣ MyWebApplication/
    ┃ ┣ Controllers/
    ┃ ┃ ┗ WeatherForecastController.cs
    ┃ ┣ appsettings.Development.json
    ┃ ┣ appsettings.json
    ┃ ┣ deploymentsettings.json
    ┃ ┣ Dockerfile
    ┃ ┣ MyWebApplication.csproj
    ┃ ┣ Program.cs
    ┃ ┗ WeatherForecast.cs
    ┗ MyWebApplication.sln

You can run the deployment tool with the following command:

`dotnet aws deploy --project-path MyWebApplication/MyWebApplication/MyWebApplication.csproj --apply deploymentsettings.json`

