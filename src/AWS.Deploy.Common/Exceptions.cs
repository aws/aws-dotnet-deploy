// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;

namespace AWS.Deploy.Common
{
    public abstract class DeployToolException : Exception
    {
        public DeployToolErrorCode ErrorCode { get; set; }

        public DeployToolException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public enum DeployToolErrorCode
    {
        ProjectPathNotFound = 10000100,
        ProjectParserNoSdkAttribute = 10000200,
        InvalidCliArguments = 10000300,
        SilentArgumentNeedsStackNameArgument = 10000400,
        SilentArgumentNeedsDeploymentRecipe = 10000500,
        DeploymentProjectPathNotFound = 10000600,
        RuleHasInvalidTestType = 10000700,
        MissingSystemCapabilities = 10000800,
        NoDeploymentRecipesFound = 10000900,
        NoCompatibleDeploymentRecipesFound = 10001000,
        DeploymentProjectNotSupported = 10001100,
        InvalidValueForOptionSettingItem = 10001200,
        InvalidDockerBuildArgs = 10001300,
        InvalidDockerExecutionDirectory = 10001400,
        InvalidDotnetPublishArgs = 10001500,
        ErrorParsingApplicationMetadata = 10001600,
        FailedToCreateContainerDeploymentBundle = 10001700,
        FailedToCreateDotnetPublishDeploymentBundle = 10001800,
        OptionSettingItemDoesNotExistInRecipe = 10001900,
        UnableToCreateValidatorInstance = 10002000,
        OptionSettingItemValueValidationFailed = 10002100,
        StackCreatedFromDifferentDeploymentRecommendation = 10002200,
        DeploymentConfigurationNeedsAdjusting = 10002300,
        UserDeploymentInvalidStackName = 10002400,
        InvalidPropertyValueForUserDeployment = 10002500,
        FailedToDeserializeUserDeploymentFile = 10002600,
        DeploymentBundleDefinitionNotFound = 10002700,
        DeploymentManifestUpdateFailed = 10002800,
        DockerFileTemplateNotFound = 10002900,
        UnableToMapProjectToDockerImage = 10003000,
        NoValidDockerImageForProject = 10003100,
        NoValidDockerMappingForSdkType = 10003200,
        NoValidDockerMappingForTargetFramework = 10003300,
        FailedToGenerateCDKProjectFromTemplate = 10003400,
        FailedToInstallProjectTemplates = 10003500,
        FailedToWritePackageJsonFile = 10003600,
        FailedToInstallNpmPackages = 10003700,
        DockerBuildFailed = 10003800,
        FailedToGetCDKVersion = 10003900,
        DockerLoginFailed = 10004000,
        DockerTagFailed = 10004100,
        DockerPushFailed = 10004200,
        FailedToFindRecipeDefinitions = 10004300,
        DotnetPublishFailed = 10004400,
        FailedToFindZipUtility = 10004500,
        ZipUtilityFailedToZip = 10004600,
        FailedToGenerateDockerFile = 10004700,
        BaseTemplatesInvalidPath = 10004800,
        InvalidSolutionPath = 10004900,
        InvalidAWSDeployRecipesCDKCommonVersion = 10005000,
        FailedToDeployCdkApplication = 10005100,
        AppRunnerServiceDoesNotExist = 10005200,
        BeanstalkEnvironmentDoesNotExist = 10005300,
        LoadBalancerDoesNotExist = 10005400,
        LoadBalancerListenerDoesNotExist = 10005500,
        CloudWatchRuleDoesNotExist = 10005600,
        InvalidLocalUserSettingsFile = 10005700,
        FailedToUpdateLocalUserSettingsFile = 10005800,
        FailedToCheckDockerInfo = 10005900,
        UnableToResolveAWSCredentials = 10006000,
        UnableToCreateAWSCredentials = 10006100,
        FailedToDeleteStack = 10006200,
        FailedToFindDeployableTarget = 10006300,
        BeanstalkAppPromptForNameReturnedNull = 10006400,
        BeanstalkEnvPromptForNameReturnedNull = 10006500,
        EC2KeyPairPromptForNameReturnedNull = 10006600,
        TcpPortInUse = 10006700,
        CompatibleRecommendationForRedeploymentNotFound = 10006800,
        InvalidSaveDirectoryForCdkProject = 10006900,
        FailedToFindDeploymentProjectRecipeId = 10007000,
        UnexpectedError = 10007100
    }

