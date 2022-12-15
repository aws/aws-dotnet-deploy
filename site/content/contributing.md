The AWS Deploy Tool for .NET is an open source project hosted on [GitHub](https://github.com/aws/aws-dotnet-deploy) and we openly welcome community contributions.

[Click here](https://github.com/aws/aws-dotnet-deploy/compare) to submit a pull request.


## Using the Deployment Tool from Source

* Ensure that all the [pre-requisties](https://aws.github.io/aws-dotnet-deploy/docs/getting-started/pre-requisites/) (including Docker) are installed and your default AWS profile has admin access
* Clone the repository using `git clone https://github.com/aws/aws-dotnet-deploy.git`
* Open `AWS.Deploy.sln` in Visual Studio.
* Ensure that `src/AWS.Deloy.CLI` is set as the start up project.
* Go to `AWS.Deploy.CLI debug properties` by clicking the following drop down
![](./assets/images/debugPropertiesDropdown.png)
* Add a new launch profile called `AWS.Deploy.CLI`
* In the *Command line arguments* text box add the following (leave other text boxes blank):
```

deploy —project-path path/to/aws-dotnet-deploy/testapps/WebAppWithDockerFile

```
*The above command assumes you have a [default] profile configured in your AWS credentials file. If you want to use a different profile, use the `--profile` switch. You can also specify a different region via the `--region` switch.*

* Select the `AWS.Deploy.CLI` launch profile and run the tool
* Follow the prompts on the screen and perform a deployment using any compatible recommendation.
* A successful deployment indicates that your environment is set up correctly and you can proceed to debugging the source code.


## Repository Overview

The .NET deployment tool comprises of various top level components.

### [AWS.Deploy.CLI](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.CLI)

This component controls the UI elements for the .NET deployment tool and serves as the first entry point when running the tool. This is the layer that end users directly interact with and it uses the [System.CommandLine](https://docs.microsoft.com/en-us/dotnet/standard/commandline/) package for the CLI UX.

**Key Components**

* **[Program.cs](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Program.cs)** - Serves as the entry point to the CLI application.

* **[CustomServiceCollectionExtension](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Extensions/CustomServiceCollectionExtension.cs)** - This serves as the dependency injection container and registers all the dependencies required by the deployment tool.

* **[CommandFactory](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Commands/CommandFactory.cs)** - This instantiates and executes the top-level ***dotnet aws*** command and other sub-commands like [*deploy*](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Commands/DeployCommand.cs), [*list-deployments*](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Commands/ListDeploymentsCommand.cs) and [*delete-deployment*](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Commands/DeleteDeploymentCommand.cs)*.* It also holds all the [CLI switches](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Commands/CommandFactory.cs#L40-#L49) that are applicable to the any command. All new commands/switches will be declared and instantiated via this class.

* **[CommandHandlerInput](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.CLI/Commands/CommandHandlerInput)** - This contains the POCO objects that hold the values for all CLI switches of the associated command. These objects are provided as input while creating the command handlers. See [here](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.CLI/Commands/CommandFactory.cs#L185) for an example.

* **[TypeHintCommandFactory](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Commands/TypeHints/TypeHintCommandFactory.cs)** - Some of the option settings require special logic on how to prompt the user for a value and how to parse the user input. This class stores the mapping between the option settings and their corresponding typehints. **TypeHints** can be found [here](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.CLI/Commands/TypeHints). **TypeHintResponses** can be found [here](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.CLI/TypeHintResponses)

* **[ConsoleUtilities](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/ConsoleUtilities.cs)** - Contains utility methods to drive the CLI UX

* **[CommandLineWrapper](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Utilities/CommandLineWrapper.cs)** - Contains utility methods to invoke command line processes like `docker build` or `cdk deploy`

* **[Exceptions](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.CLI/Exceptions.cs)** - Declares all exceptions that are thrown by the AWS.Deploy.CLI component

* **[AWS.Deploy.CLI.UnitTests](https://github.com/aws/aws-dotnet-deploy/tree/main/test/AWS.Deploy.CLI.UnitTests)** - Contains unit tests for this component


### [AWS.Deploy.Recipes](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes)

This component contains the recipes that serve as the backbone of each deployment recommendation. It also contains the CDK templates that is used to deploy the customer’s .NET application as a CloudFormation stack

**Key Components**

* **[RecipeDefinitions](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions)** - It contains the deployment recipes that target different AWS services. Each recipe file contains a list of option settings which is a collection of AWS resources that users can customize as per their needs.
    * See [here](https://aws.github.io/aws-dotnet-deploy/docs/deployment-projects/recipe-file/) to learn more about the recipe file schema.
    * See [here](https://aws.github.io/aws-dotnet-deploy/docs/support/) to look at the support matrix between recipes and different .NET application types.

* **[DeploymentBundleDefinitions](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/DeploymentBundleDefinitions)** - It contains the common option settings that users can customize for container based and non-container based recipes. All recipes have a [DeploymentBundle](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L59) property that specifies how to package the user’s .NET project for deployment.

    * *Container* - Indicates that Docker is used to build the Docker image and pushed to Amazon Elastic Container Registry.
    * *DotnetPublishZipFile -* Indicates that the `dotnet publish` is used to prepare the user’s .NET project for deployment.

* **[CdkTemplates](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/CdkTemplates)** - It contains the CDK templates for recipe files found [here](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions). All recipes with the [DeploymentType](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L53) set to *“CdkProject”* have their own specific CDK template identified by the [CdkProjectTemplate](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L65) property.


### [AWS.Deploy.CDK.Common](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes.CDK.Common)

This component contains the common utility classes and methods that are used by the CDK templates found [here](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/CdkTemplates).

**Note** - The `AWS.Deploy.CDK.Common.csproj` is never referenced directly because it contains the [Amazon.CDK.Lib](https://www.nuget.org/packages/Amazon.CDK.Lib) package which is close to 70 MB is size. Instead, if we need functionality from this project then we directly link the appropriate `.cs` files. See [here](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/AWS.Deploy.Orchestration.csproj#L11) for an example.


### [AWS.Deploy.Common](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Common)

This component contains POCO objects and common logic that is used by other components in the deployment tool

**Note** - This project does not reference any other project except for `../src/AWS.Deploy.Constants/AWS.Deploy.Constants.projitems`

**Key Components**

* **[ProjectDefinitionParser](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/ProjectDefinitionParser.cs)** - This class contains the logic to validate whether a .NET project exists at the path specified via `dotnet aws deploy —project-path <PROJECT-PATH>`. It also parses the user’s csproj/fsproj file and returns a [ProjectDefinition](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/ProjectDefinition.cs)

* **[RecipeDefinition](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions)** - This is a POCO class that serves as the deserialized model for all recipe files found [here](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions)

* **[Recommendation](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/Recommendation.cs)** - This is a POCO class that stores different metadata such as the [RecipeDefinition](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions) and [ProjectDefinition](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/ProjectDefinition.cs). There is 1:1 mapping between each recipe file and a recommendation object. Each recommendation object has a computed priority which determines its precedence.

* **[DeploymentBundleDefinition](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/DeploymentBundles/DeploymentBundleDefinition.cs)** - This is a POCO class that serves as the deserialized model for all [DeploymentBundleDefinitions](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/DeploymentBundleDefinitions)

* **[OptionSettingItem](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/Recipes/OptionSettingItem.cs)** - This is a POCO class that serves as the deserialized model for option settings found inside [deployment recipes](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L467). These settings can be tweaked by the user to customize their deployment. This is defined as a partial class and the rest of its functionality is specified inside [OptionSettingItem.ValueOverride](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/Recipes/OptionSettingItem.ValueOverride.cs)

* **[ValidatorFactory](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Common/Recipes/Validation/ValidatorFactory.cs)** - The deployment tool performs input validation to guard against invalid input. These validators are specified inside the recipe files and can either be a [recipe-level](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L369) validator or an [optionSetting-level](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L626) validator. The ValidatorFactory is responsible for invoking the correct validator depending on the option setting being configured.

* **[Exceptions](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/Exceptions.cs)** - Declares all exceptions that are thrown by the AWS.Deploy.Common component. It also specifies the error codes that are associated with **all** exceptions thrown by the deployment tool (**Note** - The error codes are only bundled with exceptions that implement [`DeployToolException`](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Common/Exceptions.cs#L9) abstract class. These exceptions are caused by a user error and can be remedied by the user based on the error message thrown.)

* **[AWS.Deploy.CLI.Common.UnitTests](https://github.com/aws/aws-dotnet-deploy/tree/main/test/AWS.Deploy.CLI.Common.UnitTests)** - Contains unit tests for this component.


### [AWS.Deploy.DockerEngine](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.DockerEngine)

This component contains all the logic for dealing with Docker. Some of its responsibilities are outlined below:

* [Generating a Dockerfile](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.DockerEngine/DockerEngine.cs#L57) if the user’s project does not contain one. **Note** - a Dockerfile is only generated when deploying via a container-based recommendation such as [ASP.NETAppAppRunner.recipe](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes/RecipeDefinitions/ASP.NETAppAppRunner.recipe) or [ASP.NETAppECSFargate.recipe](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes/RecipeDefinitions/ASP.NETAppECSFargate.recipe)

* [Determining the Docker execution directory](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.DockerEngine/DockerEngine.cs#L175). It serves as the working directory for the `docker build` command and all relative paths in the Dockerfile are resolved from this directory.


### [AWS.Deploy.Orchestration](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Orchestration)

This component serves as the main workhorse for the deployment tooling and it contains the most number of logical pieces.

**Key Components**

* **[Orchestrator](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/Orchestrator.cs)** - It holds various dependencies required by the deployment tool and its design is aligned with the [composition over inheritance](https://en.wikipedia.org/wiki/Composition_over_inheritance) philosophy.

* **[OrchestratorSession](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/OrchestratorSession.cs)** - This is a POCO class that holds the user’s AWS credentials, the deployment region, AWS account ID and metadata about the user’s .NET project.

* **[DeploymentCommandFactory](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/DeploymentCommands/DeploymentCommandFactory.cs)** - The deployment tool supports different deployment types (CDK based, existing Beanstalk environment, Pushing to ECR). Each type has its own deployment command and this mapping is stored inside the DeploymentCommandFactory.

* **[IOrchestratorInteractiveService](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/IOrchestratorInteractiveService.cs)** - This interface defines the logging service for the deployment tool.

* **[SystemCapabilityEvaluator](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/SystemCapabilityEvaluator.cs)** - Verifies that pre-requisties such as Docker and Node.js (required for CDK) are installed on the user’s system

* **[RecommendationEngine](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/RecommendationEngine/RecommendationEngine.cs)** - This is responsible for parsing the recipe files into a [Recommendation](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Common/Recommendation.cs) object. It also computes the priority by running the [recommendation rules](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L173) against the customer’s .NET project

* **[RecipeHandler](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/RecipeHandler.cs)** - It contains methods to parse recipe files, locate custom (user defined) recipes and run recipe level validators

* **[OptionSettingHandler](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/OptionSettingHandler.cs)** - It contains methods to interact with option settings. Some of its functionality includes modifying the value, retrieving the current value and running option setting validators.

* **[DeploymentSettingsHandler](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/DeploymentSettingsHandler.cs)** - Users can create their own deployment settings file that specifies a list of option settings and their values (See [here](https://github.com/aws/aws-dotnet-deploy/blob/main/test/AWS.Deploy.CLI.Common.UnitTests/ConfigFileDeployment/TestFiles/ECSFargateConfigFile.json) for an example). The user can invoke `dotnet aws deploy --apply <SETTINGS-FILE-PATH> --silent` which applies the option settings values, runs all validators and kicks off a deployment without any user prompts. DeploymentSettingsHandler is responsible for parsing the settings file and applying the option setting values.

* **[AWSResourceQueryer](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/Data/AWSResourceQueryer.cs)** - Contains methods to query resources from different AWS services.

* **[DeploymentBundleHandler](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/DeploymentBundleHandler.cs)** - All recipes have a [DeploymentBundle](https://github.com/aws/aws-dotnet-deploy/blob/66fb1e34bbea10bbd995596e1eec801b79ff9102/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json#L59) property that specifies how to package the user’s .NET project for deployment. DeploymentBundleHandler is reponsible for creating the appropriate deployment bundle

* **[DeployedApplicationQueryer](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/Utilities/DeployedApplicationQueryer.cs)** - It contains the functionality to retrieve the list of previously deployed applications and can also determine if an existing cloud application can be redeployed using the current set of recommendations.

* **[CdkProjectHandler](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/CdkProjectHandler.cs)** - Contains all the logic to interact with CDK.

* **[CdkAppSettingsSerializer](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/CdkAppSettingsSerializer.cs)** - It writes all the deployment settings into the `appsettings.json` file. This file is then deserialized into [IRecipeProps](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes.CDK.Common/RecipeProps.cs) and passed to the [CDK templates.](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/CdkTemplates)

* **[Exceptions.cs](https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Orchestration/Exceptions.cs)** - Declares all exceptions that are thrown by the AWS.Deploy.Orchestration component

* **[AWS.Deploy.Orchestration.UnitTests](https://github.com/aws/aws-dotnet-deploy/tree/main/test/AWS.Deploy.Orchestration.UnitTests)** - Contains unit tests for this component


### [AWS.Deploy.Constants](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Constants)

This is a shared project and contains various constants used throughout the codebase


### [AWS.Deploy.CLI.IntegrationTests](https://github.com/aws/aws-dotnet-deploy/tree/main/test/AWS.Deploy.CLI.IntegrationTests)

This contains the integration test suite


### Server Mode

To support IDEs using the AWS .NET Deploy tool the CLI can be launched in server mode. This will run the CLI as a server that exposes an API IDEs will be able to interact with to perform deployment activities. When the server mode is started the deploy tool will act as an ASP.NET Core application. Server mode will keep all of the deployment logic and rules inside the deploy tool CLI. IDEs are treated as frontends rendering the information that comes from the deploy tool and passing any of the information the user has specified into the deploy tool to validate and persist.

**Debugging the .NET Deployment Tool via Server Mode**

1. [Install](https://aws.amazon.com/visualstudio/) the AWS Toolkit for Visual Studio
2. Open the following file in a text editor - `%localappdata%\AWSToolkit\PublishSettings.json`
3. Under the `DeployServer` parent, add a new property `AlternateCliPath` that points to the `AWS.Deploy.CLI` executable. This instruct the Toolkit to launch the deploy tool from a custom location.

**Example**
```json
{
   "DeployServer":{
      "AlternateCliPath":"C:\\code\\aws-dotnet-deploy\\src\\AWS.Deploy.CLI\\bin\\Release\\net6.0\\AWS.Deploy.CLI.exe",
      "PortRange":{
         "Start":10000,
         "End":10100
      }
   }
}
```

Follow these steps to attach a debugger:

1. Launch a new Visual Studio window, and begin deploying a test project to AWS.
2. In another Visual Studio window with Deploy Tool solution open, select *Debug > Attach to Process* then choose the `AWS.Deploy.CLI.exe` process that was started by the previous step.


**Key Components**

* [AWS.Deploy.CLI.ServerMode](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.CLI/ServerMode) - It contains various Swagger based API controllers and POCO models that govern the REST API behaviour

* [AWS.Deploy.ServerMode.ClientGenerator](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.ServerMode.ClientGenerator) - This ingests the API specification file (`swagger.json`) to generate the REST API client.

* [AWS.Deploy.ServerMode.Client](https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.ServerMode.Client) - This is the actual REST API client used by the frontend (VS Toolkit) to interact with the deployment tool CLI.

## Build and Test Documentation

### Install Material for MkDocs
Material for MkDocs is a theme for MkDocs, a static site generator geared towards (technical) project documentation. If you're familiar with Python, you can install Material for MkDocs with pip, the Python package manager.

```
pip install mkdocs-material
```

For, other installation options [see here](https://squidfunk.github.io/mkdocs-material/getting-started/)

### Deploying to a Local Server
MkDocs comes with a built-in dev-server that lets you preview your documentation as you work on it.

From the root of the project repository, run the following command:
```
mkdocs serve
```

Paste the link to the local server on a web browser to look at the documentation.

The dev-server also supports auto-reloading, and will rebuild your documentation whenever anything in the configuration file, documentation directory, or theme directory changes.