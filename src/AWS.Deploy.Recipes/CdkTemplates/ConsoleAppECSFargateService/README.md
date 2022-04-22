# AWS deploy tool deployment project

This .NET project is a deployment project used by the AWS deploy tool to deploy .NET applications to AWS. The project is made
up of 2 parts.

First is a *.recipe file which defines all of the settings for deployment project. The recipe file is what
the AWS.Deploy.Tools and the AWS Toolkit for Visual Studio use to drive the user experience to deploy a .NET application
with this deployment project.

The second part of the deployment project is a .NET AWS CDK project which defines the AWS infrastructure that the
.NET application will be deployed to.

## What is CDK?
 
The AWS Cloud Development Kit (CDK) is an open source software development framework to define your cloud application
resources using familiar programming languages like C#. In CDK projects, constructs are instantiated for each of the
AWS resources required. CDK projects are used to generate an AWS CloudFormation template to be used by the
AWS CloudFormation service to create a Stack of all of the resources defined in a template.

Visit the following link for more information on the AWS CDK:
https://aws.amazon.com/cdk/

## Should I use the CDK CLI?

In a regular CDK project the CDK CLI, acquired from NPM, would be used to execute the CDK project. Because AWS deploy
tool deployment projects are made of both a recipe and a CDK project you should not use the CDK CLI directly on
the deployment project.

The AWS deploy tool from either AWS.Deploy.Tools package or AWS Toolkit for Visual Studio
should be used to drive the experience. The AWS deploy tool will take care of acquiring the CDK CLI and executing the
CDK CLI passing in all of the settings gathered in the AWS deploy tool.

## Can I modify the deployment project?

When a deployment projects is saved the project can be customized by adding more CDK constructs or customizing the existing
CDK constructs.

The default folder structure puts the CDK constructs originally defined by the deployment recipe into a folder called
"Generated". It is recommended to not directly modify these files and instead customize the settings via the
AppStack.CustomizeCDKProps() method. This allows the AWS deploy tool to easily updated the generated code
as new features are added to the original recipe the deployment project was created from. Checkout the AppStack.cs
file for information on how to customize the CDK project.

## Can I add more settings to the recipe?

As you customize the deployment project you might want to present the user's of the deployment project more
settings that will be displayed in the AWS.Deploy.Tools package or AWS Toolkit for Visual Studio. The recipe
file in the deployment project can be modified to add new settings. Below is the link to the JSON schema for the
recipe.

https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.Recipes/RecipeDefinitions/aws-deploy-recipe-schema.json

For any new settings added to the recipe you will need to add corresponding fields in the Configuration class using the
setting ids as the property names.

