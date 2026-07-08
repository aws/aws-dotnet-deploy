**Recipe ID:** AspNetAppBedrockAgentCore

**Recipe Description:** [Experimental] This ASP.NET Core application will be built as a container image and deployed to Amazon Bedrock AgentCore Runtime, a fully managed service for hosting AI agent applications. The recipe provisions an AgentCore Runtime to run your agent and the required IAM roles. If your project does not contain a Dockerfile, it will be automatically generated. Recommended for ASP.NET Core projects that reference AWS.AgentCore.Hosting.

**Settings:**

* **Runtime Name**
    * ID: RuntimeName
    * Description: The name of the Bedrock AgentCore Runtime.
    * Type: String
* **Request Headers**
    * ID: RequestHeaders
    * Description: HTTP request header names to pass through to the agent container. Headers not in this list are stripped by the AgentCore Runtime.
    * Type: List
* **AgentCore Memory**
    * ID: AgentCoreMemory
    * Description: Configure AgentCore Memory for persistent conversation history. When enabled, a Memory resource is provisioned and wired to the agent via the AWS_AGENTCORE_MEMORY_ID environment variable.
    * Type: Object
    * Settings:
        * **Create New Memory**
            * ID: CreateNew
            * Description: Do you want to create a new AgentCore Memory resource?
            * Type: Bool
        * **Existing Memory ID**
            * ID: MemoryId
            * Description: The ID of an existing AgentCore Memory resource to use. The agent will read/write conversation history to this memory.
            * Type: String
* **Virtual Private Cloud (VPC)**
    * ID: VPC
    * Description: A VPC enables you to place the AgentCore Runtime into a virtual network for access to private resources. Not required for most deployments.
    * Type: Object
    * Settings:
        * **Use a VPC**
            * ID: UseVPC
            * Description: Do you want to place the AgentCore Runtime in a VPC?
            * Type: Bool
        * **Create New VPC**
            * ID: CreateNew
            * Description: Do you want to create a new VPC?
            * Type: Bool
        * **VPC ID**
            * ID: VpcId
            * Description: The ID of the VPC to use.
            * Type: String
        * **Security Groups**
            * ID: SecurityGroups
            * Description: A list of EC2 security groups to assign to the AgentCore Runtime. This is commonly used to provide access to resources like Amazon RDS or ElastiCache running in their own security groups.
            * Type: List
* **Runtime IAM Role**
    * ID: RuntimeIAMRole
    * Description: The Identity and Access Management (IAM) role that provides AWS credentials to the AgentCore Runtime. When creating a new role, it includes permissions for ECR pull, CloudWatch Logs, Bedrock model invocation (cross-region), and AgentCore Memory access.
    * Type: Object
    * Settings:
        * **Create New Role**
            * ID: CreateNew
            * Description: Do you want to create a new role?
            * Type: Bool
        * **Existing Role ARN**
            * ID: RoleArn
            * Description: The ARN of the existing IAM role to use for the AgentCore Runtime.
            * Type: String
* **Environment Variables**
    * ID: AgentCoreEnvironmentVariables
    * Description: Configure environment variables that will be set for the AgentCore Runtime container.
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
