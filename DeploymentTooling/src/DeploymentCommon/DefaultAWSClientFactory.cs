using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;

namespace AWS.DeploymentCommon
{
    public class DefaultAWSClientFactory : IAWSClientFactory
    {
        public T GetAWSClient<T>(AWSCredentials credentials, string region) where T : IAmazonService
        {
            var awsOptions = new AWSOptions
            {
                Credentials = credentials,
                Region = RegionEndpoint.GetBySystemName(region)
            };

            return awsOptions.CreateServiceClient<T>();
        }
    }
}