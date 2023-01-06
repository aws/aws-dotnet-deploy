**Recipe ID:** ConsoleAppEcsFargateService

**Recipe Description:** This .NET Console application will be built using a Dockerfile and deployed as a service to Amazon Elastic Container Service (Amazon ECS) with compute power managed by AWS Fargate compute engine. If your project does not contain a Dockerfile it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy a service as a container image on Linux.

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
* **ECS Service Name**
    * ID: ECSServiceName
    * Description: The name of the ECS service running in the cluster.
    * Type: String
* **Desired Task Count**
    * ID: DesiredCount
    * Description: The desired number of ECS tasks to run for the service.
    * Type: Int
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
* **Virtual Private Cloud (VPC)**
    * ID: Vpc
    * Description: A VPC enables you to launch the application into a virtual network that you've defined.
    * Type: Object
    * Settings:
        * **Use default VPC**
            * ID: IsDefault
            * Description: Do you want to use the default VPC for the deployment?
            * Type: Bool
        * **Create New VPC**
            * ID: CreateNew
            * Description: Do you want to create a new VPC?
            * Type: Bool
        * **Existing VPC ID**
            * ID: VpcId
            * Description: The ID of the existing VPC to use.
            * Type: String
* **ECS Service Security Groups**
    * ID: ECSServiceSecurityGroups
    * Description: A comma-delimited list of EC2 security groups to assign to the ECS service. This is commonly used to provide access to Amazon RDS databases running in their own security groups.
    * Type: String
* **Task CPU**
    * ID: TaskCpu
    * Description: The number of CPU units used by the task. See the following for details on CPU values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
    * Type: Int
* **Task Memory**
    * ID: TaskMemory
    * Description: The amount of memory (in MB) used by the task. See the following for details on memory values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
    * Type: Int
* **AutoScaling**
    * ID: AutoScaling
    * Description: The AutoScaling configuration for the ECS service.
    * Type: Object
    * Settings:
        * **Enable**
            * ID: Enabled
            * Description: Do you want to enable AutoScaling?
            * Type: Bool
        * **Minimum Capacity**
            * ID: MinCapacity
            * Description: The minimum number of ECS tasks handling the demand for the ECS service.
            * Type: Int
        * **Maximum Capacity**
            * ID: MaxCapacity
            * Description: The maximum number of ECS tasks handling the demand for the ECS service.
            * Type: Int
        * **AutoScaling Metric**
            * ID: ScalingType
            * Description: The metric to monitor for scaling changes.
            * Type: String
        * **CPU Target Utilization**
            * ID: CpuTypeTargetUtilizationPercent
            * Description: The target cpu utilization percentage that triggers a scaling change.
            * Type: Double
        * **Scale in cooldown (seconds)**
            * ID: CpuTypeScaleInCooldownSeconds
            * Description: The amount of time, in seconds, after a scale in activity completes before another scale in activity can start.
            * Type: Int
        * **Scale out cooldown (seconds)**
            * ID: CpuTypeScaleOutCooldownSeconds
            * Description: The amount of time, in seconds, after a scale out activity completes before another scale out activity can start.
            * Type: Int
        * **Memory Target Utilization**
            * ID: MemoryTypeTargetUtilizationPercent
            * Description: The target memory utilization percentage that triggers a scaling change.
            * Type: Double
        * **Scale in cooldown (seconds)**
            * ID: MemoryTypeScaleInCooldownSeconds
            * Description: The amount of time, in seconds, after a scale in activity completes before another scale in activity can start.
            * Type: Int
        * **Scale out cooldown (seconds)**
            * ID: MemoryTypeScaleOutCooldownSeconds
            * Description: The amount of time, in seconds, after a scale out activity completes before another scale out activity can start.
            * Type: Int
* **Environment Variables**
    * ID: ECSEnvironmentVariables
    * Description: Configure environment properties for your application.
    * Type: KeyValue
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
