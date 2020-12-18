using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace AspNetAppElasticBeanstalkLinux
{
    /// <summary>
    /// Provides name of the latest 64bit Amazon Linux 2 running .NET Core.
    /// </summary>
    public class SolutionStackNameProvider
    {
        private readonly AmazonElasticBeanstalkClient _client;

        public SolutionStackNameProvider()
        {
            _client = new AmazonElasticBeanstalkClient();
        }

        /// <summary>
        /// Returns latest 64bit Amazon Linux 2 running .NET Core.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task<string> GetSolutionStackNameAsync()
        {
            var request = new ListAvailableSolutionStacksRequest();
            var response = await _client.ListAvailableSolutionStacksAsync(request);
            var netCoreSolutionStack = response.SolutionStacks.Where(stack => stack.EndsWith("running .NET Core"));
            if (!netCoreSolutionStack.Any())
            {
                throw new AmazonElasticBeanstalkException(".NET Core Solution Stack doesn't exist.");
            }

            // Assuming solution stack list is ordered latest to oldest as per documentation
            return netCoreSolutionStack.First();
        }
    }
}
