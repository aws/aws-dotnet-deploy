# Other Issues
This section of the troubleshooting guide explains how to determine, diagnose, and fix common issues you might encounter during the deployment process.

## Invalid project path provided

**Why is this happening**: The CLI deployment command takes in an optional `--project-path` option.

For example:
```
dotnet aws deploy --project-path <PATH>
```
The deployment process would fail if an invalid `--project-path` is provided.

**Resolution**: The `--project-path` can be absolute or relative to the current working directory and must be one of the following:

 - A file path pointing to a `*.csproj` or `*.fsproj` file.
 - A directory path that contains a `*.csproj` or `*.fsproj` file.

If a `--project-path` option is not provided, then AWS.Deploy.Tools will look for a `*.csproj` or `*.fsproj` file in the current working directory.

## Failed to find compatible deployment recommendations

**Why is this happening**: Behind the scenes, AWS.Deploy.Tools uses a recipe configuration file to provide an opinionated deployment experience. See [here](../../docs/features/recipe/) to learn more about recipes.

Recipe configurations target different AWS services and there may be incompatibilities between the chosen recipe and your .NET application.

Another reason why there are no recommendations generated is if your application's `.csproj` file is using a variable for the `TargetFramework` property.
For example:


    <Project Sdk="Microsoft.NET.Sdk.Web">
	    <PropertyGroup>
		    <TargetFrameworkVersion>net5.0</TargetFrameworkVersion>
		    <TargetFramework>$(TargetFrameworkVersion)</TargetFramework>
	    </PropertyGroup>
	</Project>
    
No recommendations will be generated for the above `.csproj` file.
**This is a bug which we will address**: [GitHub issue](https://github.com/aws/aws-dotnet-deploy/issues/550). 

Meanwhile, please provide explicit values for the `TargetFramework` property. 

**Resolution**: If you think that your project is not correctly recognized by our tool and no recommendations are generated, then file a [GitHub issue](https://github.com/aws/aws-dotnet-deploy/issues/new/choose) describing your project and also providing relevant details about your `.csproj` or `.fsproj` file. This will help us understand and narrow down the gaps in our recommendation engine and customer use cases.

## Deployment failures related to JSON configuration file

**Why is this happening**: AWS.Deploy.Tools allows for prompt-less deployments using a JSON configuration file. This workflow can easily be plugged into your CI/CD pipeline for automated deployments. 

If a deployment failure occurs while using a configuration file, It is possible that the configuration file has the wrong definition or the wrong format.

**Resolution**: Kindly ensure that the JSON configuration file has the correct JSON syntax. See [here](../../docs/features/config-file/) for an example of a valid JSON configuration file.

## Insufficient IAM Permissions

**Why is this happening**: Access to AWS is governed by IAM policies. They are a group of permissions which determine whether the request to an AWS resource/service is allowed or denied.

AWS.Deploy.Tools, internally uses a variety of different services to host your .NET application on AWS. If you encounter an error saying `user is not authorized to perform action because no identity based policies allow it`, that means you need to add the corresponding permission to the IAM policy that is used by the current IAM role/user.

**Note: Exact wording for an insufficient permissions related errors may differ from the above**

**Resolution**: You can refer to the official AWS documentation on IAM policies:

* See [here](https://docs.aws.amazon.com/IAM/latest/UserGuide/tutorial_managed-policies.html) for a tutorial on how to create customer managed IAM policies.
* See [here](https://docs.aws.amazon.com/IAM/latest/UserGuide/troubleshoot_policies.html) for troubleshooting IAM policies.

## Deployment failure due to whitespace character in USERPROFILE path

**Why is this happening**: This happens due to a know issue with the AWS Cloud Development Kit (CDK). The CDK is used to AWS.Deploy.Tools under the covers and it cannot cannot access the `$TEMP` directory inside the `$USERPROFILE` path if it contains a whitespace character.

**Resolution**: See [here](../../docs/getting-started/custom-workspace) for guidance on setting a custom workspace that will be used by AWS.Deploy.tools.