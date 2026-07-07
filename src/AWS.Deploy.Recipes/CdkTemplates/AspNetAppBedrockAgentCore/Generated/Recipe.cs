// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.CDK;
using Amazon.CDK.AWS.BedrockAgentCore;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.IAM;

using AWS.Deploy.Recipes.CDK.Common;

using AspNetAppBedrockAgentCore.Configurations;
using Constructs;
using PolicyStatement = Amazon.CDK.AWS.IAM.PolicyStatement;

// This is a generated file from the original deployment recipe. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// To customize the CDK constructs created in this file you should use the AppStack.CustomizeCDKProps() method.

namespace AspNetAppBedrockAgentCore
{
    using static AWS.Deploy.Recipes.CDK.Common.CDKRecipeCustomizer<Recipe>;
    using BedrockRuntime = Amazon.CDK.AWS.BedrockAgentCore.Runtime;

    public class Recipe : Construct
    {
        /// <summary>
        /// The IAM role used by the AgentCore Runtime.
        /// </summary>
        public Role? RuntimeRole { get; private set; }

        /// <summary>
        /// The IAM role ARN for the Runtime (works for both new and existing roles).
        /// </summary>
        public string? RuntimeRoleArn { get; private set; }

        /// <summary>
        /// The AgentCore Memory resource (null when not configured or using existing).
        /// </summary>
        public CfnMemory? Memory { get; private set; }

        /// <summary>
        /// The Bedrock AgentCore Runtime.
        /// </summary>
        public BedrockRuntime? AgentCoreRuntime { get; private set; }

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            : base(scope, "Recipe")
        {
            var settings = props.Settings;

            ConfigureRuntimeIAMRole(settings);
            ConfigureMemory(settings);
            ConfigureAgentCoreRuntime(props, settings);
        }

