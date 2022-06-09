### Overview

The deployment tool examines your .NET project and generates a CDK deployment project in your workspace. Deployment tool then uses this deployment project to provision the necessary infrastructure and deploy your application.

Each generated deployment project includes:

* *.NET CDK project* - This auto-generated project takes the collected settings from the user and performs the deployment using the AWS CDK. For simple deployments where you don’t want to further customize resources, you can feel free to ignore the CDK project. You can customize this project, or leave it as-is.
* *JSON metadata file* (https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json)- this file contains all of the metadata the deployment tool uses to drive the experience. This includes rules used in the recommendation engine to determine if the recipe is compatible with a project. It also has all of the settings that the deploy tool CLI, and eventually Visual Studio, uses to allow users to customize the experience.


Deployment project can be generated implicitly or explicitly. By default, the deployment project is generated in %userprofile%/.aws-dotnet-deploy directory each time you run a deployment tool.

You can also explicitly generate deployment project into your workspace. You can then customize the project and the deployment settings to fit your needs.
