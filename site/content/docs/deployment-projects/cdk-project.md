# CDK Project

A custom deployment project uses a C# Cloud Development Kit (CDK) project to create the AWS infrastructure as a CloudFormation stack based on the settings provided by the user and to deploy your project to that infrastructure.


### CDK Refresher
The following links are useful resources to learn how to write .NET CDK projects.

* [.NET CDK Workshop](https://cdkworkshop.com/40-dotnet.html)
* [.NET CDK Reference](https://docs.aws.amazon.com/cdk/api/v2/dotnet/api/index.html)
* [AWS CDK Developer Guide](https://docs.aws.amazon.com/cdk/v2/guide/home.html)

The main concepts to understand about the CDK are the following.

* Stack - this is the top level container for all of the AWS resources represented as constructs. In deployment projects this type is usually the `AppStack` type in the project.
* Constructs - AWS resources are modeled as constructs. Some constructs are a one-to-one mapping to a single AWS resource. The CDK also has higher level abstractions where a single construct represents a collection of AWS resources that solve a common problem.
* Construct properties - For every construct there is a properties object that is created first, all necessary values are set and then passed into the constructor of a construct.


### Main method in Program.cs

The `Main` method in `Program.cs` for the CDK deployment project must be coded in a certain style to ensure compatibility with the deploy tool. The deploy tool relies on .NET's Configuration system to pass along settings from the deploy tool to the CDK project. In the example below the `ConfigurationBuilder().AddAWSDeployToolConfiguration(app)` method reads the settings that were passed into the project from the deploy tool.

With the configuration read from the deploy tool, the CDK environment is set to the account and region the deploy tool was configured with.

The other major difference from normal CDK projects is the call to `CDKRecipeSetup.RegisterStack`. This is required to stamp the CloudFormation stack with the recipe id that created the stack. Future redeployments can only update existing stacks that were created by the original recipe. It also serializes the settings collected from the deploy tool into the metadata for the CloudFormation stack so redeployments can use the previous settings used for deployment.

```
public static void Main(string[] args)
{
    var app = new App();

    var builder = new ConfigurationBuilder().AddAWSDeployToolConfiguration(app);
    var recipeProps = builder.Build().Get<RecipeProps<Configuration>>();
    var appStackProps = new DeployToolStackProps<Configuration>(recipeProps)
    {
        Env = new Environment
        {
            Account = recipeProps.AWSAccountId,
            Region = recipeProps.AWSRegion
        }
    };

    // The RegisterStack method is used to set identifying information on the stack
    // for the recipe used to deploy the application and preserve the settings used in the recipe
    // to allow redeployment. The information is stored as CloudFormation tags and metadata inside
    // the generated CloudFormation template.
    CDKRecipeSetup.RegisterStack<Configuration>(new AppStack(app, appStackProps), appStackProps.RecipeProps);

    app.Synth();
}
```

### Layout of a CDK deployment project

The layout of the generated CDK project puts all the code that was used to create the AWS resources defined in the starting recipe in a directory called **Generated**. 

![Catagories in AWS Toolkit for Visual Studio](../../assets/images/deployment-project-file-layout.png)

It is recommended to not modify the code in the `Generated` directory to make it easier to merge future changes from the starting recipe into your custom deployment project. If you do not intend to update your custom deployment project from the original built-in recipe you may modify the code or rearrange the directory layout.

If you choose to not modify the `Generated` code it is recommended to customize the CDK project starting from the `AppStack` class. Here is the constructor of `AppStack`.

```
internal AppStack(Construct scope, IDeployToolStackProps<Configuration> props)
    : base(scope, props.StackName, props)
{
    _configuration = props.RecipeProps.Settings;

    // Setup callback for generated construct to provide access to customize CDK properties before creating constructs.
    CDKRecipeCustomizer<Recipe>.CustomizeCDKProps += CustomizeCDKProps;

    // Create custom CDK constructs here that might need to be referenced in the CustomizeCDKProps. For example if
    // creating a DynamoDB table construct and then later using the CDK construct reference in CustomizeCDKProps to
    // pass the table name as an environment variable to the container image.

    // Create the recipe defined CDK construct with all of its sub constructs.
    var generatedRecipe = new Recipe(this, props.RecipeProps);

    // Create additional CDK constructs here. The recipe's constructs can be accessed as properties on
    // the generatedRecipe variable.
}
```        

The `var generatedRecipe = new Recipe(this, props.RecipeProps);` line of code creates all of the AWS resources from the `Generated` directory. Your customizations could create new AWS resources via CDK constructs before or after this line. Typically you would create new resources before this line if you want those resources to be connected to the resources defined in the `Recipe` type. If you need to create new resources that are connected to the resources defined in the `Recipe` then create them after this line. The instance of `Recipe` has public properties for all of the resources that were created in the `Recipe`.

In this constructor a callback method called `CustomizeCDKProps` is setup. This callback method is called right before any constructs are created from the `Recipe`. This allows modifying the construct's property object before it is passed into the construct.

The example below shows the `CustomizeCDKProps` callback that checks to see if the resource being created is the Beanstalk Environment. If it is, cast the property object to the appropriate property object and then make whatever customizations are needed.

```
private void CustomizeCDKProps(CustomizePropsEventArgs<Recipe> evnt)
{
    if (string.Equals(evnt.ResourceLogicalName, nameof(evnt.Construct.BeanstalkEnvironment)))
    {
        if (evnt.Props is CfnEnvironmentProps props)
        {
            Console.WriteLine("Customizing Beanstalk Environment");
        }
    }
}
```

### Configuration

The settings collected in the deploy tool are passed into the CDK project and then deserialized into the `Configuration` object. If you add new settings to the recipe file you will need to add the Id of the new settings to the `Configuration` object. If you added an `Object` setting with a collection of child settings then you need to create a new type with the child setting ids as properties on the new type. Then add a new property on the `Configuration` type for the new type with the property name being the id of the `Object` setting.

The `Configuration` object follows the same `Generated` directory pattern described above. Custom settings should be added to the partial `Configuration.cs` file outside of the `Generated` directory.