# Deployment projects

### What is a deployment project?

A deployment project allows you to customize the resources you deploy to AWS. Instead of choosing one of the recipes supplied by AWS, you can generate a deployment project and modify it to fit your application needs.

Each deployment project includes:

* **.NET CDK project** - takes the collected settings from the user and performs the deployment using the AWS CDK.  It’s a standard C# console project that uses NuGet packages containing CDK construct types to define the AWS infrastructure.
* **JSON metadata file** - a JSON configuration file that contains all of the settings that are needed to determine which deployment services to use and their configurations. Here is the [JSON file definition](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions).

### Generating a project

You can generate deployment project into your workspace. The `--project-display-name` sets the name for the project that will be seen to you during the deployment.

    dotnet aws deployment-project generate --project-display-name MyCustomCDKProject

You can also generate the deployment project in the directory of your choice by specifying the `--output` parameter.

    dotnet aws deployment-project generate --output ../myCustomCDKProject --project-display-name MyCustomCDKProject

You can then customize the project and the deployment settings to fit your needs.

The `AppStack class` is the recommended place to add new AWS resources or customize the resources created in the generated code.

### Customizing deployment resources

Now you can go ahead and add additional resources to the generated deployment project. Deployment projects enable you to change the deployment interface.

  > Note: Most of the code is located in the generated folder. We don’t recommend you edit the files in there directly, and instead use it for reference. If you want to take updates from the original recipe the deployment project was created from, you can just copy the code into the generated folder.

In the example below, we’ll show you how to add a DynamoDB table....
<TODO add example here for DynamoDB>

### Customizing deployment settings
You can also add new settings that will be presented to the user during deployment.
TODO


### Specifying external project

Now that you created your custom deployment project, you can choose it over the default recipes supplied by AWS.

 By default, when you invoke the `dotnet aws deploy` command, it looks for all deployment projects under the directory where the solution file is at, or the root of the Git repository.

 If you have a deployment project you want to use but it’s in a separate workspace, possibly a separate repository, then use the `--deployment-project` switch to pass in the path of the shared deployment project.

    dotnet aws deploy --deployment-project [PATH_TO_CUSTOM_DEPLOYMENT_PROJECT]

### Sharing deployment projects

It is important to save the generated deployment project in version control so that uou can reuse a project for multiple applications you want to deploy or share them with the rest of your team.
