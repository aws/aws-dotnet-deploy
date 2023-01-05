using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.CodeBuild;
using Amazon.CodeBuild.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using AWS.Deploy.Tools.CI.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWS.Deploy.Tools.CI;

/// <summary>
/// Lambda functions that will interact with CodeBuild as part of the AWS.Deploy.Tools CI/CD pipeline.
/// </summary>
public class CodeBuildFunctions
{
    private readonly IAmazonSecurityTokenService _githubSTSClient;

    public CodeBuildFunctions(IAmazonSecurityTokenService githubSTSClient)
    {
        _githubSTSClient = githubSTSClient;
    }

    /// <summary>
    /// The <see cref="GetAvailableTestRunner"/> function is responsible for checking available test runner accounts to run the AWS.Deploy.Tools CodeBuild PR check.
    /// A list of IAM roles, representing the test runner accounts, is passed to the function. 
    /// These roles are assumed and used to check if the CodeBuild CI project in the test runner account is currently running any jobs.
    /// </summary>
    /// <returns>The function will return the IAM role of the account that is not running any CodeBuild CI jobs.</returns>
    /// <exception cref="ArgumentNullException">If the input passed to the function is invalid.</exception>
    /// <exception cref="Exception">If no test runner account is available.</exception>
    /// <exception cref="Exception">If a CodeBuild CI project is not found in the test runner account.</exception>
    [LambdaFunction(Name = "GetAvailableTestRunner", Policies = "@AWSDeployToolsCIGetAvailableTestRunnerLambdaAssumeRolePolicy")]
    public async Task<string> GetAvailableTestRunner(GetAvailableTestRunnerInput input)
    {
        if (string.IsNullOrEmpty(input.Roles))
        {
            throw new ArgumentNullException(nameof(input.Roles));
        }
        if (string.IsNullOrEmpty(input.ProjectName))
        {
            throw new ArgumentNullException(nameof(input.ProjectName));
        }
        if (string.IsNullOrEmpty(input.Branch))
        {
            throw new ArgumentNullException(nameof(input.Branch));
        }

        var roles = input.Roles.Split(",").Select(x => x.Trim()).ToList();

        if (!roles.Any())
        {
            throw new ArgumentNullException(nameof(input.Roles));
        }

        foreach (var role in roles)
        {
            var assumeRoleResponse =
                await _githubSTSClient.AssumeRoleAsync(
                    new AssumeRoleRequest
                    {
                        RoleArn = role,
                        RoleSessionName = "DeployToolTestRunner"
                    }
                );

            using var testRunnerSTSClient = new AmazonSecurityTokenServiceClient(assumeRoleResponse.Credentials);
            var callerIdentity = await testRunnerSTSClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());

            using var codeBuildClient = new AmazonCodeBuildClient(assumeRoleResponse.Credentials);

            var batchGetProjectsResponse =
                await codeBuildClient.BatchGetProjectsAsync(
                    new BatchGetProjectsRequest
                    {
                        Names = new List<string> { input.ProjectName }
                    }
                );

            if (!batchGetProjectsResponse.Projects.Any())
            {
                throw new Exception($"Could not find any project with the name '{input.ProjectName}' in account '{callerIdentity.Account}'.");
            }

            var project = batchGetProjectsResponse.Projects.First();

            var listBuildsForProjectResponse =
                await codeBuildClient.ListBuildsForProjectAsync(
                    new ListBuildsForProjectRequest
                    {
                        ProjectName = project.Name
                    }
                );

            var runningBuilds = 0;
            if (listBuildsForProjectResponse.Ids.Any())
            {
                var latestBuilds = listBuildsForProjectResponse.Ids.Take(20).ToList();
                var detailedBuilds =
                    await codeBuildClient.BatchGetBuildsAsync(
                        new BatchGetBuildsRequest
                        {
                            Ids = latestBuilds
                        }
                    );

                foreach (var detailedBuild in detailedBuilds.Builds)
                {
                    if (detailedBuild.BuildComplete == false)
                    {
                        runningBuilds++;
                    }
                }
            }

            if (runningBuilds < project.ConcurrentBuildLimit)
            {
                return role;
            }
            else
            {
                continue;
            }
        }

        throw new Exception("There are no available Test Runner accounts.");
    }
}
