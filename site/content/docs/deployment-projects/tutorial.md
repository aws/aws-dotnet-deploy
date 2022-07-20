# Custom deployment project tutorial

For this tutorial we going to create a custom deployment project to deploy our Acme.WebApp project. This web application uses an AWS DynamoDB table as the backend store. We want to create a custom deployment project that the team can use to deploy the application to Amazon Elastic Container Service (ECS) along with the application's DynamoDB table. 

***Note: For this tutorial we are not concerned with the logic of the application being deployed. To follow along with this tutorial you can replace Acme.WebApp with any "hello world" web application.***


Tasks we will accomplish:

* Create a custom deployment project
* Modify the custom deployment project's recipe file to allow the user to configure what DynamoDB table to use.
* Customize the deployment project's CDK project to create the DynamoDB table and set an environment variable that our application can read to know which table to use.

### Creating the custom deployment project

To create the custom deployment project, navigate to the Acme.WebApp project directory and run the following command.

    dotnet aws deployment-project generate --output ../Acme.WebApp.DeploymentProject --project-display-name "ASP.NET Core app with DynamoDB" 

The AWS Deploy Tool will analyze the Acme.WebApp project and display which built-in recipes can be used as the starting point of the custom deployment project. Since we want to deploy to ECS, pick the option that says "ASP.NET Core App to Amazon ECS using AWS Fargate".

Once the starting recipe is picked, `Acme.WebApp.DeploymentProject` is created in a sibling project to the application project. If you are using Visual Studio you might want to add the new `Acme.WebApp.DeploymentProject` project to your solution.

The `--project-display-name` switch above configures the name of the recommendation that is shown in the deploy tool when deploying the application project.


### Adding DynamoDB settings to the recipe

We want to give our team members who will use the custom deployment project the choice to either select an existing DynamoDB table or create a new table during deployment. To do that we need to add a few new settings to our deployment project's recipe definition.

In the directory containing the deployment project open the `Acme.WebApp.DeploymentProject.recipe` file in your JSON editor of choice. Find the `OptionSettings` section that contains the settings users can use to configure their project.

To get started we are going to create a new "Object" setting called `Backend` to group all of our new settings. The snippet below shows the object-level setting. The options we are about to create will be displayed to the users in the "General" category when configuring their deployment.

```
  "OptionSettings": [

    ...

    {
      "Id": "Backend",
      "Name": "Backend",
      "Category": "General",
      "Description": "Configure the backend store.",
      "Type": "Object",
      "AdvancedSetting": false,
      "Updatable": true,
      "ChildOptionSettings": [

      ]
    },


    ...
  }
```

Now we are going to create our child settings. The first is a setting to determine if we should create a new table or not. This setting is a `Bool` type which is defaulted to `true`. As a best practice the `Updatable` setting is set to `false` to protect users from accidentally deleting the table when redeploying in the future.

```
      "ChildOptionSettings": [
        {
          "Id": "CreateNewTable",
          "Name": "Create New DynamoDB Table",
          "Description": "Do you want to create a new DynamoDB table for the backend store?",
          "Type": "Bool",
          "DefaultValue": true,
          "AdvancedSetting": false,
          "Updatable": false
        },

        ...

       ]

```

If the user unchecks the `CreateNewTable` setting we need to give them the choice to select an existing table. This `ExistingTableName` setting is a "String" type that will store the name of an existing DynamoDB table to use as the backend store.

```
      "ChildOptionSettings": [

        ...

        {
          "Id": "ExistingTableName",
          "Name": "Existing DynamoDB Table",
          "Description": "Existing DynamoDB table to use as the backend store.",
          "Type": "String",
          "TypeHint": "DynamoDBTableName",
          "DefaultValue": "",
          "AdvancedSetting": false,
          "Updatable": true,
          "DependsOn": [
            {
              "Id": "Backend.CreateNewTable",
              "Value": false
            }
          ],
          "Validators": [
            {
              "ValidatorType": "Regex",
              "Configuration": {
                "Regex": "[a-zA-Z0-9_.-]+",
                "ValidationFailedMessage": "Invalid table name."
              }
            },
            {
              "ValidatorType": "StringLength",
              "Configuration": {
                "MinLength": 3,
                "MaxLength": 255
              }
            }
          ]
        }
      ]
```

Let us take a deeper dive into the properties for the `ExistingTableName` setting.

