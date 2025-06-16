## Release 2025-06-16

### AWS.Deploy.CLI (2.0.0)
* NodeJS has been removed from the generated Dockerfiles. If you have an application that requires NodeJS, you will need to add it to the generated Dockerfile and rerun the Deploy Tool.
* Upgrade the Deploy Tool from .NET 6 to .NET 8
* Switch from System.CommandLine to Spectre.CLI for a better CLI experience
* Update Amazon CDK library to 2.194.0 and CDK CLI to 2.1013.0
* Update AWS .NET SDK to V4
* Upgrade Microsoft Templating Engine from .NET 5 to .NET 8
* Add support for deploying .NET 10 applications across different recipes
* Add support for Podman in addition to the existing Docker support
* Update the minimum NodeJS version from 14.x to 18.x
### AWS.Deploy.Recipes.CDK.Common (2.0.0)
### AWS.Deploy.ServerMode.Client (2.0.0)

## Release 2025-06-05

### AWS.Deploy.CLI (1.30.1)
* Update CDK Bootstrap template to version 28
### AWS.Deploy.Recipes.CDK.Common (1.30.1)
### AWS.Deploy.ServerMode.Client (1.30.1)

## Release 2025-05-01

### AWS.Deploy.CLI (1.30.0)
* Automatically deploy unsupported .NET versions using a self-contained build to Elastic Beanstalk
### AWS.Deploy.Recipes.CDK.Common (1.30.0)
### AWS.Deploy.ServerMode.Client (1.30.0)

## Release 2025-04-24

### AWS.Deploy.CLI (1.29.0)
* Add support for deploying ARM web apps to ECS Fargate
* Add support for deploying ARM console apps to ECS Fargate
* Add support for deploying ARM web apps to Elastic Beanstalk on Linux
### AWS.Deploy.Recipes.CDK.Common (1.29.0)
### AWS.Deploy.ServerMode.Client (1.29.0)

## Release 2025-03-28

### AWS.Deploy.CLI (1.28.2)
* Update the default docker image node version from version 18 to 22
### AWS.Deploy.Recipes.CDK.Common (1.28.2)
### AWS.Deploy.ServerMode.Client (1.28.2)

## Release 2024-12-30

### AWS.Deploy.CLI (1.28.1)
* Update the version of Amazon.CDK.Lib to 2.171.1
### AWS.Deploy.Recipes.CDK.Common (1.28.1)
* Update the version of Amazon.CDK.Lib to 2.171.1
### AWS.Deploy.ServerMode.Client (1.28.1)

## Release 2024-11-15

### AWS.Deploy.ServerMode.Client (1.28.0)
* Update Microsoft.AspNetCore.SignalR.Client version to fix System.Text.Json vulnerability
### AWS.Deploy.CLI (1.28.0)
* Update beanstalk platform resolution logic to additionally use 'Deprecated' versions in order to continue supporting .NET 6.
* Read region value for non default profiles
### AWS.Deploy.Recipes.CDK.Common (1.28.0)

## Release 2024-10-24

### AWS.Deploy.CLI (1.27.0)
* Added support for .NET 9 in deployment recipes.
* Added ability to configure EC2 IMDSv1 access for the Windows and Linux Elastic Beanstalk recipes.
* Support Elastic Beanstalk's transition to using EC2 Launch Templates from the deprecated Launch Configuration.
### AWS.Deploy.Recipes.CDK.Common (1.27.0)
### AWS.Deploy.ServerMode.Client (1.27.0)

## Release 2024-10-11

### AWS.Deploy.CLI (1.26.1)
* Update the CDK Bootstrap template to the latest version
* Removed the System.Text.Json dependency from the deployment project templates
### AWS.Deploy.Recipes.CDK.Common (1.26.1)
* Removed the System.Text.Json dependency from the deployment project templates
### AWS.Deploy.ServerMode.Client (1.26.1)
* Removed the System.Text.Json dependency from the deployment project templates

## Release 2024-09-27

### AWS.Deploy.CLI (1.26.0)
* Update the CDK Bootstrap template to the latest version
* Fix an issue causing container deployments to fail when run on an ARM-based system
