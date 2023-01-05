# AWS.Deploy.Tools.CI

The `AWS.Deploy.Tools.CI` project is used to setup the CI/CD PR check between the [AWS.Deploy.Tools](https://github.com/aws/aws-dotnet-deploy) GitHub Repo and the AWS CodeBuild project that will be executed by the GitHub workflow `AWS CodeBuild CI (codebuild-ci.yml)`.

The workflow will be executed starting from `AWS CodeBuild CI (codebuild-ci.yml)` PR check, which will call a Lambda function in an `AWS Account` (which will act as a load balancer) which will ping 1 or many `Test Runner` accounts to check for availability.

## Main Account (Load Balancer)

We will start by deploying the `Lambda function` to the main testing account. Make sure you have AWS credentials with `Admin` access for the main testing account stored in your `credentials` file locally. 

From the [_**AWS.Deploy.Tools.CI Project Directory**_](/buildtools/AWS.Deploy.Tools.CI/AWS.Deploy.Tools.CI/), run the following commands:
```
dotnet tool update -g Amazon.Lambda.Tools
dotnet lambda deploy-serverless --profile <main-testing-account-admin-profile> --region <main-testing-account-region> --config-file aws-lambda-tools-defaults.json  --resolve-s3 true
```

The `Lambda Function` should now be deployed in the main testing account. Log into the account and take note of the `Lambda Function` name as it will be needed in a later step. Also, check the Resources tab of the created stack an take note of the IAM role `AWSDeployToolsCIGitHubTrustRole` ARN.

## Test Runner Accounts

You can setup 1 or multiple test runner accounts. These are new AWS Accounts other than the main account that containes the Lambda function. These accounts will run the `AWS CodeBuild Project` defined in [ci.template.yml](./ci.template.yml)

The setup needs an OIDC Identity provider to be defined for the Test Runner account. If one exists, take note of the ARN. If not, go to [Identity providers](https://us-east-1.console.aws.amazon.com/iamv2/home?region=us-west-2#/identity_providers) and create one.
Use the following config:
* Provider: `token.actions.githubusercontent.com`
* Audiences: `sts.amazonaws.com`
* Generate Thumbprint

To create the `AWS CodeBuild Project` in a test runner account
1. Go to CloudFormation, create a Stack using [buildtools/ci.template](ci.template.yml)
2. Use the following variables:
    * Stack name: `aws-dotnet-deploy-ci`
    * CodeBuildProjectName: `aws-dotnet-deploy-ci`
    * GitHubOrg: `aws`
    * GitHubRepositoryName: `aws-dotnet-deploy`
    * MainAWSAccountId : *Main AWS Account ID that you deployed the Lambda function to*
    * OIDCProviderArn: *ARN of the OIDC Identity Provider for the Test Runner account*
    * TestRunnerTrustRoleName: `aws-dotnet-deploy-ci-role`
2. Once the Stack is created, take note of CodeBuild Project name `aws-dotnet-deploy-ci` and the new `TestRunnerTrustRole` ARN that you can find in the Resources tab of the created stack.

Repeat the above steps for every account that you want to use as a Test Runner.

## GitHub Workflow

In order for the GitHub workflow `AWS CodeBuild CI (codebuild-ci.yml)` to work properly, we need to set some GitHub secrets on the repo. Based on the names/ARNs you noted from previous steps, add the following secrets:
* CI_TESTING_LOAD_BALANCER_LAMBDA_NAME: *From Main Account step, this is the name of the Lambda function*
* CI_MAIN_TESTING_ACCOUNT_ROLE_ARN: *From Main Account step, this is the IAM role AWSDeployToolsCIGitHubTrustRole ARN*
* CI_TESTING_CODE_BUILD_PROJECT_NAME: *From Test Runner Accounts step, this is `aws-dotnet-deploy-ci`*
* CI_TEST_RUNNER_ACCOUNT_ROLES: This is a comma-delimited string of `TestRunnerTrustRole` ARNs from the Test Runner Accounts step

# Troubleshooting

## thumbprint rotation
```
Error: OpenIDConnect provider's HTTPS certificate doesn't match configured thumbprint
```

This can happen if GitHub has rotated the thumbprint of the certificate. Follow [this guide](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_create_oidc_verify-thumbprint.html) to generate new thumbprint.

Redeploy the ci.template with the new thumbprint. Additionally, contact https://github.com/aws-actions/configure-aws-credentials/issues for the thumbprint rotation.