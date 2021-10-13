# AWS .NET deployment tool


This repository contains the AWS .NET deployment tool for .NET CLI - the opinionated tooling that simplifies deployment of .NET applications with minimum AWS knowledge. The tool suggests the right AWS compute service to deploy your application to.  It then builds and packages your application as required by the chosen compute service, generates the deployment infrastructure, deploys your application by using the Cloud Development Kit (CDK), and displays the endpoint.

The tool assumes minimal knowledge of AWS. It is designed to guide you through the deployment process and provides suggested defaults. The tool will show you all compute service options available to deploy your application, and will recommend a default with information about why it was chosen. The other compute service options will be shown with an explanation of their differences. If the selected compute option does not match your needs, you can select a different compute service.

The goal of the deployment tool is to deploy cloud-native .NET applications that are built with .NET Core 2.1 and above. A cloud-native .NET application is written in .NET with the intent to deploy to Linux. It is not tied to any Windows specific technology such as Windows registry, IIS or MSMQ, and can be deployed on virtualized compute. The tool **cannot** be used to deploy .NET Framework, Desktop, Xamarin, or other applications that do not fit the "cloud-native" criteria.

## Project Status
The tool is currently in **developer preview**. It currently has limited support for deployment targets and the settings for those targets. We are looking for feedback on the type of applications users want to deploy to AWS and what features are important to them. Please provide your feedback by opening an [issue in this repository](https://github.com/aws/aws-dotnet-deploy/issues).

## Pre-requisites

To take advantage of this library you’ll need:

* An AWS account with a local credential profile configured in the shared AWS config and credentials files.
  * The local credential profile can be configured by a variety of tools. For example, the credential profile can be configured with the [AWS Toolkit for Visual Studio](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/credentials.html) or the [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html), among others.
* [.NET Core 3.1](https://dotnet.microsoft.com/download) or later
* [Node.js 10.3](https://nodejs.org/en/download/) or later
  * The [AWS Cloud Development Kit (CDK)](https://aws.amazon.com/cdk/) is used by this tool to create the AWS infrastructure to run applications. The CDK requires Node.js to function.
* (optional) [Docker](https://docs.docker.com/get-docker/)
  * Used when deploying to a container based service like Amazon Elastic Container Service (Amazon ECS)
* (optional) The zip cli tool
   *   Mac / Linux only. Used when creating zip packages for deployment bundles. The zip cli is used to maintain Linux file permissions.
## Getting started

The deployment tool is distributed as a .NET Tool from NuGet.org. The installation of the tool is managed with the `dotnet` CLI.

### Installing the tool

To install the deployment tool, use the dotnet tool install command:

```
dotnet tool install -g aws.deploy.cli
```

To update to the latest version of the deployment tool, use the dotnet tool update command.

```
dotnet tool update -g aws.deploy.cli
```

To uninstall it, simply type:
```
dotnet tool uninstall -g aws.deploy.cli
```

Once you install the tool, you can view the list of available commands by typing:
```
dotnet aws --help
```

To get help about individual commands like deploy or delete-deployment you can use the `--help` switch with the commands. For example to get help for the deploy command type:
```
dotnet aws deploy --help
```

## Deploying your application

To deploy your application, `cd` to the directory that contains the .csproj or .fsproj file and type:
```
dotnet aws deploy
```

*(Alternatively the `--project-path` switch can be used to point to specific directory or project file.)*

You will be prompted to enter the name of the stack that your application will be deployed to. (A **stack** is a collection of **AWS** resources that you can manage as a single unit. In other words, you can create, update, or delete a collection of resources by creating, updating, or deleting stacks.)

Once you enter the name of the stack, the deployment tool's recommendation engine will inspect your project codebase and provide its recommendation for how you should deploy the application. If possible the tool will also show other compatible ways to deploy the application that you can choose to use over the recommendation.

```
Name the AWS stack to deploy your application to
(a stack is a collection of AWS resources that you can manage as a single unit.)
--------------------------------------------------------------------------------
Enter value (default MyApplication):


Recommended Deployment Option
-----------------------------
1: ASP.NET Core App to Amazon ECS using Fargate
ASP.NET Core applications built as a container and deployed to Amazon Elastic Container Service (ECS) with compute power managed by AWS Fargate compute engine. Recommended for applications that can be deployed as a container image. If your project does not contain a Dockerfile, one will be generated for the project.

Additional Deployment Options
------------------------------
2: ASP.NET Core App to AWS Elastic Beanstalk on Linux
Deploy an ASP.NET Core application to AWS Elastic Beanstalk. Recommended for applications that are not set up to be deployed as containers.

Choose deployment option (recommended default: 1)
```

## Supported application types

### ASP.NET Core web applications
ASP.NET Core applications can be deployed either to virtual servers with AWS Elastic Beanstalk or containers with Amazon Elastic Container Service (Amazon ECS). If you wish to deploy your application as a container and your project does not yet have a `Dockerfile` one will be generated for you into your project during deployment.

### Blazor WebAssembly applications
Blazor WebAssembly applications can be deployed by creating and configuring an Amazon S3 bucket for web hosting. Your Blazor application will then be uploaded to the S3 bucket.

### Long running service applications
Programs that are meant to run indefinitely can be deployed as an Amazon ECS service. This is common for backend services that process messages. The application will be deployed as a container image. If your project does not yet have a Dockerfile, one will be generated for you into your project during deployment."


### Schedule tasks
Programs that need to run periodically, for example, once every hour, can be deployed as a schedule task using Amazon ECS and Amazon CloudWatch Events. The application will be deployed as a container image. If your project does not yet have a `Dockerfile`, one will be generated for you into your project during deployment.

## Getting Help

For feature requests or issues using this tool please open an [issue in this repository](https://github.com/aws/aws-dotnet-deploy/issues).

## Contributing
We welcome community contributions and pull requests. See [CONTRIBUTING](https://github.com/aws/aws-dotnet-deploy/blob/main/CONTRIBUTING.md) for information on how to set up a development environment and submit code.

## Additional Resources

* [AWS Developer Center - Explore .NET on AWS](https://aws.amazon.com/developer/language/net/) Find all the .NET code samples, step-by-step guides, videos, blog content, tools, and information about live events that you need in one place.
* [AWS Developer Blog - .NET](https://aws.amazon.com/blogs/developer/category/programing-language/dot-net/) Come see what .NET developers at AWS are up to! Learn about new .NET software announcements, guides, and how-to's.
* [@dotnetonaws](https://twitter.com/dotnetonaws) Follow us on twitter!

## License

Libraries in this repository are licensed under the Apache 2.0 License.
See [LICENSE](https://github.com/aws/aws-dotnet-deploy/blob/main/LICENSE) and [NOTICE](https://github.com/aws/aws-dotnet-deploy/blob/main/NOTICE) for more information.
