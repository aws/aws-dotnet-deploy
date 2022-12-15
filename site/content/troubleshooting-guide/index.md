# General Issues
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

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<PropertyGroup>
		<TargetFrameworkVersion>net6.0</TargetFrameworkVersion>
		<TargetFramework>$(TargetFrameworkVersion)</TargetFramework>
	</PropertyGroup>
</Project>
```

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

**Why is this happening**: This happens due to a know issue with the AWS Cloud Development Kit (CDK). The CDK is used by AWS.Deploy.Tools under the covers and it cannot cannot access the `$TEMP` directory inside the `$USERPROFILE` path if it contains a whitespace character.

**Resolution**: See [here](../docs/getting-started/custom-workspace.md) for guidance on setting a custom workspace that will be used by AWS.Deploy.Tools.

## AWS CDK Bootstrap related Deployment Failure

A common error that is displayed for CDK Bootstrap related deployment failures is the following:
```
The AWS CDK Bootstrap, which is the process of provisioning initial resources for the deployment environment, has failed. Please review the output above for additional details [and check out our troubleshooting guide for the most common failure reasons]. You can learn more about CDK bootstrapping at https://docs.aws.amazon.com/cdk/v2/guide/bootstrapping.html.
```
The AWS Deploy Tool for .NET uses [AWS CDK](https://docs.aws.amazon.com/cdk/v2/guide/home.html) to create the AWS infrastructure needed to deploy your application. The AWS CDK is a framework for defining cloud infrastructure in code and provisioning it through AWS CloudFormation.

Deploying AWS CDK apps into an AWS environment (a combination of an AWS account and region) requires that you provision resources the AWS CDK needs to perform the deployment. These resources include an Amazon S3 bucket for storing files and IAM roles that grant permissions needed to perform deployments. The process of provisioning these initial resources is called [bootstrapping](https://docs.aws.amazon.com/cdk/v2/guide/bootstrapping.html).

The required resources are defined in a AWS CloudFormation stack, called the bootstrap stack, which is usually named `CDKToolkit`. Like any AWS CloudFormation stack, it appears in the AWS CloudFormation console once it has been deployed.

There could be several reasons why you are experiencing this issue. However, the most common ones are related to **Insufficient IAM Permissions** and an **Existing CDK Staging Bucket**.

**Insufficient IAM Permission**: CDKBoostrap failed because your profile does not have sufficient permissions to create the boostrap stack. Check the log - in this case, you should see an error that looks something like this:
```
LookupRole API: iam:GetRole User: arn:aws:iam::123456789101:user/user is not authorized to perform: iam:GetRole on resource
```

**Resolution**: Add missing IAM permissions to your profile. See our documentation for [recommended IAM policies](https://aws.github.io/aws-dotnet-deploy/docs/getting-started/setup-creds/) for each deployment type.

**Existing CDKToolkit S3 bucket**: In rare cases, it is possible for the CDK Boostrap process to not clean the resources properly after the failed deployment. This causes the next deployment to fail as well, because the S3 bucket already exists. The error could look something like:
```
StagingBucket cdk-hnb659fds-assets-123456789101-us-west-2 already exists
```

**Resolution**: Open the AWS Console, go to S3 service, and manually delete the 'CDKToolkit' S3 bucket. Once the bucket is deleted, go ahead and deploy your application.

## MemorySize Constraint for Blazor WebAssembly
When attempting to deploy using the Blazor WebAssembly App recipe, you may see a deployment failure such as:
```
Resource handler returned message: "'MemorySize' value failed to satisfy constraint: Member must have value less than or equal to 3008
```

**Why this is happening:** The [BucketDeployment](https://docs.aws.amazon.com/cdk/api/v2/docs/aws-cdk-lib.aws_s3_deployment.BucketDeployment.html) CDK Construct used to deploy the Blazor recipe uses an AWS Lambda function to replicate the application files from the CDK bucket to the deployment bucket. In some versions of the deploy tool the default memory limit for this Lambda function exceeded the 3008MB quota placed on new AWS accounts.

**Resolution:** See [Lambda: Concurrency and memory quotas](https://docs.aws.amazon.com/lambda/latest/dg/troubleshooting-deployment.html#troubleshooting-deployment-quotas) for how to request a quota increase.

## App Runner Failed with _Resource handler returned message: "null"_
When attempting to deploy to App Runner, creation of the `AWS::AppRunner::Service` resource may fail with a message such as:
```
CREATE_FAILED | AWS::AppRunner::Service | Recipe/AppRunnerService (RecipeAppRunnerService) Resource handler returned message: "null"
```

**Why this is happening:** This error could happen for a variety of reasons, such as the application failing its initial health check or limited permissions.

**Resolution:** The resolution will depend on the failure reason. To aid diagnosis, attempt to deploy your application again. While it is deploying, navigate to the the Deployment and Application logs sections of _App Runner > Services > [name of your cloud application]_ in the AWS Console and review the logs for any unexpected errors. See [Viewing App Runner logs streamed to CloudWatch Logs](https://docs.aws.amazon.com/apprunner/latest/dg/monitor-cwl.html) for more details.