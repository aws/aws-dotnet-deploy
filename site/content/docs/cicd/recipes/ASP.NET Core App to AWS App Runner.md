**Recipe ID:** AspNetAppAppRunner

**Recipe Description:** This ASP.NET Core application will be built as a container image on Linux and deployed to AWS App Runner, a fully managed service for web applications and APIs. If your project does not contain a Dockerfile, it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy your web application as a Linux container image on a fully managed environment.

**Settings:**

* **Service Name**
    * ID: ServiceName
    * Description: The name of the AWS App Runner service.
    * Type: String
* **Container Port**
    * ID: Port
    * Description: The port the container is listening for requests on.
    * Type: Int
* **Start Command**
    * ID: StartCommand
    * Description: Override the start command from the image's default start command.
    * Type: String
* **Application IAM Role**
    * ID: ApplicationIAMRole
    * Description: The Identity and Access Management (IAM) role that provides AWS credentials to the application to access AWS services.
    * Type: Object
    * Settings:
        * **Create New Role**
            * ID: CreateNew
            * Description: Do you want to create a new role?
            * Type: Bool
        * **Existing Role ARN**
            * ID: RoleArn
            * Description: The ARN of the existing role to use.
            * Type: String
* **Service Access IAM Role**
    * ID: ServiceAccessIAMRole
    * Description: The Identity and Access Management (IAM) role that provides gives the AWS App Runner service access to pull the container image from ECR.
    * Type: Object
    * Settings:
        * **Create New Role**
            * ID: CreateNew
            * Description: Do you want to create a new role?
            * Type: Bool
        * **Existing Role ARN**
            * ID: RoleArn
            * Description: The ARN of the existing role to use.
            * Type: String
* **CPU**
    * ID: Cpu
    * Description: The number of CPU units reserved for each instance of your App Runner service.
    * Type: String
* **Memory**
    * ID: Memory
    * Description: The amount of memory reserved for each instance of your App Runner service.
    * Type: String
* **Encryption KMS Key**
    * ID: EncryptionKmsKey
    * Description: The ARN of the KMS key that's used for encryption of application logs.
    * Type: String
* **Health Check Protocol**
    * ID: HealthCheckProtocol
    * Description: The IP protocol that App Runner uses to perform health checks for your service.
    * Type: String
* **Health Check Path**
    * ID: HealthCheckPath
    * Description: The URL that health check requests are sent to.
    * Type: String
* **Health Check Interval**
    * ID: HealthCheckInterval
    * Description: The time interval, in seconds, between health checks.
    * Type: Int
* **Health Check Timeout**
    * ID: HealthCheckTimeout
    * Description: The time, in seconds, to wait for a health check response before deciding it failed.
    * Type: Int
* **Health Check Healthy Threshold**
    * ID: HealthCheckHealthyThreshold
    * Description: The number of consecutive checks that must succeed before App Runner decides that the service is healthy.
    * Type: Int
* **Health Check Unhealthy Threshold**
    * ID: HealthCheckUnhealthyThreshold
    * Description: The number of consecutive checks that must fail before App Runner decides that the service is unhealthy.
    * Type: Int
* **VPC Connector**
    * ID: VPCConnector
    * Description: App Runner requires this resource when you want to associate your App Runner service to a custom Amazon Virtual Private Cloud (Amazon VPC).
    * Type: Object
    * Settings:
        * **Use VPC Connector**
            * ID: UseVPCConnector
            * Description: Do you want to use a VPC Connector to connect to a VPC?
            * Type: Bool
        * **Create New VPC Connector**
            * ID: CreateNew
            * Description: Do you want to create a new VPC Connector?
            * Type: Bool
        * **Existing VPC Connector ID**
            * ID: VpcConnectorId
            * Description: The ID of the existing VPC Connector to use.
            * Type: String
        * **Create New VPC**
            * ID: CreateNewVpc
            * Description: Do you want to create a new VPC to use for the VPC Connector?
            * Type: Bool
        * **VPC ID**
            * ID: VpcId
            * Description: A list of VPC IDs that App Runner should use when it associates your service with a custom Amazon VPC.
            * Type: String
        * **Subnets**
            * ID: Subnets
            * Description: A list of IDs of subnets that App Runner should use when it associates your service with a custom Amazon VPC. Specify IDs of subnets of a single Amazon VPC. App Runner determines the Amazon VPC from the subnets you specify.
            * Type: List
        * **Security Groups**
            * ID: SecurityGroups
            * Description: A list of IDs of security groups that App Runner should use for access to AWS resources under the specified subnets. If not specified, App Runner uses the default security group of the Amazon VPC. The default security group allows all outbound traffic.
            * Type: List
* **Environment Variables**
    * ID: AppRunnerEnvironmentVariables
    * Description: Configure environment properties for your application.
    * Type: KeyValue
* **Environment Architecture**
    * ID: EnvironmentArchitecture
    * Description: The CPU architecture of the environment to create.
    * Type: String
* **Docker Build Args**
    * ID: DockerBuildArgs
    * Description: The list of additional options to append to the `docker build` command.
    * Type: String
* **Dockerfile Path**
    * ID: DockerfilePath
    * Description: Specify a path to a Dockerfile as either an absolute path or a path relative to the project.
    * Type: String
* **Docker Execution Directory**
    * ID: DockerExecutionDirectory
    * Description: Specifies the docker execution directory where the docker build command will be executed from.
    * Type: String
* **ECR Repository Name**
    * ID: ECRRepositoryName
    * Description: Specifies the ECR repository where the Docker images will be stored
    * Type: String
