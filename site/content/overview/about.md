# About AWS Deploy Tool

**About AWS Deploy Tool** is an interractive tooling for the .NET CLI and the AWS Toolkit for Visual Studio that helps deploy .NET applications with minimum AWS knowledge, and with the fewest clicks or commands. It works by analyzing .NET codebases and guiding developers to the right AWS service. It then selects the right deployment service, builds and packages your application, generates the right IAM roles, and creates the deployment infrastructure. It allows for a quick and easy Proof of concept (POC), smooth graduation to CI/CD, and a gradual ramp up on AWS knowledge.

## Key capabilities

AWS Deploy Tool has the following capabilities:

* **Compute recommendations for your application** – Get recommendations about the type of compute best suited for your application based on the application type.
* **Dockerfile  generation** - The tool will generate the Dockerfile .... if needed, otherwise an existing Dockerfile will be used.
* **Auto packaging and deployment** – Auto-generate IAM roles and permissions your application requires, build the deployment artifacts, generate a deployment CDK project, provision the infrastructure and  deploy your application to the chosen AWS compute.
* **Repeatable and shareable deployments** – Persist configuration settings and AWS Cloud Development Kit (CDK) project used to deploy your application. You can also version control them and share with other team members for repeatable deployments.
* **Extensibility (Build your own deployment recipes)** - Promote organizational best practices across the company. You can extend the recommendation engine by defining your own deployment recipe that fits your deployment ecosystem, but using our specs for deployment. Custom recipes are NuGet packages which can be added to any .NET codebase, and will be incorporated into the included recommendation engine to provide the opinionated deployment.
* **Help with learning AWS!** - Once you are ready to start exploring, the deployment tool will help you gradually learn AWS tools like CDK that are used under the hood. It generates well documented/organized CDK projects that you can easily start modifying to fit your specific use-case.

## Availability
### ... in .NET CLI

AWS Deploy tool is available for download as a NuGet package. See [How to install](docs/getting-started/installation.md) section.

### ... in AWS Toolkit for Visual Studio
The AWS Toolkit for Visual Studio exposes the same deployment functionality via  **Publish to AWS** feature. For information about toolkit versions and using the feature, see [Publish to AWS](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/publish-experience.html) in the [AWS Toolkit for Visual Studio User Guide](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/).

## What's new?

TODO
