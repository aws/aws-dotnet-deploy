// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;

using Amazon.CDK;
using Amazon.CDK.AWS.BedrockAgentCore;
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
        /// The Bedrock AgentCore Runtime.
        /// </summary>
        public BedrockRuntime? AgentCoreRuntime { get; private set; }

        public Recipe(Construct scope, IRecipeProps<Configuration> props)
            : base(scope, "Recipe")
        {
            var settings = props.Settings;

            ConfigureRuntimeIAMRole(settings);
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
                Actions = new[] { "bedrock-agentcore:ListEvents", "bedrock-agentcore:CreateEvent" },
                Resources = new[] { $"arn:{partition}:bedrock-agentcore:*:{account}:memory/*" }
            }));

            RuntimeRoleArn = RuntimeRole.RoleArn;
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

            // Configure network mode
            var networkConfiguration = RuntimeNetworkConfiguration.UsingPublicNetwork();

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
