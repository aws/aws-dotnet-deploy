# Custom Deployment Projects

AWS Deploy Tool comes with multiple recipes that can deploy your .NET application to [several different AWS compute services](../support.md). These built-in recipes let you configure AWS resources related to your application such as IAM roles, virtual private clouds, and load balancers.

But what if your application uses additional AWS resources that are not included in the built-in recipes, such as DynamoDB tables or SQS queues? 

You can create a **custom deployment project** to expand one of the built-in recipes to manage additional AWS resources and services. 

Once you create a custom deployment project, the AWS Deploy Tool CLI and the AWS Toolkit for Visual Studio will display it alongside the built-in recipes and offer users the same deployment experience.

### Parts of a custom deployment project

A custom deployment project is comprised of two parts. You can follow the getting start guide on this page or the [tutorial](./tutorial.md) to create these, then refer to the full reference guides below.

* [Recipe file](./recipe-file.md) - a JSON file that drives the user experience in both the deploy tool CLI and AWS Toolkit for Visual Studio.  
* [.NET CDK project](./cdk-project.md) - a C# project that uses the [AWS Cloud Development Kit (CDK)](https://aws.amazon.com/cdk/) to define the infrastructure that will be created for the deployment project. 

### Getting started

To create a custom deployment project, execute the following command in the directory of the .NET project you wish to deploy.

    dotnet aws deployment-project generate --output <output-directory> --project-display-name <display-name> 

The `--output` switch sets the directory where the deployment project will be saved. Use the `--project-display-name` switch to customize the name of the custom recipe that will be shown to users when the .NET project is being deployed.

When you run the above command, the tool will display a list of built-in recipes that are compatible with your .NET project. Choose the built-in recipe that is closest to what you wish to deploy. This will be used as the starting point that you can then customize.

Select the starting recipe and then a deployment project will be created in the location of the `--output` directory. Now you can begin customizing the deployment project.


### Add to source control

It is important to add the custom deployment project to source control. Redeployments require the custom deployment project to be available to the deployment tooling. If a CloudFormation stack was created from a custom deployment project and that 
deployment project has been deleted then you will not be able to redeploy to that CloudFormation stack.


### Searching for custom deployment projects

When the deploy command is initiated AWS Deploy Tool starts from the solution (.sln) of the .NET project being deployed and searches for custom deployment projects anywhere underneath the solution directory. The custom deployment projects are sent through the deployment tooling's recommendation engine to make sure they are compatible with the .NET project being deployed.

If the custom deployment project is outside of the solution directory use the `--deployment-project` switch for the `dotnet aws deploy` command to pass in the path of the custom deployment project to use. This is common when sharing custom deployment projects across multiple solutions.

