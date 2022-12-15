# AWS .NET deployment tool [![nuget](https://img.shields.io/nuget/v/AWS.Deploy.Tools.svg) ![downloads](https://img.shields.io/nuget/dt/AWS.Deploy.Tools.svg)](https://www.nuget.org/packages/AWS.Deploy.Tools/)

## Overview
This repository contains the AWS Deploy Tool for .NET CLI - the opinionated tooling that simplifies deployment of .NET applications. The tool suggests the right AWS compute service to deploy your application to.  It then builds and packages your application as required by the chosen compute service, generates the deployment infrastructure, deploys your application by using the appropriate deployment engine (Cloud Development Kit (CDK) or native service APIs), and displays the endpoint.

The tool assumes minimal knowledge of AWS. It is designed to guide you through the deployment process and provides suggested defaults. The tool will show you all compute service options available to deploy your application, and will recommend a default with information about why it was chosen. The other compute service options will be shown with an explanation of their differences. If the selected compute option does not match your needs, you can select a different compute service.

The goal of the deployment tool is to deploy cloud-native .NET applications that are built with .NET Core 3.1 and above. A cloud-native .NET application is written in .NET with the intent to deploy to Linux. It is not tied to any Windows specific technology such as Windows registry, IIS or MSMQ, and can be deployed on virtualized compute. The tool **cannot** be used to deploy .NET Framework, Desktop, Xamarin, or other applications that do not fit the "cloud-native" criteria.

