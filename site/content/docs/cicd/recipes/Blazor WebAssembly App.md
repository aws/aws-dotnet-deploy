**Recipe ID:** BlazorWasm

**Recipe Description:** This Blazor WebAssembly application will be built and hosted in a new Amazon Simple Storage Service (Amazon S3) bucket. The Blazor application will be exposed publicly through a CloudFront distribution using the Amazon S3 bucket as the origin.

**Settings:**

* **Index Document**
    * ID: IndexDocument
    * Description: The default page to use when the endpoint is accessed with no resource path.
    * Type: String
* **Error Document**
    * ID: ErrorDocument
    * Description: The error page to use when an error occurs while accessing the resource path.
    * Type: String
* **Redirect 404 and 403 Errors**
    * ID: Redirect404ToRoot
    * Description: Redirect any 404 and 403 requests to the index document. This is useful in Blazor applications that modify the resource path in the browser. If the modified resource path is reused in a new browser it will result in a 403 from Amazon CloudFront since no S3 object exists at that resource path.
    * Type: Bool
* **Backend REST API**
    * ID: BackendApi
    * Description: URI to a backend rest api that will be added as an origin to the CloudFront distribution. For example an API Gateway endpoint.
    * Type: Object
    * Settings:
        * **Enable**
            * ID: Enable
            * Description: Enable adding backend rest api
            * Type: Bool
        * **Uri**
            * ID: Uri
            * Description: Uri to the backend rest api
            * Type: String
        * **Resource Path Pattern**
            * ID: ResourcePathPattern
            * Description: The resource path pattern to determine which request go to backend rest api. (i.e. "/api/*") 
            * Type: String
* **CloudFront Access Logging**
    * ID: AccessLogging
    * Description: Configure if and how access logs are written for the CloudFront distribution
    * Type: Object
    * Settings:
        * **Enable**
            * ID: Enable
            * Description: Enable CloudFront Access Logging
            * Type: Bool
        * **Log Cookies**
            * ID: LogIncludesCookies
            * Description: Include cookies in access logs
            * Type: Bool
        * **Create Logging Bucket**
            * ID: CreateLoggingS3Bucket
            * Description: Create new S3 bucket for access logs to be stored. Bucket and logs will be retained after deployment is deleted.
            * Type: Bool
        * **Logging Bucket**
            * ID: ExistingS3LoggingBucket
            * Description: S3 bucket to use for storing access logs
            * Type: String
        * **Logging S3 Key Prefix**
            * ID: LoggingS3KeyPrefix
            * Description: Optional S3 key prefix to store access logs (e.g. app-name/)
            * Type: String
* **CloudFront Price Class**
    * ID: PriceClass
    * Description: Configure the edge locations that will respond to request for the CloudFront distribution
    * Type: String
* **Enable IPv6**
    * ID: EnableIpv6
    * Description: Control if IPv6 should be enabled for the CloudFront distribution
    * Type: Bool
* **Maximum HTTP Version**
    * ID: MaxHttpVersion
    * Description: The maximum http version that users can use to communicate with the CloudFront distribution
    * Type: String
* **Web ACL Arn**
    * ID: WebAclId
    * Description: The AWS WAF (web application firewall) ACL arn
    * Type: String
* **Environment Architecture**
    * ID: EnvironmentArchitecture
    * Description: The CPU architecture of the environment to create.
    * Type: String
* **Dotnet Build Configuration**
    * ID: DotnetBuildConfiguration
    * Description: The build configuration to use for the dotnet build
    * Type: String
* **Dotnet Publish Args**
    * ID: DotnetPublishArgs
    * Description: The list of additional dotnet publish args passed to the target application.
    * Type: String
* **Self Contained Build**
    * ID: SelfContainedBuild
    * Description: Publishing your app as self-contained produces an application that includes the .NET runtime and libraries. Users can run it on a machine that doesn't have the .NET runtime installed.
    * Type: Bool
