# Deployment projects

### What is a deployment project?

A deployment project allows you to customize the resources you deploy to AWS. For example if your deployment require additional application resources like a DynamoDB tables or SQS queues those resources could be added to the deployment project. 

When you have a deployment project you use the same deployment experience starting deployment with your application you want to deploy. The list of deployment recommendations will include your deployment projects.

### Getting started

To get started creating a deployment project execute the following command in the directory of the .NET project to deploy.

    dotnet aws deployment-project generate --output <output-directory> --project-display-name <display-name> 

The `--output` switch sets the directory where the deployment project will be saved to. To customize the name the deployment project that will be shown to users when the .NET project is being deployed use the `--project-display-name` switch.

Once you run the `dotnet aws deployment-project generate` the tool will display a list of system recipes that are compatible for the .NET project. Choose the recipe that is closest to what your project needs. These recipes will be used as the starting point for your deployment project that you can later customize.

Select the starting recipe and then a deployment project will be created in the location of the `--output` directory. Now you can begin customizing the deployment project.


### Add to source control

When you use a deployment project to deploy a .NET project it is important to add the deployment project to source control. Redeployments require the deployment project to be available to the deployment tooling. If a CloudFormation stack was created from a deployment project and that 
deployment project has been deleted then you will not be able to redeploy to that CloudFormation stack.


### Searching for deployment projects

When the deploy command is initiated the tooling searchs for the solution of the .NET project and searches for deployment project anywere underneath the solution directory. The deployment projects are sent through the deployment tooling's recommendation engine to make sure they are compatible with the project being deployed.

If the deployment project is outside of the solution directory the `--deployment-project` switch for the `dotnet aws deploy` command can be used to pass in the path of the deployment project to use. This is common for shared deployment-projects across multiple solutions.

### Parts of a deployment project

A deployment project is made of 2 parts. Click on the links below for a information about each part.

* [Recipe JSON file](./recipe-file.md) - a JSON file that drives the deployment experience in the deploy tool CLI and AWS Toolkit for Visual Studio.  
* [.NET CDK project](./cdk-project.md) - a C# project that uses the CDK to define the infrastructure that will be created for the deployment project. 
