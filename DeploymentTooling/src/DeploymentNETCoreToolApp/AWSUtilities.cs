using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace AWS.DeploymentNETCoreToolApp
{
    public class AWSUtilities
    {
        private readonly IToolInteractiveService _toolInteractiveService;

        public AWSUtilities(IToolInteractiveService toolInteractiveService)
        {
            _toolInteractiveService = toolInteractiveService;
        }

        public AWSCredentials ResolveAWSCredentials(string profileName, string lastUsedProfileName)
        {
            var chain = new CredentialProfileStoreChain();
            AWSCredentials credentials = null;

            if(!string.IsNullOrEmpty(profileName))
            {
                if(chain.TryGetAWSCredentials(profileName, out credentials))
                {
                    _toolInteractiveService.WriteLine($"Configuring AWS Credentials from Profile {profileName}.");
                    return credentials;
                }
            }

            if (!string.IsNullOrEmpty(lastUsedProfileName) && chain.TryGetAWSCredentials(lastUsedProfileName, out credentials))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS Credentials with previous configured profile value {lastUsedProfileName}.");
                return credentials;
            }

            try
            {
                credentials = FallbackCredentialsFactory.GetCredentials();
                _toolInteractiveService.WriteLine($"Configuring AWS Credentials using AWS SDK credential search.");
                return credentials;
            }
            catch(AmazonServiceException)
            {
                // FallbackCredentialsFactory throws an exception if no credentials are found. Burying exception because if no credentials are found
                // we want to continue and ask the user to select a profile.
            }

            var sharedCredentials = new SharedCredentialsFile();
            if (sharedCredentials.ListProfileNames().Count == 0)
            {
                throw new NoAWSCredentialsFoundException(NoAWSCredentialsFoundException.UNABLE_RESOLVE_MESSAGE);
            }

            var consoleUtilities = new ConsoleUtilities(_toolInteractiveService);
            var selectedProfileName = consoleUtilities.AskUserToChoose(sharedCredentials.ListProfileNames(), "Select AWS Credentials Profile", null);

            if(!chain.TryGetAWSCredentials(selectedProfileName, out credentials))
            {
                throw new NoAWSCredentialsFoundException($"Unable to create AWS credentials for profile {profileName}.");
            }

            return credentials;
        }

        public string ResolveAWSRegion(string region, string lastRegionUsed)
        {
            if(!string.IsNullOrEmpty(region))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS region with specified value {region}.");
                return region;
            }
            if (!string.IsNullOrEmpty(lastRegionUsed))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS region with previous configured value {lastRegionUsed}.");
                return lastRegionUsed;
            }

            var fallbackRegion = FallbackRegionFactory.GetRegionEndpoint()?.SystemName;
            if(!string.IsNullOrEmpty(fallbackRegion))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS region using AWS SDK region search to {region}.");
                return fallbackRegion;
            }

            var availableRegions = new List<string>();
            foreach(var value in Amazon.RegionEndpoint.EnumerableAllRegions.OrderBy(x => x.SystemName))
            {
                availableRegions.Add($"{value.SystemName} ({value.DisplayName})");
            }

            var consoleUtilities = new ConsoleUtilities(_toolInteractiveService);
            var selectedRegion = consoleUtilities.AskUserToChoose(availableRegions, "Select AWS Region", null);

            // Strip display name
            selectedRegion = selectedRegion.Substring(0, selectedRegion.IndexOf('(') - 1).Trim();

            return selectedRegion;
        }

        public async Task<IList<string>> GetListOfElasticBeanstalkApplications(IAmazonElasticBeanstalk beanstalkClient)
        {
            var response = await beanstalkClient.DescribeApplicationsAsync();

            var applicationNames = new List<string>();
            foreach (var application in response.Applications)
            {
                applicationNames.Add(application.ApplicationName);
            }

            return applicationNames;
        }

        public async Task<IList<string>> GetListOfElasticBeanstalkEnvironments(IAmazonElasticBeanstalk beanstalkClient,
            string applicationName)
        {
            var environmentNames = new List<string>();
            
            var request = new DescribeEnvironmentsRequest {ApplicationName = applicationName};
            DescribeEnvironmentsResponse response;
            do
            {
                response = await beanstalkClient.DescribeEnvironmentsAsync();
                request.NextToken = response.NextToken;
                
                foreach (var environment in response.Environments)
                {
                    environmentNames.Add(environment.EnvironmentName);
                }                
            } while (!string.IsNullOrEmpty(response.NextToken));

            return environmentNames;
        }
    }
}
