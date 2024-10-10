**Recipe ID:** AspNetAppEcsFargate

**Recipe Description:** This ASP.NET Core application will be deployed to Amazon Elastic Container Service (Amazon ECS) with compute power managed by AWS Fargate compute engine. If your project does not contain a Dockerfile, it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy your application as a container image on Linux.

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
* **Container Port**
    * ID: Port
    * Description: The port the container is listening for requests on.
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
    * ID: AdditionalECSServiceSecurityGroups
    * Description: A list of EC2 security groups to assign to the ECS service. This is commonly used to provide access to Amazon RDS databases running in their own security groups.
    * Type: List
* **Task CPU**
    * ID: TaskCpu
    * Description: The number of CPU units used by the task. See the following for details on CPU values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
    * Type: Int
* **Task Memory**
    * ID: TaskMemory
    * Description: The amount of memory (in MB) used by the task. See the following for details on memory values: https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html#fargate-task-defs
    * Type: Int
* **Elastic Load Balancer**
    * ID: LoadBalancer
    * Description: Load Balancer the ECS Service will register tasks to.
    * Type: Object
    * Settings:
        * **Create New Load Balancer**
            * ID: CreateNew
            * Description: Do you want to create a new Load Balancer?
            * Type: Bool
        * **Existing Load Balancer ARN**
            * ID: ExistingLoadBalancerArn
            * Description: The ARN of an existing load balancer to use.
            * Type: String
        * **Deregistration delay (seconds)**
            * ID: DeregistrationDelayInSeconds
            * Description: The amount of time to allow requests to finish before deregistering ECS tasks.
            * Type: Int
        * **Health Check Path**
            * ID: HealthCheckPath
            * Description: The ping path destination where Elastic Load Balancing sends health check requests.
            * Type: String
        * **Health Check Timeout**
            * ID: HealthCheckTimeout
            * Description: The amount of time, in seconds, during which no response from a target means a failed health check.
            * Type: Int
        * **Health Check Interval**
            * ID: HealthCheckInternval
            * Description: The approximate interval, in seconds, between health checks of an individual instance.
            * Type: Int
        * **Healthy Threshold Count**
            * ID: HealthyThresholdCount
            * Description: The number of consecutive health check successes required before considering an unhealthy target healthy.
            * Type: Int
        * **Unhealthy Threshold Count**
            * ID: UnhealthyThresholdCount
            * Description: The number of consecutive health check successes required before considering an unhealthy target unhealthy.
            * Type: Int
        * **Type of Listener Condition**
            * ID: ListenerConditionType
            * Description: The type of listener rule to create to direct traffic to ECS service.
            * Type: String
        * **Listener Condition Path Pattern**
            * ID: ListenerConditionPathPattern
            * Description: The resource path pattern to use for the listener rule. (i.e. "/api/*") 
            * Type: String
        * **Listener Condition Priority**
            * ID: ListenerConditionPriority
            * Description: Priority of the condition rule. The value must be unique for the Load Balancer listener.
            * Type: Int
        * **Internet-Facing**
            * ID: InternetFacing
            * Description: Should the load balancer have an internet-routable address? Internet-facing load balancers can route requests from clients over the internet. Internal load balancers can route requests only from clients with access to the VPC.
            * Type: Bool
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
        * **Request per task**
            * ID: RequestTypeRequestsPerTarget
            * Description: The number of request per ECS task that triggers a scaling change.
            * Type: Int
        * **Scale in cooldown (seconds)**
            * ID: RequestTypeScaleInCooldownSeconds
            * Description: The amount of time, in seconds, after a scale in activity completes before another scale in activity can start.
            * Type: Int
        * **Scale out cooldown (seconds)**
            * ID: RequestTypeScaleOutCooldownSeconds
            * Description: The amount of time, in seconds, after a scale out activity completes before another scale out activity can start.
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
