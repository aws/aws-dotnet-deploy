# deployment-project generate command

### Usage
    dotnet aws deployment-project - Generates and saves the deployment CDK project in the user provided location.

### Synopsis
    dotnet aws deployment-project generate [-d|--diagnostics] [-s|--silent] [--profile <PROFILE>] [--region <REGION>] [--project-path <PROJECT-PATH>] [--project-display-name <DISPLAY-NAME>]

### Description
Generates and saves the deployment CDK project in a user provided directory path without proceeding with a deployment. Allows user to customize the CDK project before deploying the application.

### Examples

Creating a deployment project based on the .NET project in the current directory. The new deployment project will be saved to a sibling directory called CustomDeploymentProject. When the .NET project is next deployed the deployment project will be an available option called "Team custom deployment project".

    dotnet aws deployment-project generate --output ../CustomDeploymentProject --project-display-name "Team custom deployment project" 

