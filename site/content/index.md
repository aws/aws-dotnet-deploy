

# AWS .NET deployment tool for the .NET CLI<a name="deployment-tool"></a>


****  

After you've developed your cloud-native .NET Core application on a development machine, you'll most likely want to deploy it to AWS.

Deployment to AWS sometimes involves multiple AWS services and resources, each of which must be configured. To ease this deployment work, you can use the AWS .NET deployment tool for the .NET CLI, or *deployment tool* for short.

The deployment tool is an opinionated tooling that simplifies deployment of .NET applications. The tool suggests the right AWS compute service to deploy your application to. It then builds and packages your application as required by the chosen compute service, generates the deployment infrastructure, deploys your application by using the appropriate deployment engine (Cloud Development Kit (CDK) or native service APIs), and displays the endpoint.

> **Note**

> If you develop on Windows with Visual Studio, you might have the AWS Toolkit for Visual Studio installed. The toolkit provides similar deployment functionality in its **Publish to AWS** feature. For information about toolkit versions and using the feature, see [Publish to AWS](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/publish-experience.html) in the [AWS Toolkit for Visual Studio User Guide](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/).

When you run the deployment tool for a .NET Core application, the tool shows you all of the AWS compute-service options that are available to deploy your application. It suggests the most likely choice, as well as the most likely settings to go along with that choice. It then builds and packages your application as required by the chosen compute service. It generates the deployment infrastructure, deploys your application by using the AWS Cloud Development Kit (CDK), and then displays the endpoint.

You can select deployment options interactively or specify them in a [JSON configuration file](docs/features/config-file.md). You can also keep the default values that the tool selects for you.

**Capabilities**

+ Deploys to AWS Elastic Beanstalk, Amazon ECS (using AWS Fargate), AWS App Runner, and Amazon S3 (for Blazor WebAssembly).
+ Deploys cloud-native .NET applications that are built with .NET Core 3.1 and later, and that are written with the intent to deploy to Linux. Such an application isn't tied to any Windows-specific technology such as the Windows Registry, IIS, or MSMQ, and can be deployed on virtualized compute.
+ Deploys ASP.NET Core web apps, Blazor WebAssembly apps, long-running service apps, and scheduled tasks. For more information, see the [README](https://github.com/aws/aws-dotnet-deploy#supported-application-types) in the GitHub repo.

**Additional information**

+ The [aws-dotnet-deploy](https://github.com/aws/aws-dotnet-deploy) GitHub repo.
+ Blog post [Reimagining the AWS .NET deployment experience](http://aws.amazon.com/blogs/developer/reimagining-the-aws-net-deployment-experience/).
+ Blog post [Update on our new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/update-new-net-deployment-experience/).
+ Blog post [Deployment Projects with the new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/dotnet-deployment-projects/).

