# Integrating with CI/CD

You can use AWS Deploy Tool when developing your app using any Continuous Deployment system. Continuous Deployment systems let you automatically build, test and deploy your application each time you check in updates to your source code. Before you can use AWS Deploy Tool in your CD pipeline, you must have [required pre-requisites](../docs/getting-started/pre-requisites.md) installed and configured in the CD environment.

### Suppressing prompts with `--silent`

To turn off the interactive features, use the `-s (--silent)` switch. This will ensure the tool never prompts for any questions that could block an automated process.

    dotnet aws deploy --silent

### Creating a deployment setting file

To specify the services to deploy and their configurations for your environment, you need to create deployment settings file. The deployment settings file is a JSON configuration file that contains all of the settings that the deployment tool uses to drive the experience. Here is the [JSON file definition](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions).

Storing deployment settings in a JSON file also allows those settings to be version controlled.

This section defines the JSON definitions and syntax that you construct and use a deployment settings file.

TODO

### Invoking from CI/CD

The `--apply` switch on deploy command allows you to specify a deployment settings file.

Deployment settings file path is always relative to the `--project-path`. Here's an example of a web application with the following directory structure:

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

To deploy the application with above directory structure in CI/CD pipeline without any prompts, use the following command:

    dotnet aws deploy --silent --project-path MyWebApplication/MyWebApplication/MyWebApplication.csproj --apply deploymentsettings.json

