# About the AWS Deploy Tool for .NET

** AWS Deploy Tool for .NET** is an interactive tooling for the .NET CLI and the AWS Toolkit for Visual Studio that helps deploy .NET applications with minimum AWS knowledge, and with the fewest clicks or commands.

### Key capabilities

AWS Deploy Tool has the following capabilities:

* **Compute recommendations for your application** – Get recommendations about the type of compute best suited for your application based on the application type.
* **Dockerfile  generation** - The tool will generate a Dockerfile if needed, otherwise an existing Dockerfile will be used.
* **Auto packaging and deployment** – The tool builds the deployment artifacts, generates a deployment CDK project, provisions the infrastructure and deploys your application to the chosen AWS compute.
* **Repeatable and shareable deployments** – You can generate and modify AWS Cloud Development Kit (CDK) deployment projects to fit your specific use case. You can also version control your projects and share them with your team for repeatable deployments.
* **Help with learning AWS CDK for .NET!** - Gradually learn the underlying AWS tools that AWS Deploy Tool for .NET is built on, such as the AWS CDK.

### Availability
####... in .NET CLI

AWS Deploy Tool for .NET is available for download as a NuGet package. See [How to install](docs/getting-started/installation.md) section.

#### ... in AWS Toolkit for Visual Studio
The AWS Toolkit for Visual Studio exposes the same deployment functionality via  **Publish to AWS** feature. For information about toolkit versions and using the feature, see [Publish to AWS](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/publish-experience.html) in the [AWS Toolkit for Visual Studio User Guide](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/).

### Additional Resources

* The [aws-dotnet-deploy](https://github.com/aws/aws-dotnet-deploy) GitHub repo.
* Blog Post: [Reimagining the AWS .NET deployment experience](http://aws.amazon.com/blogs/developer/reimagining-the-aws-net-deployment-experience/).
* Blog Post: [Update on our new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/update-new-net-deployment-experience/).
* Blog Post: [Deployment Projects with the new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/dotnet-deployment-projects/).
* Video: Re:Invent 2021: [“What’s new with .NET development and deployment on AWS”](https://www.youtube.com/watch?v=UvTJ_Inb634)
