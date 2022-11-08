# Integrating with CI/CD

You can use AWS Deploy Tool when developing your app using any Continuous Deployment system. Continuous Deployment systems let you automatically build, test and deploy your application each time you check in updates to your source code. Before you can use AWS Deploy Tool in your CD pipeline, you must have [required pre-requisites](../docs/getting-started/pre-requisites.md) installed and configured in the CD environment.

### Suppressing prompts with `--silent`

To turn off the interactive features, use the `-s (--silent)` switch. This will ensure the tool never prompts for any questions that could block an automated process.

```
dotnet aws deploy --silent
```

### Creating a deployment settings file

You can persist the deployment configuration to a JSON file using the `--save-settings <SETTINGS_FILE_PATH)>` switch. This JSON file can be version controlled and plugged into your CI/CD system for future deployments.

**Note** - The `--save-settings` switch will only persist settings that have been modified (which means they hold a non-default value). To persist all settings use the `--save-all-settings` switch.

```
dotnet aws deploy --project-path <PROJECT_PATH> [--save-settings|--save-all-settings] <SETTINGS_FILE_PATH>
```

**Note** - The `SETTINGS_FILE_PATH` can be an absolute path or relative to the `PROJECT_PATH`.

Here's an example of a web application with the following directory structure:

    MyWebApplication/
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
    ┃ ┣ WeatherForecast.cs
    ┗ MyWebApplication.sln

To perform a deployment and also persist the deployment configuration to a JSON file, use the following command:

```
dotnet aws deploy --project-path MyWebApplication/MyWebApplication/MyWebApplication.csproj --save-settings deploymentsettings.json
```

This will create a JSON file at `MyWebApplication/MyWebApplication/deploymentsettings.json` with the following structure:

```json
{
      "AWSProfile": <AWS_PROFILE>
      "AWSRegion": <AWS_REGION>,
      "ApplicationName": <APPLICATION_NAME>,
      "RecipeId": <RECIPE_ID>
      "Settings": <JSON_BLOB>
}
```

* _**AWSProfile**_: The name of the AWS profile that was used during deployment.

* _**AWSRegion**_: The name of the AWS region where the deployed application is hosted.

* _**ApplicationName**_: The name that is used to identify your cloud application within AWS. If the application is deployed via AWS CDK, then this name points to the CloudFormation stack.

* _**RecipeId**_: The recipe identifier that was used to deploy your application to AWS.

* _**Settings**_: This is a JSON blob that stores the values of all available settings that can be tweaked to adjust the deployment configuration.

### Invoking from CI/CD

The `--apply` switch on the deploy command allows you to specify a deployment settings file.

```
dotnet aws deploy --project-path <PROJECT_PATH> --apply <SETTINGS_FILE_PATH>
```

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

To deploy the application with the above directory structure in CI/CD pipeline without any prompts, use the following command:

```
dotnet aws deploy --silent --project-path MyWebApplication/MyWebApplication/MyWebApplication.csproj --apply deploymentsettings.json
```