We welcome your feedback! Please let us know what you think by opening an [issue](https://github.com/aws/aws-dotnet-deploy/issues).


## Useful Links
* [Complete Documentation Guide on GitHub.io](https://aws.github.io/aws-dotnet-deploy/)
* [Contributing to the Project](https://aws.github.io/aws-dotnet-deploy/contributing/)
* [AWS Deploy Tool for .NET on NuGet](https://www.nuget.org/packages/AWS.Deploy.Tools)
* Blog posts:
  * [AWS Streamlines Deployment experience for .NET applications](https://aws.amazon.com/blogs/developer/aws-announces-a-streamlined-deployment-experience-for-net-applications/)
  * [Reimagining the AWS .NET deployment experience](http://aws.amazon.com/blogs/developer/reimagining-the-aws-net-deployment-experience/)
  * [Update on our new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/update-new-net-deployment-experience/)
  * [Deployment Projects with the new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/dotnet-deployment-projects/)
* Youtube videos:
  * [AWS On Air ft. New .NET Deployment Experience - Command Line](https://www.youtube.com/watch?v=5uyL8MXxljc)
  * [Re:Invent 2021: “What’s new with .NET development and deployment on AWS”](https://www.youtube.com/watch?v=UvTJ_Inb634)

## Pre-requisites

To take advantage of this library you’ll need:

* An AWS account with a local credential profile configured in the shared AWS config and credentials files.
  * The local credential profile can be configured by a variety of tools. For example, the credential profile can be configured with the [AWS Toolkit for Visual Studio](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/credentials.html) or the [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html), among others.
  * Note: You need to make sure to add the appropriate CloudFormation permissions to your credentials's profile / assumed role.
  * For SSO, please visit the [.NET SDK Reference Guide](https://docs.aws.amazon.com/sdkref/latest/guide/access-sso.html).
* [.NET 6](https://dotnet.microsoft.com/download) or later
* [Node.js 14](https://nodejs.org/en/download/) or later
  * The [AWS Cloud Development Kit (CDK)](https://aws.amazon.com/cdk/) is used by this tool to create the AWS infrastructure to run applications. The CDK requires Node.js to function. This dependency is needed for deployments that are CDK based. If you will be using deployments that are not CDK based, you are not required to have this dependency.
* (optional) [Docker](https://docs.docker.com/get-docker/)
  * Used when deploying to a container based service like Amazon Elastic Container Service (Amazon ECS)
* (optional) The zip cli tool
   *   Mac / Linux only. Used when creating zip packages for deployment bundles. The zip cli is used to maintain Linux file permissions.

## Getting started

The deployment tool is distributed as a .NET Tool from NuGet.org. The installation of the tool is managed with the `dotnet` CLI.

### Installing the tool

To install the deployment tool, use the dotnet tool install command:

```
dotnet tool install -g aws.deploy.tools
```

To update to the latest version of the deployment tool, use the dotnet tool update command.

```
dotnet tool update -g aws.deploy.tools
```

To uninstall it, simply type:
```
dotnet tool uninstall -g aws.deploy.tools
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
ASP.NET Core applications can be deployed either to virtual servers with AWS Elastic Beanstalk or containers with Amazon Elastic Container Service (Amazon ECS) and AWS App Runner. If you wish to deploy your application as a container and your project does not yet have a `Dockerfile` one will be generated for you into your project during deployment.
Using the deployment tool, you will be able to deploy ASP.NET Core web applications to AWS using the AWS Cloud Development Kit (CDK). 
For users coming from the previous AWS Visual Studio Toolkit experience who have previous Elastic Beanstalk deployments and environments, you will be able to use the deployment tool to deploy to those environments.

#### Elastic Beanstalk backwards compatibility support
Currently, the AWS Visual Studio toolkit allows users to deploy their ASP.NET Core web applications to Elastic Beanstalk using a wizard that uses the AWS .NET SDK on the backend to perform the deployment. This process creates the necessary Elastic Beanstalk resources such as a Beanstalk Application and Beanstalk Environment using the AWS .NET SDK. 

The deployment tool uses AWS CDK to perform the deployments to AWS which create a CloudFormation stack that manages all the Elastic Beanstalk resources. In addition to CloudFormation stacks, the tool supports deployments to the existing Elastic Beanstalk environments that were created using the the older version of the AWS Toolkit for Visual Studio.

The deployment tool will detect existing Elastic Beanstalk environments in your AWS account and list them alongside the CloudFormation stacks. You can deploy your application to an existing Beanstalk environment and update the necessary environment settings. The deployment will be performed using the AWS .NET SDK.

Note: Deploying to existing Beanstalk Environments will not migrate your existing resources to CloudFormation. To create a new CloudFormation stack, you need to explicitly deploy your application to the new deployment target.

### Blazor WebAssembly applications
Blazor WebAssembly applications can be deployed to an Amazon S3 bucket for web hosting. The Amazon S3 bucket will be created and configured automatically by the tool, which will then upload your Blazor application to the S3 bucket.

### Long running service applications
Programs that are meant to run indefinitely can be deployed as an Amazon ECS service. This is common for backend services that process messages. The application will be deployed as a container image. If your project does not yet have a Dockerfile, one will be generated for you into your project during deployment.

### Schedule tasks
Programs that need to run periodically, for example, once every hour, can be deployed as a schedule task using Amazon ECS and Amazon CloudWatch Events. The application will be deployed as a container image. If your project does not yet have a `Dockerfile`, one will be generated for you into your project during deployment.

## Supported AWS Services

### AWS Elastic Beanstalk
AWS Elastic Beanstalk is an easy-to-use service for deploying and scaling web applications and services. Elastic Beanstalk automatically handles the deployment, from capacity provisioning, load balancing, auto-scaling to application health monitoring. At the same time, you retain full control over the AWS resources powering your application and can access the underlying resources at any time

### Amazon Elastic Container Service
Amazon ECS is a fully managed container orchestration service that helps you easily deploy, manage, and scale containerized applications. It deeply integrates with the rest of the AWS platform to provide a secure and easy-to-use solution for running container workloads in the cloud and now on your infrastructure with Amazon ECS Anywhere.

### AWS App Runner
AWS App Runner is a fully managed service that makes it easy for developers to quickly deploy containerized web applications and APIs, at scale and with no prior infrastructure experience required. Start with your source code or a container image. App Runner builds and deploys the web application automatically, load balances traffic with encryption, scales to meet your traffic needs, and makes it easy for your services to communicate with other AWS services and applications that run in a private Amazon VPC. With App Runner, rather than thinking about servers or scaling, you have more time to focus on your applications.

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
