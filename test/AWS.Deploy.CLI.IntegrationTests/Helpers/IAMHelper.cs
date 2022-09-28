// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class IAMHelper
    {
        private readonly IAmazonIdentityManagementService _client;
        private readonly IToolInteractiveService _interactiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public IAMHelper(IAmazonIdentityManagementService client, IAWSResourceQueryer awsResourceQueryer, IToolInteractiveService toolInteractiveService)
        {
            _client = client;
            _awsResourceQueryer = awsResourceQueryer;
            _interactiveService = toolInteractiveService;
        }

        /// <summary>
        /// Delete an existing IAM Role by first removing the role from an existing instance profile and deleting that profile.
        /// </summary>
        public async Task DeleteRoleAndInstanceProfileAfterBeanstalkEnvionmentDeployment(string roleName)
        {
            var existingRoles = await _awsResourceQueryer.ListOfIAMRoles("ec2.amazonaws.com");
            var role = existingRoles.FirstOrDefault(x => string.Equals(roleName, x.RoleName));
            if (role != null)
            {
                await _client.RemoveRoleFromInstanceProfileAsync(new RemoveRoleFromInstanceProfileRequest
                {
                    RoleName = roleName,
                    InstanceProfileName = roleName
                });

                await _client.DeleteInstanceProfileAsync(new DeleteInstanceProfileRequest()
                {
                    InstanceProfileName = roleName
                });

                await _client.DeleteRoleAsync(new DeleteRoleRequest { RoleName = role.RoleName });
            }
        }

        public async Task CreateRoleForBeanstalkEnvionmentDeployment(string roleName)
        {
            _interactiveService.WriteLine($"Creating role {roleName} for deployment to Elastic Beanstalk environemnt");
            var existingRoles = await _awsResourceQueryer.ListOfIAMRoles("ec2.amazonaws.com");
            var role = existingRoles.FirstOrDefault(x => string.Equals(roleName, x.RoleName));
            if (role != null)
            {
                _interactiveService.WriteLine($" The role {roleName} already exists");
            }
            else
            {
                var assumeRolepolicyDocument =
               @"{
                   'Version':'2008-10-17',
                   'Statement':[
                      {
                         'Effect':'Allow',
                         'Principal':{
                            'Service':'ec2.amazonaws.com'
                         },
                         'Action':'sts:AssumeRole'
                      }
                   ]
                }";

                await _client.CreateRoleAsync(new CreateRoleRequest
                {
                    RoleName = roleName,
                    AssumeRolePolicyDocument = assumeRolepolicyDocument.Replace("'", "\""),
                    MaxSessionDuration = 7200
                });
            }

            InstanceProfile instanceProfile = null;
            try
            {
                instanceProfile = (await _client.GetInstanceProfileAsync(new GetInstanceProfileRequest()
                {
                    InstanceProfileName = roleName
                })).InstanceProfile;
            }
            catch (NoSuchEntityException) { }

            // Check to see if an instance profile exists for this role and if not create it.
            if (instanceProfile == null)
            {
                _interactiveService.WriteLine($"Creating new IAM Instance Profile {roleName}");
                await _client.CreateInstanceProfileAsync(new CreateInstanceProfileRequest()
                {
                    InstanceProfileName = roleName
                });

                _interactiveService.WriteLine($"Attaching IAM role {roleName} to Instance Profile {roleName}");
                await _client.AddRoleToInstanceProfileAsync(new AddRoleToInstanceProfileRequest()
                {
                    RoleName = roleName,
                    InstanceProfileName = roleName
                });
            }
            // If it already exists see if this role is already assigned and if not assign it.
            else
            {
                _interactiveService.WriteLine($"IAM Instance Profile {roleName} already exists");
                if (instanceProfile.Roles.FirstOrDefault(x => x.RoleName == roleName) == null)
                {
                    _interactiveService.WriteLine($"Attaching IAM role {roleName} to Instance Profile {roleName}");
                    await _client.AddRoleToInstanceProfileAsync(new AddRoleToInstanceProfileRequest()
                    {
                        RoleName = roleName,
                        InstanceProfileName = roleName
                    });
                }
            }
        }
    }
}