        /// <summary>
        /// Creates or references the IAM role for the AgentCore Runtime.
        /// When CreateNew is true, the role gets an opinionated set of permissions:
        /// ECR pull, CloudWatch Logs, Bedrock model invocation (cross-region), and AgentCore Memory.
        /// When CreateNew is false, the existing role ARN is used as-is.
        /// </summary>
        private void ConfigureRuntimeIAMRole(Configuration settings)
        {
            if (!settings.RuntimeIAMRole.CreateNew)
            {
                if (string.IsNullOrEmpty(settings.RuntimeIAMRole.RoleArn))
                    throw new InvalidOrMissingConfigurationException("The provided Runtime IAM Role ARN is null or empty.");

                RuntimeRoleArn = settings.RuntimeIAMRole.RoleArn;
                return;
            }

            var stack = Stack.Of(this);
            var region = stack.Region;
            var account = stack.Account;
            var partition = Aws.PARTITION;

            RuntimeRole = new Role(this, nameof(RuntimeRole), InvokeCustomizeCDKPropsEvent(nameof(RuntimeRole), this, new RoleProps
            {
                AssumedBy = new ServicePrincipal("bedrock-agentcore.amazonaws.com", new ServicePrincipalOpts
                {
                    Conditions = new Dictionary<string, object>
                    {
                        ["StringEquals"] = new Dictionary<string, string>
                        {
                            ["aws:SourceAccount"] = account
                        },
                        ["ArnLike"] = new Dictionary<string, string>
                        {
                            ["aws:SourceArn"] = $"arn:{partition}:bedrock-agentcore:{region}:{account}:*"
                        }
                    }
                })
            }));

            RuntimeRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryReadOnly"));
            RuntimeRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchLogsFullAccess"));

            RuntimeRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "bedrock:InvokeModel*" },
                Resources = new[]
                {
                    $"arn:{partition}:bedrock:*::foundation-model/*",
                    $"arn:{partition}:bedrock:*:{account}:inference-profile/*"
                }
            }));

            RuntimeRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "bedrock-agentcore:CreateEvent",
                    "bedrock-agentcore:GetEvent",
                    "bedrock-agentcore:ListEvents",
                    "bedrock-agentcore:DeleteEvent",
                    "bedrock-agentcore:ListActors",
                    "bedrock-agentcore:ListSessions",
                    "bedrock-agentcore:RetrieveMemoryRecords",
                    "bedrock-agentcore:GetMemoryRecord",
                    "bedrock-agentcore:ListMemoryRecords"
                },
                Resources = new[] { $"arn:{partition}:bedrock-agentcore:*:{account}:memory/*" }
            }));

            RuntimeRoleArn = RuntimeRole.RoleArn;
        }

        /// <summary>
        /// Creates or references the AgentCore Memory resource.
        /// When CreateNew is true, provisions a new CfnMemory with a default episodic strategy.
        /// When an existing MemoryId is provided, it will be injected as an env var on the runtime.
        /// </summary>
        private void ConfigureMemory(Configuration settings)
        {
            var memoryConfig = settings.AgentCoreMemory;
            if (memoryConfig == null || (!memoryConfig.CreateNew && string.IsNullOrEmpty(memoryConfig.MemoryId)))
                return;

            if (memoryConfig.CreateNew)
            {
                Memory = new CfnMemory(this, nameof(Memory), InvokeCustomizeCDKPropsEvent(nameof(Memory), this, new CfnMemoryProps
                {
                    Name = $"{SanitizeResourceName(settings.RuntimeName, 41)}_memory",
                    EventExpiryDuration = 90
                }));

                new CfnOutput(this, "MemoryId", new CfnOutputProps
                {
                    Value = Memory.AttrMemoryId,
                    Description = "The ID of the AgentCore Memory resource"
                });
            }
        }

        /// <summary>
        /// Gets the effective Memory ID — from the newly created resource or the user-provided value.
        /// Returns null when memory is not configured.
        /// </summary>
        private string? GetEffectiveMemoryId(Configuration settings)
        {
            if (Memory != null)
                return Memory.AttrMemoryId;

            if (!string.IsNullOrEmpty(settings.AgentCoreMemory?.MemoryId))
                return settings.AgentCoreMemory.MemoryId;

            return null;
        }

        /// <summary>
        /// Configures VPC networking for the AgentCore Runtime.
        /// Uses public networking by default; switches to VPC mode when a VPC is configured.
        /// </summary>
        private RuntimeNetworkConfiguration ConfigureVpc(Configuration settings)
        {
            if (settings.VPC == null || !settings.VPC.UseVPC)
                return RuntimeNetworkConfiguration.UsingPublicNetwork();

            IVpc vpc;
            if (settings.VPC.CreateNew)
            {
                // Create a new VPC limited to 2 AZs to avoid unsupported AZ issues
                vpc = new Vpc(this, "RuntimeVpc", InvokeCustomizeCDKPropsEvent("RuntimeVpc", this, new VpcProps
                {
                    MaxAzs = 2
                }));
            }
            else if (!string.IsNullOrEmpty(settings.VPC.VpcId))
            {
                vpc = Vpc.FromLookup(this, "RuntimeVpc", new VpcLookupOptions
                {
                    VpcId = settings.VPC.VpcId
                });
            }
            else
            {
                throw new InvalidOrMissingConfigurationException(
                    "VPC mode is enabled but no VPC was specified. Either set CreateNew to true or provide a VpcId.");
            }

            // Prefer private subnets with egress (NAT) to ensure the runtime can reach AWS
            // service endpoints. Fall back to private isolated, then public as last resort.
            // AgentCore runtimes in public subnets are NOT assigned a public IP, so they
            // won't have internet access without a NAT — but we allow it rather than failing.
            // Limited to 2 AZs to avoid unsupported availability zones.
            SubnetSelection subnetSelection;
            if (vpc.PrivateSubnets.Length > 0)
            {
                subnetSelection = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    AvailabilityZones = vpc.AvailabilityZones.Take(2).ToArray()
                };
            }
            else
            {
                subnetSelection = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC,
                    AvailabilityZones = vpc.AvailabilityZones.Take(2).ToArray()
                };
            }

            var vpcConfigProps = new VpcConfigProps
            {
                Vpc = vpc,
                VpcSubnets = subnetSelection
            };

            if (settings.VPC.SecurityGroups.Count > 0)
            {
                vpcConfigProps.SecurityGroups = settings.VPC.SecurityGroups
                    .Select((sgId, i) => SecurityGroup.FromSecurityGroupId(this, $"RuntimeSG{i}", sgId))
                    .Cast<ISecurityGroup>()
                    .ToArray();
            }

            return RuntimeNetworkConfiguration.UsingVpc(this, vpcConfigProps);
        }

        /// <summary>
        /// Sanitizes a resource name to match AgentCore naming rules: starts with a letter,
        /// contains only alphanumeric characters and underscores, truncated to maxLength.
        /// </summary>
        private static string SanitizeResourceName(string name, int maxLength)
        {
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            if (sanitized.Length == 0 || !char.IsLetter(sanitized[0]))
                sanitized = "m" + sanitized;

            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            return sanitized;
        }

        /// <summary>
        /// Creates the Bedrock AgentCore Runtime using the CDK L2 construct.
        /// The runtime hosts your ASP.NET Core agent application as a managed container
        /// deployed from an ECR image.
        /// </summary>
        private void ConfigureAgentCoreRuntime(IRecipeProps<Configuration> props, Configuration settings)
        {
            if (string.IsNullOrEmpty(RuntimeRoleArn))
                throw new InvalidOperationException($"{nameof(RuntimeRoleArn)} has not been set. The {nameof(ConfigureRuntimeIAMRole)} method should be called before {nameof(ConfigureAgentCoreRuntime)}");

            if (string.IsNullOrEmpty(props.ECRRepositoryName))
                throw new InvalidOrMissingConfigurationException("The provided ECR Repository Name is null or empty.");

            // Build the artifact from the ECR repository
            var ecrRepository = Repository.FromRepositoryName(this, "ECRRepository", props.ECRRepositoryName);
            var artifact = AgentRuntimeArtifact.FromEcrRepository(ecrRepository, props.ECRImageTag ?? "latest");

            // Determine execution role: use newly created or import existing
            IRole executionRole = RuntimeRole ?? Role.FromRoleArn(this, "ImportedRuntimeRole", RuntimeRoleArn!);

            // Build environment variables dictionary
            var environmentVariables = new Dictionary<string, string>(settings.AgentCoreEnvironmentVariables);

            // Inject Memory ID if configured
            var memoryId = GetEffectiveMemoryId(settings);
            if (memoryId != null && !environmentVariables.ContainsKey("AWS_AGENTCORE_MEMORY_ID"))
            {
                environmentVariables["AWS_AGENTCORE_MEMORY_ID"] = memoryId;
            }

            // Configure network mode
            var networkConfiguration = ConfigureVpc(settings);

            var runtimeProps = new RuntimeProps
            {
                RuntimeName = settings.RuntimeName,
                AgentRuntimeArtifact = artifact,
                ExecutionRole = executionRole,
                NetworkConfiguration = networkConfiguration,
                EnvironmentVariables = environmentVariables
            };

            if (settings.RequestHeaders.Count > 0)
            {
                runtimeProps.RequestHeaderConfiguration = new RequestHeaderConfiguration
                {
                    AllowlistedHeaders = settings.RequestHeaders.ToArray()
                };
            }

            AgentCoreRuntime = new BedrockRuntime(this, nameof(AgentCoreRuntime),
                InvokeCustomizeCDKPropsEvent(nameof(AgentCoreRuntime), this, runtimeProps));

            // Output the Runtime ARN and ID
            new CfnOutput(this, "AgentRuntimeArn", new CfnOutputProps
            {
                Value = AgentCoreRuntime.AgentRuntimeArn,
                Description = "The ARN of the Bedrock AgentCore Runtime"
            });

            new CfnOutput(this, "AgentRuntimeId", new CfnOutputProps
            {
                Value = AgentCoreRuntime.AgentRuntimeId,
                Description = "The ID of the Bedrock AgentCore Runtime"
            });
        }
    }
}
