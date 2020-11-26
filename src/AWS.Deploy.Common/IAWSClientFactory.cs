using Amazon.Runtime;

namespace AWS.DeploymentCommon
{
    public interface IAWSClientFactory
    {
        T GetAWSClient<T>(AWSCredentials credentials, string region) where T : IAmazonService;
    }
}