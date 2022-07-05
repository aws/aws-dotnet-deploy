# deploy command

### Usage
    dotnet aws deploy - Inspect, build, and deploy the .NET project to AWS using the chosen AWS compute.

### Synopsis
    dotnet aws deploy [-d|—-diagnostics] [-s|--silent] [--profile <PROFILE>] [--region <REGION>] [--project-path <PROJECT-PATH>] [[--save-settings|--save-all-settings] <SETTINGS-FILE-PATH>] [--application-name <CLOUD-APPLICATION-NAME>] [--apply <PATH-TO-DEPLOYMENT-SETTINGS>] [--deployment-project <CDK-DEPLOYMENT-PROJECT-PATH>] [-?|-h|--help]

### Description
Inspects the project and recommends AWS compute that is most suited to the type of deployed application. Then builds the project, generates a deployment CDK project to provision the required infrastructure, and deploys the .NET project to AWS using the chosen AWS compute.

### Examples

Deploying HelloWorld

    dotnet new web -n HelloWorld -f net6.0
    cd HelloWorld
    dotnet aws deploy

Deploying application to a non-default profile

    dotnet aws deploy --profile myCustomProfile --region us-east1
