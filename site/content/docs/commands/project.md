# deployment-project generate command

### Usage
    dotnet aws deployment-project - Generates and saves the deployment CDK project in the user provided location.

### Synopsis
    dotnet aws deployment-project generate [-d|--diagnostics] [-s|--silent] [--profile <PROFILE>] [--region <REGION>] [--project-path <PROJECT-PATH>] [--project-display-name <DISPLAY-NAME>]

### Description
Generates and saves the deployment CDK project in a user provided directory path without proceeding with a deployment. Allows user to customize the CDK project before deploying the application.

### Examples
```
dotnet aws deployment-project generate --region us-west-2
```