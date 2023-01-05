namespace AWS.Deploy.Tools.CI.Models;

/// <summary>
/// Input to the Lambda function <see cref="CodeBuildFunctions.GetAvailableTestRunner"/>
/// </summary>
public class GetAvailableTestRunnerInput
{
    /// <summary>
    /// Comma-separated list of IAM roles in the test runner accounts that have a trust relationship 
    /// with the AWS account that will be hosting the Lambda function <see cref="CodeBuildFunctions.GetAvailableTestRunner"/>
    /// These roles will be used to invoke a CodeBuild job hosted in the test runner accounts.
    /// </summary>
    public string Roles { get; set; }
    
    /// <summary>
    /// The CodeBuild project name that will be invoked in the test runner account.
    /// </summary>
    public string ProjectName { get; set; }

    /// <summary>
    /// The GitHub branch/commit-id that will be passed to the CodeBuild project.
    /// </summary>
    public string Branch { get; set; }

    public GetAvailableTestRunnerInput(string roles, string projectName, string branch)
    {
        Roles = roles;
        ProjectName = projectName;
        Branch = branch;
    }
}
