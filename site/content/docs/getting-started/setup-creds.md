# Setting up credentials for the deployment tool<a name="deployment-tool-setup-creds"></a>

****  

The information shown below is about how to set up credentials for the deployment tool. If you're looking for information about setting up credentials for your .NET project, see [Configure AWS credentials](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html) in the [AWS SDK for .NET Developer Guide](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html) instead.

To run the deployment tool against your AWS account, you must have a credentials profile in your shared AWS config and credentials files. The profile must be set up with at least an access key ID and a secret access key for an AWS Identity and Access Management (IAM) user. There are various ways to do this. For information see the following references:

- [Create users and roles](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-users-roles.html) and [Using the shared AWS credentials file](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-file.html) in the AWS SDK for .NET Developer Guide.
- [Providing AWS credentials](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/credentials.html) in the [AWS Toolkit for Visual Studio User Guide](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/welcome.html).
- [Configuration and credential file settings](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html) in the [AWS Command Line Interface User Guide](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-welcome.html).

The credentials that you use to run the deployment tool must have permissions for certain services, depending on the tasks that you're trying to perform. The following are some examples of the typical permissions that are required to run the tool. Additional permissions might be required, depending on the type of application you're deploying and the services it uses.

| Task | Permissions for services | 
| --- |--- |
| Display a list of AWS CloudFormation stacks (list-deployments) | CloudFormation | 
| Deploy and redeploy to Elastic Beanstalk (deploy) | CloudFormation, Elastic Beanstalk | 
| Deploy and redeploy to Amazon ECS (deploy) | CloudFormation, Elastic Beanstalk, Elastic Container Registry | 

For additional information about permissions, see the topics in the [Troubleshooting Guide](../../troubleshooting-guide.md).

In your shared AWS config and credentials files, if the `[default]` profile exists, the deployment tool uses that profile by default. You can change this behavior by specifying a profile for the tool to use, either system-wide or in a particular context.

To specify a system-wide profile, do the following:

* Define the `AWS_PROFILE` environment variable globally, as appropriate for your operating system. Be sure to reopen command prompts or terminals as necessary. If the profile you specify doesn't include an AWS Region, the tool might ask you to choose one.
  > **Warning**
  > If you set the `AWS_PROFILE` environment variable globally for your system, other SDKs, CLIs, and tools will also use that profile. If this behavior is unacceptable, specify a profile for a particular context instead.

To specify a profile for a particular context, do one of the following:

* Define the `AWS_PROFILE` environment variable in the command prompt or terminal session from which you're running the tool (as appropriate for your operating system).
* Provide the `--profile` and `--region` command switches. For example: `dotnet-aws list-deployments --region us-west-2`. For additional information about the deployment tool's commands, see [Running the deployment tool](run-tool.md).
  > **Note**
  > If you provide only the `--profile` argument, the AWS Region isn't read from the profile that you specify. Instead, the tool reads the Region from the `[default]` profile if one exists, or asks for the desired profile interactively.
* Specify nothing and, assuming that you don't have a `[default]` profile, the tool will ask you to choose a profile and an AWS Region.

## Next steps
- See how to [run the tool](run-tool.md)