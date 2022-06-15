# Deployment projects

### What is a deployment project?

A deployment project allows you to customize the resources you deploy to AWS. Instead of choosing one of the recipes supplied by AWS, you can generate a deployment project and modify it to fit your needs.

For simple deployments where you don’t want to further customize resources, you can feel free to ignore the CDK project. You can customize this project, or leave it as-is.


Each deployment project includes:

* **.NET CDK project** - This auto-generated project takes the collected settings from the user and performs the deployment using the AWS CDK.
* **JSON metadata file** - contains all of the metadata the deployment tool uses to drive the experience. This includes rules used in the recommendation engine to determine if the recipe is compatible with a project. It also has all of the settings that the deploy tool CLI, and eventually Visual Studio, uses to allow users to customize the experience.

(https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json)-

### How to generate deployment project

You can also explicitly generate deployment project into your workspace. You can then customize the project and the deployment settings to fit your needs.

First, generate the deployment project in the directory of your choice:

    > dotnet aws deployment-project generate --output ../myCustomCDKProject --project-display-name MyCustomCDKProject

In the command above, the `--project-display-name` sets the name for the project that will be seen to you during the deployment.

Now you can go ahead and add additional resources or your custom requirements to the generated deployment project. In the example below, we’ll show you how to add a DynamoDB table....
<TODO add example here for DynamoDB>

   > **Note: It is important to save the generated deployment project in version control because it is required for re-deployments.**

### Specifying external deployment project

Instead of choosing one of the recipes supplied by AWS, you can choose your custom deployment project.

<TODO>


  > Note: A temporary deployment project is generated implicitly in %userprofile%/.aws-dotnet-deploy/ directory each time you run `dotnet aws deploy` command.
