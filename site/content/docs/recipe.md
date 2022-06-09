#Deployment Recipes

#### Recommendation engine
At the heart of the tool there is a recommendation engine. When you run the tool, the recommendation engine inspects your .NET project and suggests the right deployment service that is most appropriate for your project.

This allows the engine to provide intelligent recommendations that are tailored to the type of .NET application that is being deployed. The recommendation rules are defined in the deployment recipes.

#### Deployment Recipes
The recommendation rules are defined in the deployment recipes.  The deployment tool turns these recipes into recommendations for a given .NET project.

There is a recipe for each project type. All recipes can be found on [GitHub](https://github.com/aws/aws-dotnet-deploy/tree/1344e9e8e5485d7d38af524657178facf27ec973/src/AWS.Deploy.Recipes/RecipeDefinitions). Each recipe contains the following:

1. [**JSON metadata file**](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json)- this file contains all of the metadata the deployment tool uses to drive the experience. This includes rules used in the recommendation engine to determine if the recipe is compatible with a project. It also has all of the settings that the deploy tool CLI, and eventually Visual Studio, uses to allow users to customize the experience.
2. **.NET CDK project template** -a recipe also contains a .NET project template that will be used to generate an [AWS Cloud Development Kit (CDK)](https://aws.amazon.com/cdk/) deployment project.

    > Note: There are a couple special case recipes that don't have CDK project templates. (Push to ECR and Deploy to existing Beanstalk).
