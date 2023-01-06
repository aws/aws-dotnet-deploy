# Creating a deployment settings file

### What is a deployment settings file

A deployment settings file allows you to define the deployment settings of your application in a _JSON_ format. This JSON file can be version controlled and plugged into your CI/CD system for future deployments. 

Deployment settings files could be used as a way to automate your deployments or use them in a [CI/CD setting](./cicd.md) where you would define all the settings that you need to apply for your deployment and then use the `--apply` flag on the CLI to link to the deployment setting file. 

By doing this, the AWS .NET deployment tool reads all the settings you have defined and applies them to the deployment. You will need to do a final confirmation to initiate the deployment in the CLI. However, you can bypass the confirmation by making the deployment a silent one. This can be done by adding the `--silent` flag which will turn off any prompts.

The deployment settings file has the following structure:

```json
{
      "AWSProfile": <AWS_PROFILE>,
      "AWSRegion": <AWS_REGION>,
      "ApplicationName": <APPLICATION_NAME>,
      "RecipeId": <RECIPE_ID>,
      "Settings": <JSON_BLOB>
}
```

* _**AWSProfile**_: The name of the AWS profile that will be used during deployment.

* _**AWSRegion**_: The name of the AWS region where the deployed application is hosted.

* _**ApplicationName**_: The name that is used to identify your cloud application within AWS. If the application is deployed via AWS CDK, then this name points to the CloudFormation stack.

* _**RecipeId**_: The recipe identifier that will be used to deploy your application to AWS.

* _**Settings**_: This is a JSON blob that stores the values of all available settings that can be tweaked to adjust the deployment configuration. This is represented as a _JSON_ object that contains the deployment setting IDs and values as a key/pair configuration. 

*Note:*  _**AWSProfile, AWSRegion and ApplicationName**_ are optional and can be overriden by using the appropriate command line switches. This enables users to craft a *deployment settings file* that could be used for multiple AWS profiles and regions.

An example of overriding _**AWSProfile, AWSRegion and ApplicationName**_ in the CLI:
```
dotnet aws deploy --application-name WebApp1 --profile default --region us-west-2
```

Each recipe has its own set of settings that can be configured. The following pages in this section list the settings for each recipe that can be used to fill in the `Settings` section of the file.

### Example of a deployment settings file

An example of what a deployment settings file would look like:
```json
{
      "AWSProfile": "default",
      "AWSRegion": "us-west-2",
      "ApplicationName": "WebApp1",
      "RecipeId": "AspNetAppEcsFargate",
      "Settings": {
        "ECSCluster": {
            "CreateNew": true,
            "NewClusterName": "WebApp1-Cluster"
        },
        "ECSServiceName": "WebApp1-service",
        "DesiredCount": 3,
        "ApplicationIAMRole": {
            "CreateNew": true
        }
      }
}
```
Settings that contain child settings under them are represented as another _JSON_ object similar to the example provided above.

### How to create a deployment settings file

1. Create a new `JSON` file.
2. Add the 3 properties _**AWSProfile, AWSRegion and ApplicationName**_. These are generic and not tied to a specific *Recipe* file.
> _**AWSProfile**_: The name of the AWS profile that will be used during deployment.
> _**AWSRegion**_: The name of the AWS region where the deployed application is hosted.
> _**ApplicationName**_: The name that is used to identify your cloud application within AWS. If the application is deployed via AWS CDK, then this name points to the CloudFormation stack.
3. Pick a *Recipe* from the **Deployment Recipes** section in the navigation to use for your deployment. A Recipe defines the .NET application type and the AWS compute to deploy it to. For example [ASP.NET Core App to Amazon ECS using AWS Fargate](./recipes/ASP.NET%20Core%20App%20to%20Amazon%20ECS%20using%20AWS%20Fargate.md).
4. Add a _**Settings**_ object to define deployment settings.
5. To set the `ECS Service Name`, get the ID from the *Recipe* which is `ECSServiceName`. The value needs to be the same type as the `Type` of setting. `ECS Service Name` has a type `String`, so give it a value `WebApp1-service`.
6. To set `ECS Cluster`, the ID is `ECSCluster` and the setting has a type `Object`. The value of `Object` types is another JSON blob representing the setting ID and value of the `Object's children settings`. To set the 2 children settings `Create New ECS Cluster` and `New Cluster Name`, use the IDs `CreateNew` and `NewClusterName` respectively. `Create New ECS Cluster` has a type `Bool` so we can set `true` or `false`, and `New Cluster Name` is a `String`. The deployment settings file will look like this:
```json
{
      "AWSProfile": "default",
      "AWSRegion": "us-west-2",
      "ApplicationName": "WebApp1",
      "RecipeId": "AspNetAppEcsFargate",
      "Settings": {
        "ECSCluster": {
            "CreateNew": true,
            "NewClusterName": "WebApp1-Cluster"
        },
        "ECSServiceName": "WebApp1-service"
      }
}
```

Keep adding more settings by using the *Recipe* you selected as reference and be mindful of the type of setting you are setting.