// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Reflection;

namespace AWS.DeploymentCommon
{
    public class ProjectFileNotFoundException : Exception
    {
        public ProjectFileNotFoundException(string projectPath)
            : base($"Project path {projectPath} not found.")
        {
            Path = projectPath;
        }

        public string Path { get; set; }
    }

    /// <summary>
    /// Throw if the user attempts to deploy a <see cref="RecipeDefinition"/>
    /// that uses <see cref="RecipeDefinition.DeploymentTypes.CdkProject"/>
    /// but NodeJs/NPM could not be detected. 
    /// </summary>
    [AWSDeploymentExpectedException]
    public class MissingNodeJsException : Exception {}

    /// <summary>
    /// Throw if the user attempts to deploy a <see cref="RecipeDefinition"/>
    /// that requires <see cref="RecipeDefinition.DeploymentBundleTypes.Container"/>
    /// but Docker could not be detected. 
    /// </summary>
    [AWSDeploymentExpectedException]
    public class MissingDockerException : Exception {}

    /// <summary>
    /// Throw if Recommendation Engine is unable to generate
    /// recommendations for a given target context
    /// </summary>
    [AWSDeploymentExpectedException]
    public class FailedToGenerateAnyRecommendations : Exception {}

    /// <summary>
    /// Indicates a specific strongly typed Exception can be anticipated.
    /// Whoever throws this error should also present the user with helpful information
    /// on what wrong and how to fix it.  This is the preferred UX.
    /// <para />
    /// Conversely, if an Exceptions not marked with attribute reaches the entry point
    /// application (ie Program.cs) than all exception details will likely be presented to
    /// user.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AWSDeploymentExpectedExceptionAttribute : Attribute { }

    public static class ExceptionExtensions
    {
        /// <summary>
        /// True if the <paramref name="e"/> is decorated with
        /// <see cref="AWSDeploymentExpectedExceptionAttribute"/>.
        /// </summary>
        public static bool IsAWSDeploymentExpectedException(this Exception e) =>
            null != e?.GetType()
                .GetCustomAttribute(typeof(AWSDeploymentExpectedExceptionAttribute), inherit: true);

        public static string PrettyPrint(this Exception e)
        {
            if (null == e)
                return string.Empty;

            return $"{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}{PrettyPrint(e.InnerException)}";

        }
    }
}
