**Recipe ID:** ConsoleAppEcsFargateScheduleTask

**Recipe Description:** This .NET Console application will be built using a Dockerfile and deployed as a scheduled task to Amazon Elastic Container Service (Amazon ECS) with compute power managed by AWS Fargate compute engine. If your project does not contain a Dockerfile it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy a scheduled task as a container image on Linux.

**Settings:**

* **ECS Cluster**
    * ID: ECSCluster
    * Description: The ECS cluster used for the deployment.
    * Type: Object
    * Settings:
        * **Create New ECS Cluster**
            * ID: CreateNew
            * Description: Do you want to create a new ECS cluster?
            * Type: Bool
        * **Existing Cluster ARN**
            * ID: ClusterArn
            * Description: The ARN of the existing cluster to use.
            * Type: String
        * **New Cluster Name**
            * ID: NewClusterName
            * Description: The name of the new cluster to create.
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
* **Task Schedule**
    * ID: Schedule
    * Description: The schedule or rate (frequency) that determines when Amazon CloudWatch Events runs the rule. For details about the format for this value, see the CloudWatch Events guide: https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/ScheduledEvents.html
    * Type: String
* **Virtual Private Cloud (VPC)**
    * ID: Vpc
    * Description: A VPC enables you to launch the application into a virtual network that you've defined.
    * Type: Object
    * Settings:
        * **Use default VPC**
            * ID: IsDefault
            * Description: Do you want to use the default VPC?
            * Type: Bool
        * **Create New VPC**
            * ID: CreateNew
            * Description: Do you want to create a new VPC?
            * Type: Bool
        * **Existing VPC ID**
            * ID: VpcId
            * Description: The ID of the existing VPC to use.
            * Type: String
* **Task CPU**
    * ID: TaskCpu
    * Description: The number of CPU units used by the task. See the following for details on CPU values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
    * Type: Int
* **Task Memory**
    * ID: TaskMemory
    * Description: The amount of memory (in MB) used by the task. See the following for details on memory values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
    * Type: Int
* **Environment Variables**
    * ID: ECSEnvironmentVariables
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
