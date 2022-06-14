# Suppressing prompts with —-silent

You can use AWS Deploy Tool when developing your app using any Continuous Deployment system. Continuous Deployment systems let you automatically build, test and deploy your application each time you check in updates to your source code. Before you can use AWS Deploy Tool in your CD pipeline, you must have [required pre-requisites](../../docs/getting-started/pre-requisites.md) installed and configured in the CD environment.

To turn off the interactive features of the tooling, use the `-s (--silent)` switch. This will ensure the tooling never prompts for any questions which could block an automated process.

To supply the settings to be used for deployment, use the `--apply` switch to specify the path to a JSON file that contains settings. Storing deployment settings in a JSON file also allows those settings to be version controlled.

You can run the deployment tool with the following command to deploy the application with above directory structure in CI/CD pipeline without any prompts.

    dotnet aws deploy --project-path MyWebApplication/MyWebApplication/MyWebApplication.csproj --apply deploymentsettings.json
