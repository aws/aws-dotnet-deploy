# deployment-project generate command

### Usage
    dotnet aws deployment-project - Generates and saves the deployment CDK project in the user provided location.

### Synopsis
    dotnet aws deployment-project generate [-d|--diagnostics] [-s|--silent] [--profile <PROFILE>] [--region <REGION>] [--project-path <PROJECT-PATH>] [--project-display-name <DISPLAY-NAME>]

### Description
Generates and saves the [deployment CDK project](../deployment-projects/index.md) in a user provided directory path without proceeding with a deployment. Allows user to customize the CDK project before deploying the application.

### Examples

Creates a deployment project based on the .NET project in the current directory. The new deployment project will be saved to a sibling directory called CustomDeploymentProject. The project display name _"Team custom deployment project"_ will be displayed to users when selecting a publish target when deploying with the custom deployment project.

    dotnet aws deployment-project generate --output ../CustomDeploymentProject --project-display-name "Team custom deployment project" 

