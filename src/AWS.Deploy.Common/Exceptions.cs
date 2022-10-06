// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common
{
    public abstract class DeployToolException : Exception
    {
        public DeployToolErrorCode ErrorCode { get; set; }
        public int? ProcessExitCode { get; set; }

        public DeployToolException(DeployToolErrorCode errorCode, string message, Exception? innerException = null, int? processExitCode = null) : base(message, innerException)
        {
            ErrorCode = errorCode;
            ProcessExitCode = processExitCode;
        }
    }

    public enum DeployToolErrorCode
    {
        ProjectPathNotFound = 10000100,
        ProjectParserNoSdkAttribute = 10000200,
        InvalidCliArguments = 10000300,
        SilentArgumentNeedsApplicationNameArgument = 10000400,
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
        UnexpectedError = 10007100,
        FailedToCreateCdkStack = 10007200,
        FailedToFindElasticBeanstalkSolutionStack = 10007300,
        FailedToCreateDeploymentCommandInstance = 10007400,
        FailedToFindElasticBeanstalkApplication = 10007500,
        FailedS3Upload = 10007600,
        FailedToCreateElasticBeanstalkApplicationVersion = 10007700,
        FailedToUpdateElasticBeanstalkEnvironment = 10007800,
        FailedToCreateElasticBeanstalkStorageLocation = 10007900,
        UnableToAccessAWSRegion = 10008000,
        OptInRegionDisabled = 10008100,
        ECRRepositoryPromptForNameReturnedNull = 10008200,
        FailedToFindCloudApplicationResourceType = 10008300,
        ECRRepositoryDoesNotExist = 10008400,
        FailedToDeserializeRecipe = 10008500,
        FailedToDeserializeDeploymentBundle = 10008600,
        FailedToDeserializeDeploymentProjectRecipe = 10008700,
        FailedToRunCDKBootstrap = 10008800,
        FailedToGetCredentialsForProfile = 10008900,
        FailedToRunCDKDiff = 10009000,
        FailedToCreateCDKProject = 10009100,
        ResourceQuery = 10009200,
        FailedToRetrieveStackId = 10009300,
        FailedToGetECRAuthorizationToken = 10009400,
        InvalidCloudApplicationName = 10009500,
        SelectedValueIsNotAllowed = 10009600,
        MissingValidatorConfiguration = 10009700,
        InvalidFilePath = 10009800,
        InvalidDeployToolWorkspace = 10009900,
        InvalidDeploymentManifestModel = 10010000,
        FailedToCreateDeepCopy = 10010100,
        FailedToGetOptionSettingValue = 10010200,
        ECRRepositoryNameIsNull = 10010300,
        FailedToReadCdkBootstrapVersion = 10010400,
        UnsupportedOptionSettingType = 10010500,
        FailedToSaveDeploymentSettings = 10010600,
        InvalidWindowsManifestFile = 10010700
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
        public FailedToCreateDeploymentBundleException(DeployToolErrorCode errorCode, string message, int? processExitCode, Exception? innerException = null) : base(errorCode, message, innerException, processExitCode) { }
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
    /// Thrown if <see cref="OptionSettingItem.SetValue"/> is setting a value with an incompatible type.
    /// </summary>
    public class UnsupportedOptionSettingType : DeployToolException
    {
        public UnsupportedOptionSettingType(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Thrown if Option Setting Item Validator has missing or invalid configuration.
    /// </summary>
    public class MissingValidatorConfigurationException : DeployToolException
    {
        public MissingValidatorConfigurationException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if Project Path contains an invalid path
    /// </summary>
    public class InvalidProjectPathException : DeployToolException
    {
        public InvalidProjectPathException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if an invalid <see cref="DeploymentSettings"/> is used.
    /// </summary>
    public class InvalidDeploymentSettingsException : DeployToolException
    {
        public InvalidDeploymentSettingsException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception is thrown if we cannot retrieve deployment bundle definitions
    /// </summary>
    public class NoDeploymentBundleDefinitionsFoundException : DeployToolException
    {
        public NoDeploymentBundleDefinitionsFoundException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if a failure occured while trying to update the deployment manifest file.
    /// </summary>
    public class FailedToUpdateDeploymentManifestFileException : DeployToolException
    {
        public FailedToUpdateDeploymentManifestFileException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if a failure occured while trying to deserialize a file.
    /// </summary>
    public class FailedToDeserializeException : DeployToolException
    {
        public FailedToDeserializeException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Exception thrown if a failure occured while querying resources in AWS. It must be used in conjunction with <see cref="DeployToolErrorCode.ResourceQuery"/>.
    /// </summary>
    public class ResourceQueryException : DeployToolException
    {
        public ResourceQueryException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Thrown when an invalid file path is specified as an <see cref="OptionSettingItem"/> value
    /// </summary>
    public class InvalidFilePath : DeployToolException
    {
        public InvalidFilePath(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
    }

    /// <summary>
    /// Throw if an error occurred while saving the deployment settings to a config file
    /// </summary>
    public class FailedToSaveDeploymentSettingsException : DeployToolException
    {
        public FailedToSaveDeploymentSettingsException(DeployToolErrorCode errorCode, string message, Exception? innerException = null) : base(errorCode, message, innerException) { }
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

        /// <summary>
        /// Returns the truncated error message by only preserving the leading and trailing k characters.
        /// The message will only be truncated if its length is greater than 2*k.
        /// </summary>
        /// <param name="numChars">Species the number of leading and trailing characters to preserve.</param>
        /// <returns>The truncated error message or <see cref="string.Empty"/> if the error message is null or empty.</returns>
        public static string GetTruncatedErrorMessage(this Exception? e, int numChars = 500)
        {
            var message = e?.Message;

            if (string.IsNullOrEmpty(message))
                return string.Empty;

            if (message.Length <= 2*numChars)
                return message;

            var firstKChars = message.Substring(0, numChars);
            var lastkChars = message.Substring(message.Length - numChars);
            var seperator = $"{Environment.NewLine}...{Environment.NewLine}Error truncated to the first and last {numChars} characters{Environment.NewLine}...{Environment.NewLine}";
            return $"{firstKChars}{seperator}{lastkChars}";
        }
    }
}
