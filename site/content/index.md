# AWS Deploy Tool for .NET


****

#### About AWS Deploy Tool

**About AWS Deploy Tool** is an interractive tooling for the .NET CLI and the AWS Toolkit for Visual Studio that helps deploy .NET applications with minimum AWS knowledge, and with the fewest clicks or commands. It works by analyzing .NET codebases and guiding developers to the right AWS service. It then selects the right deployment service, builds and packages your application, generates the right IAM roles, and creates the deployment infrastructure. It allows for a quick and easy Proof of concept (POC), smooth graduation to CI/CD, and a gradual ramp up on AWS knowledge.

#### Key capabilities


+ Deploys to AWS Elastic Beanstalk, Amazon ECS (using AWS Fargate), AWS App Runner, and Amazon S3 (for Blazor WebAssembly).
+ Deploys cloud-native .NET applications that are built with .NET Core 3.1 and later, and that are written with the intent to deploy to Linux. Such an application isn't tied to any Windows-specific technology such as the Windows Registry, IIS, or MSMQ, and can be deployed on virtualized compute.
+ Deploys ASP.NET Core web apps, Blazor WebAssembly apps, long-running service apps, and scheduled tasks.

* **Assistance choose the right compute** – Get recommendations about the type of compute best suited for your application based on the application type.
* **Auto packaging and deployment** – Auto-generate IAM roles and permissions your application requires, generate (if needed) a Dockerfile for your application, build the deployment artifacts, and deploy them to the chosen AWS compute.
* **Repeatable and shareable deployments** – Persist configuration settings and AWS Cloud Development Kit (CDK) project used to deploy your application. You can also version control them and share with other team members for repeatable deployments.
* **Extensibility (Build your own deployment recipes)** - Promote organizational best practices across the company. You can extend the recommendation engine by defining your own deployment recipe that fits your deployment ecosystem, but using our specs for deployment. Custom recipes are NuGet packages which can be added to any .NET codebase, and will be incorporated into the included recommendation engine to provide the opinionated deployment.
* **Learn more about AWS!** - Once you are ready to start exploring, the deployment tool will help you gradually learn AWS tools like CDK that are used under the hood. It generates well documented/organized CDK projects that you can easily start modifying to fit your specific use-case.

#### How do I get started?

*  .NET CLI - AWS Deploy tool is available for Download as a separate NuGet package. To install it, please see ....

* AWS toolkit for Visual Studio - The toolkit exposes the same deployment functionality via  **Publish to AWS** feature. For information about toolkit versions and using the feature, see [Publish to AWS](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/publish-experience.html) in the [AWS Toolkit for Visual Studio User Guide](https://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/).


You can select deployment options interactively or specify them in a [JSON configuration file](docs/features/config-file.md). You can also keep the default values that the tool selects for you.

### FAQs
8. I have an existing .NET application. Can I use the deployment tool to deploy it to AWS?
Yes, the deployment tool can be used to deploy any cloud-native .NET applications to AWS. It cannot be used to deploy .NET Framework, Desktop, Xamarin, or other applications that do not fit the “cloud-native” criteria. (See General FAQ# 1)

9. How do the improved deployment tool decide what service to choose for deployment?
It examines .NET .csproj files and project dependencies, and inspects the code for attribution and Dockerfile presence to figure out the application type and the service best suited to run the application. We support deployments to AWS Elastic Beanstalk, Amazon AWS Lambda, and Amazon Elastic Container Service (AWS ECS) using AWS Fargate compute. [Internal note: With time we plan to add support for other services, such as Amazon Elastic Kubernetes Service (AWS EKS) and Fusion.]
10. How does the improved deployment tool decide what IAM roles are required?
It uses the .NET Roslyn compiler services to inspect the application code for AWS service calls made with the .NET SDK. It then creates the IAM role for the compute service with a profile generated from your application with required permissions. It does so by mapping the service calls to the IAM action names to generate the appropriate policy. You can also inspect your code during development to see what additional permissions your development profile requires to run the application.
11. How will my application be packaged and deployed?
Once the AWS compute service is chosen, the deployment tool will package your application binary artifacts in the appropriate format for that service (e.g. zip file for AWS Lambda) and deploy it using CDK. Once the application is deployed, it will return an endpoint for the application (e.g., URL for API backend, or SQS queue for messaging app).
12. How does the deployment tool create infrastructure?
It generates a .NET CDK project for the suggested service in your workspace and uses the .NET CDK binding to build constructs. If you are not ready to learn CDK, it will auto-generate the default .NET CDK project behind the scenes. You can also change or extend the CDK project’s behavior to match your exact needs and then execute the deployment.
13. Can I re-deploy my application to a different stack?
Yes. The deployment tool saves your deployment settings, including the environment name (a.k.a. name of the Cloud Formation stack) application was deployed to. When you re-run “dotnet aws deploy” command, it will detect these previously saved settings, and ask if you want to re-deploy to the same or a different environment. If you choose the latter, it will create a new deployment stack.
14. Can I choose a different AWS service to deploy my application?
The deployment tool will show you all compute service options available to deploy your application, and will recommend a default with information about why it was chosen. The other compute service options will be shown with an explanation of their differences. If the selected compute option does not match your need, you can select a different compute service.


#### Resources

+ The [aws-dotnet-deploy](https://github.com/aws/aws-dotnet-deploy) GitHub repo.
+ Blog post [Reimagining the AWS .NET deployment experience](http://aws.amazon.com/blogs/developer/reimagining-the-aws-net-deployment-experience/).
+ Blog post [Update on our new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/update-new-net-deployment-experience/).
+ Blog post [Deployment Projects with the new AWS .NET Deployment Experience](https://aws.amazon.com/blogs/developer/dotnet-deployment-projects/).
