# Customizing deployment projects

First, generate the deployment project in the directory of your choice:

    > dotnet aws deployment-project generate --output ../myCustomCDKProject --project-display-name MyCustomCDKProject

In the command above, the `--project-display-name` sets the name for the project that will be seen to you during the deployment.

Now you can go ahead and add additional resources or your custom requirements to the generated deployment project. In the example below, we’ll show you how to add a DynamoDB table....
<TODO add example here for DynamoDB>

   > **Note: It is important to save the generated deployment project in version control because it is required for re-deployments.**

#### Specifying external deployment project

Instead of choosing one of the recipes supplied by AWS, you can choose your custom deployment project.

<TODO>

