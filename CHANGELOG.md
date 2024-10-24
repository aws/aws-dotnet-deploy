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
