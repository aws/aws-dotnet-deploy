###Deploy with —silent

To turn off the interactive features of the tooling, use the -s (--silent) switch. This will ensure the tooling never prompts for any questions which could block an automated process.

To supply the settings to be used for deployment,  use the  --apply switch to specify the path to a JSON file that contains settings. Storing the settings in a JSON file also allows those settings to be version controlled.

This section defines the JSON definitions and syntax that you use to construct a configuration file. Here is the JSON file definition (https://github.com/aws/aws-dotnet-deploy/tree/main/src/AWS.Deploy.Recipes/RecipeDefinitions).