    public class ProjectFileNotFoundException : DeployToolException
    {
        public ProjectFileNotFoundException(DeployToolErrorCode errorCode, string message, Exception? innerException = null)
            : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Common exception if the user has passed invalid input from the command line
    /// </summary>
    public class InvalidCliArgumentException : DeployToolException
    {
        public InvalidCliArgumentException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if the user attempts to deploy a <see cref="RecipeDefinition"/> but the recipe definition is invalid
    /// </summary>
    public class InvalidRecipeDefinitionException : DeployToolException
    {
        public InvalidRecipeDefinitionException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if the user attempts to deploy a <see cref="ProjectDefinition"/> but the project definition is invalid
    /// </summary>
    public class InvalidProjectDefinitionException : DeployToolException
    {
        public InvalidProjectDefinitionException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Thrown if there is a missing System Capability.
    /// </summary>
    public class MissingSystemCapabilityException : DeployToolException
    {
        public MissingSystemCapabilityException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if Recommendation Engine is unable to generate
    /// recommendations for a given target context
    /// </summary>
    public class FailedToGenerateAnyRecommendations : DeployToolException
    {
        public FailedToGenerateAnyRecommendations(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if a value is set that is not part of the allowed values
    /// of an option setting item
    /// </summary>
    public class InvalidOverrideValueException : DeployToolException
    {
        public InvalidOverrideValueException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if there is a parse error reading the existing Cloud Application's metadata
    /// </summary>
    public class ParsingExistingCloudApplicationMetadataException : DeployToolException
    {
        public ParsingExistingCloudApplicationMetadataException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if Orchestrator is unable to create
    /// the deployment bundle.
    /// </summary>
    public class FailedToCreateDeploymentBundleException : DeployToolException
    {
        public FailedToCreateDeploymentBundleException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if Option Setting Item does not exist
    /// </summary>
    public class OptionSettingItemDoesNotExistException : DeployToolException
    {
        public OptionSettingItemDoesNotExistException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    public class InvalidValidatorTypeException : DeployToolException
    {
        public InvalidValidatorTypeException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Thrown if <see cref="OptionSettingItem.SetValueOverride"/> is given an invalid value.
    /// </summary>
    public class ValidationFailedException : DeployToolException
    {
        public ValidationFailedException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if Project Path contains an invalid path
    /// </summary>
    public class InvalidProjectPathException : DeployToolException
    {
        public InvalidProjectPathException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// Throw if an invalid <see cref="UserDeploymentSettings"/> is used.
    /// </summary>
    public class InvalidUserDeploymentSettingsException : DeployToolException
    {
        public InvalidUserDeploymentSettingsException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if we cannot retrieve deployment bundle definitions
    /// </summary>
    public class NoDeploymentBundleDefinitionsFoundException : DeployToolException
    {
        public NoDeploymentBundleDefinitionsFoundException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }
    
    /// Exception thrown if a failure occured while trying to update the deployment manifest file.
    /// </summary>
    public class FailedToUpdateDeploymentManifestFileException : DeployToolException
    {
        public FailedToUpdateDeploymentManifestFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    public static class ExceptionExtensions
    {
        /// <summary>
        /// True if the <paramref name="e"/> inherits from
        /// <see cref="DeployToolException"/>.
        /// </summary>
        public static bool IsAWSDeploymentExpectedException(this Exception e) =>
            e is DeployToolException;

        public static string PrettyPrint(this Exception? e)
        {
            if (null == e)
                return string.Empty;

            return $"{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}{PrettyPrint(e.InnerException)}";
        }
    }
}