* **TypeHint** - Set to `DynamoDBTableName` which lets the deployment tool know this String type is for the name of a DynamoDB table. The deploy tool uses this information to show users a list of tables to pick from instead of a text-box.
* **Updatable** - Since modifying the name of an existing table is not a destructive change, we will allow this field to be updated during redeployments.
* **DependsOn** - This setting will only be visible if the previous `CreateNewTable` setting is set to `false`. Notice how the `Id` is the full name of the setting including the parent "Object" setting `Backend`.
* **Validators** - This attaches validators to make sure that the user-provided name matches the regex for valid table names and that the name meets the required minimum and maximum lengths. Adding validators provides feedback to users when invalid values are provided in either the CLI or Visual Studio.

Here is the full snippet of the `Backend` Object setting with the child settings.
```
    {
      "Id": "Backend",
      "Name": "Backend",
      "Category": "General",
      "Description": "Configure the backend store.",
      "Type": "Object",
      "AdvancedSetting": false,
      "Updatable": true,
      "ChildOptionSettings": [
        {
          "Id": "CreateNewTable",
          "Name": "Create New DynamoDB Table",
          "Description": "Do you want to create a new DynamoDB table for the backend store?",
          "Type": "Bool",
          "DefaultValue": true,
          "AdvancedSetting": false,
          "Updatable": false
        },
        {
          "Id": "ExistingTableName",
          "Name": "Existing DynamoDB Table",
          "Description": "Existing DynamoDB table to use as the backend store.",
          "Type": "String",
          "TypeHint": "DynamoDBTableName",
          "DefaultValue": "",
          "AdvancedSetting": false,
          "Updatable": true,
          "DependsOn": [
            {
              "Id": "Backend.CreateNewTable",
              "Value": false
            }
          ],
          "Validators": [
            {
              "ValidatorType": "Regex",
              "Configuration": {
                "Regex": "[a-zA-Z0-9_.-]+",
                "ValidationFailedMessage": "Invalid table name."
              }
            },
            {
              "ValidatorType": "StringLength",
              "Configuration": {
                "MinLength": 3,
                "MaxLength": 255
              }
            }
          ]
        }
      ]
    }
```

### Customizing the deployment project's CDK project

Now that users can customize the backend settings when deploying, the CDK project for the custom deployment project needs to be updated to react to the new settings.

***Note: The .NET CDK projects generated by the deploy tool have the C# feature `Nullable` enabled in the project file. If you do not want this feature enabled edit the csproj file and remove the `Nullable` project from the PropertyGroup.***

#### Deserializing settings

When AWS Deploy Tool executes the CDK project it passes all of the settings collected from the user and deserializes them into the `Configuration` type in the CDK project. We need to modify the `Configuration` type to store the new backend settings. 

Create a new class called `BackendConfiguration` in the `Configurations` directory. Below is the code for this new type with the properties for `CreateNewTable` and `ExistingTableName`.

```
namespace Acme.WebApp.DeploymentProject.Configurations
{
    public class BackendConfiguration
    {
        public bool CreateNewTable { get; set; }

        public string ExistingTableName { get; set; }


        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public BackendConfiguration()
        {

        }
#nullable restore warnings

        public BackendConfiguration(
            bool createNewTable,
            string existingTableName)
        {
            CreateNewTable = createNewTable;
            ExistingTableName = existingTableName;

        }
    }
}
```

In the `Configuration.cs` file add a new property for our new backend settings.

```
namespace Acme.WebApp.DeploymentProject.Configurations
{
    public partial class Configuration
    {
        public BackendConfiguration Backend { get; set; } = new BackendConfiguration();
    }
}
```

Notice that the `Backend` property was added to the partial class that is **not** in the `Generated` directory. In both the `Configuration` and `BackendConfiguration` types the property names match the setting ids used in the recipe file. This is important for the data to be property deserialized.

#### CDK Changes

The `AppStack` class is the recommended place to customize the AWS resources created by the CDK. We will modify the constructor of this class to check if `CreateNewTable` is set to true. If it is we will use the CDK construct to create a table as part of the CloudFormation stack.

