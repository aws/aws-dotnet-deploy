# Set up credentials

The AWS Deploy Tool for .NET internally uses a variety of different tools and services to host your .NET application on AWS. To run the AWS Deploy Tool, you must configure a credential profile that provides access to the AWS account you wish to deploy to. Your credentials must have permissions for certain services, depending on the tasks that you're trying to perform.

### Recommended policies

The AWS Deploy Tool for .NET uses [AWS Cloud Development Kit (CDK)](https://docs.aws.amazon.com/cdk/v2/guide/home.html) to create the AWS infrastructure needed to deploy your application. Deploying via AWS CDK will assume roles that were created when [bootstrapping](https://docs.aws.amazon.com/cdk/v2/guide/bootstrapping.html) CDK for the account and region you are deploying into. Ensure that the profile you are deploying with has permission to assume the CDK deployment roles. This can be done with a policy such as:
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sts:AssumeRole"
            ],
            "Resource": [
                "arn:aws:iam::*:role/cdk-*"
            ]
        }
    ]
}
```

In addition to permission to assume the CDK deployment roles, the following are examples of using [AWS managed policies](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies_managed-vs-inline.html#aws-managed-policies) to provide additional permissions that are required for different commands.

  > *Note: Additional permissions might be required, depending on the type of application you're deploying and the services it uses.*

|Command| Task | Recommended AWS Managed Policies |
| --- | --- |--- |
|deploy | Deploying to Amazon ECS | AWSCloudFormationFullAccess, AmazonECS_FullAccess, AmazonEC2ContainerRegistryFullAccess, AmazonSSMFullAccess, IAMFullAccess |
|deploy | Deploying to AWS App Runner| AWSCloudFormationFullAccess, AWSAppRunnerFullAccess, AmazonEC2ContainerRegistryFullAccess, AmazonSSMFullAccess, IAMFullAccess|
|deploy | Deploying to AWS Elastic Beanstalk | AWSCloudFormationFullAccess, AdministratorAccess-AWSElasticBeanstalk, AmazonSSMFullAccess, AmazonS3FullAccess (*required to upload the application bundle*), IAMFullAccess |
|deploy | Hosting WebAssembly Blazor App in Amazon S3 & Amazon CloudFront | AmazonS3FullAccess, CloudFrontFullAccess, IAMFullAccess, AmazonSSMFullAccess, AWSLambda_FullAccess (*required to copy from CDKBootstrap bucket to S3 bucket*)|
| list-deployments | List AWS CloudFormation stacks| AWSCloudFormationReadOnlyAccess  |
| delete-deployment | Delete an AWS CloudFormation stack | AWSCloudFormationFullAccess + permissions for resources being deleted |

  > Note: If you are creating IAM roles, you need IAMFullAccess otherwise IAMReadOnlyAccess. Note that the first time the CDK bootstrap stack is created it will need IAMFullAccess.



  > Note: If you encounter an error saying **`user is not authorized to perform action because no identity based policies allow it`**, that means you need to add the corresponding permission to the IAM policy that is used by the current IAM role/user. The exact wording for an insufficient permissions related errors may differ.


### Specifying profile and region

In your shared AWS config and credentials files, if the `[default]` profile exists, the deployment tool uses that profile by default. You can change this behavior by specifying a profile for the tool to use, either system-wide or in a particular context.

####... locally
* The simplest way to specify region and profile is to provide them as parameters to the tool.

```
dotnet aws deploy --profile customProfile --region us-west-2
```

For additional information about command parameters, see [Commands](../../docs/commands/deploy.md) section.

  > **Note**
  > If you provide only the `--profile` argument, the AWS Region isn't read from the profile that you specify. Instead, the tool reads the Region from the `[default]` profile if one exists, or asks for the desired profile interactively.

####... system-wide

To specify a system-wide profile and region, define the `AWS_PROFILE` and `AWS_REGION` environment variables globally, as appropriate for your operating system. Be sure to reopen command prompts or terminals as necessary.

  > **Warning**
  > If you set the `AWS_PROFILE` environment variable globally for your system, other SDKs, CLIs, and tools will also use that profile. If this behavior is unacceptable, specify a profile for a particular context instead.

### Additional Resources

* For information on AWS credentials and access management, see [Credentials and Access](https://docs.aws.amazon.com/sdkref/latest/guide/access.html)
* For information on configuration file settings, see [Config and Auth Settings Reference](https://docs.aws.amazon.com/sdkref/latest/guide/settings-reference.html)
* For information on how to create customer managed IAM policies, see [Tutorial on Managed Policies](https://docs.aws.amazon.com/IAM/latest/UserGuide/tutorial_managed-policies.html)
* For information on how to troubleshoot IAM policies, see [AWS IAM User Guide](https://docs.aws.amazon.com/IAM/latest/UserGuide/troubleshoot_policies.html)
* For information on AWS Single Sign On (AWS SSO),  visit the [.NET SDK Reference Guide](https://docs.aws.amazon.com/sdkref/latest/guide/access-sso.html).
* For information on how to provide AWS credentials in AWS Toolkit for Visual Studio, see [AWS Toolkit for Visual Studio User Guide](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/credentials.html).
