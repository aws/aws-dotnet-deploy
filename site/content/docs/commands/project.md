# deployment-project generate command

### Usage
    dotnet aws deployment-project - Generates and saves the deployment CDK project in the user provided location.

### Synopsis
    dotnet aws deployment-project generate [-d|--diagnostics] [-s|--silent] [--profile <PROFILE>] [--region <REGION>] [--project-path <PROJECT-PATH>] [--project-display-name <DISPLAY-NAME>]

### Description
Generates and saves the [deployment CDK project](../deployment-projects/index.md) in a user provided directory path without proceeding with a deployment. Allows user to customize the CDK project before deploying the application.

* The `--output` switch sets the directory where the deployment project will be saved.
* The `--project-display-name` switch sets the name that will be shown when the .NET project is being deployed.

### Examples

This example creates a deployment project from the .NET project in the current directory. The  deployment project will be saved to a sibling directory called CustomDeploymentProject. The name _"Team custom deployment project"_ will be displayed in the list of the available deployment options.

    dotnet aws deployment-project generate --output ../CustomDeploymentProject --project-display-name "Team custom deployment project"