```
using Amazon.CDK.AWS.DynamoDB;

namespace Acme.WebApp.DeploymentProject
{

    public class AppStack : Stack
    {
        private readonly Configuration _configuration;
        private Table? _ddbBackend;

        internal AppStack(Construct scope, IDeployToolStackProps<Configuration> props)
            : base(scope, props.StackName, props)
        {
            _configuration = props.RecipeProps.Settings;

            // Setup callback for generated construct to provide access to customize CDK properties before creating constructs.
            CDKRecipeCustomizer<Recipe>.CustomizeCDKProps += CustomizeCDKProps;

            if(_configuration.Backend.CreateNewTable == true)
            {
                var backendProps = new TableProps
                {
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute { Name = "Id", Type = AttributeType.STRING },
                    BillingMode = BillingMode.PAY_PER_REQUEST,
                };
                _ddbBackend = new Table(this, "Backend", backendProps);
            }

            var generatedRecipe = new Recipe(this, props.RecipeProps);
        }
```

In the snippet above the table is created before `Recipe` construct. The `Recipe` construct has all of the AWS resources that are part of the original, built-in ECS recipe that the custom deployment project was created from. 

Now that we have our table we need to pass the table name into our application code. We are going to do this by setting an environment variable that the application code will read.

The `CustomizeCDKProps` method in `AppStack` is a callback method that gets called for each AWS resource about to be created from the `Recipe` construct. Here is where we can set the environment variable. 

To know which AWS resource is about to be created, compare the `evnt.ResourceLogicalName` property to the public property name on the `Recipe` construct. The built-in recipes are written to make sure the resource logical name is the same as the public property name. In our scenario we are looking to see if the `AppContainerDefinition` is about to be created. 

When we determine that the callback is for `AppContainerDefinition` then we cast the `evnt.Props` to the corresponding property object for `AppContainerDefinition`, in this case `ContainerDefinitionOptions`. From `ContainerDefinitionOptions` we can set the table name in an environment variable.

```
private void CustomizeCDKProps(CustomizePropsEventArgs<Recipe> evnt)
{
    // Example of how to customize the container image definition to include environment variables to the running applications.
    // 
    if (string.Equals(evnt.ResourceLogicalName, nameof(evnt.Construct.AppContainerDefinition)))
    {
        if (evnt.Props is ContainerDefinitionOptions props)
        {
            if (props.Environment == null)
                props.Environment = new Dictionary<string, string>();


            if(_ddbBackend != null)
            {
                props.Environment["BACKEND_TABLE"] = _ddbBackend.TableName;
            }
            else
            {
                props.Environment["BACKEND_TABLE"] = _configuration.Backend.ExistingTableName;
            }
        }
    }
}
```

### Using the deployment project

Custom deployment projects are used through the same deployment workflow as the built-in recipes. For the CLI deploy tool, execute the `dotnet aws deploy` command in the application project directory. The custom deployment project will be displayed as the recommended option.

```
Recommended Deployment Option
-----------------------------
1: ASP.NET Core app with DynamoDB
This ASP.NET Core application will be deployed to Amazon Elastic Container Service (Amazon ECS) with compute power managed by AWS Fargate compute engine. If your project does not contain a Dockerfile, it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy your application as a container image on Linux.

Additional Deployment Options
------------------------------
2: ASP.NET Core App to Amazon ECS using AWS Fargate
This ASP.NET Core application will be deployed to Amazon Elastic Container Service (Amazon ECS) with compute power managed by AWS Fargate compute engine. If your project does not contain a Dockerfile, it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy your application as a container image on Linux.

...

```

The settings for the recommendation shows the Backend settings we customized. If we navigate to the backend settings the deploy tool will allow the user to choose between using a new table or picking an existing table.

```
...

Current settings (select number to change its value)
----------------------------------------------------
1. ECS Cluster: AcmeWebApp
2. ECS Service Name: AcmeWebApp-service
3. Backend:
        Create New DynamoDB Table: True
4. Desired Task Count: 3
5. Application IAM Role: *** Create new ***
6. Virtual Private Cloud (VPC): *** Default ***
7. Environment Variables:
8. ECR Repository Name: acmewebapp

...

```

The AWS Toolkit for Visual Studio will also recognize the custom deployment project. The deployment project will show up as the highest recommended option and the user will also be able to choose between creating a new table or choosing from a drop-down list of available tables in the account that is being deployed to.

The custom deployment project should be checked in to your source control. The deployment project is required for redeployments to existing CloudFormation stacks that were created from the custom deployment project. 