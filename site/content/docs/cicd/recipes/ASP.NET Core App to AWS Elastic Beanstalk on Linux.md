**Recipe ID:** AspNetAppElasticBeanstalkLinux

**Recipe Description:** This ASP.NET Core application will be built and deployed to AWS Elastic Beanstalk on Linux. Recommended if you want to deploy your application directly to EC2 hosts, not as a container image.

**Settings:**

* **Elastic Beanstalk Application**
    * ID: BeanstalkApplication
    * Description: The Elastic Beanstalk application.
    * Type: Object
    * Settings:
        * **Create new Elastic Beanstalk application**
            * ID: CreateNew
            * Description: Do you want to create new application?
            * Type: Bool
        * **Application Name**
            * ID: ApplicationName
            * Description: The Elastic Beanstalk application name.
            * Type: String
        * **Application Name**
            * ID: ExistingApplicationName
            * Description: The Elastic Beanstalk application name.
            * Type: String
* **Environment Name**
    * ID: BeanstalkEnvironment
    * Description: The Elastic Beanstalk environment name.
    * Type: Object
    * Settings:
        * **Environment Name**
            * ID: EnvironmentName
            * Description: The Elastic Beanstalk environment name.
            * Type: String
* **EC2 Instance Type**
    * ID: InstanceType
    * Description: The EC2 instance type of the EC2 instances created for the environment.
    * Type: String
* **Environment Type**
    * ID: EnvironmentType
    * Description: The type of environment to create; for example, a single instance for development work or load balanced for production.
    * Type: String
* **Load Balancer Type**
    * ID: LoadBalancerType
    * Description: The type of load balancer for your environment.
    * Type: String
* **Load Balancer Scheme**
    * ID: LoadBalancerScheme
    * Description: Specify "Internal" if your application serves requests only from connected VPCs. "Public" load balancers serve requests from the Internet.
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
* **Service IAM Role**
    * ID: ServiceIAMRole
    * Description: A service role is the IAM role that Elastic Beanstalk assumes when calling other services on your behalf.
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
* **Key Pair**
    * ID: EC2KeyPair
    * Description: The EC2 key pair used to SSH into EC2 instances for the Elastic Beanstalk environment.
    * Type: String
* **Beanstalk Platform**
    * ID: ElasticBeanstalkPlatformArn
    * Description: The name of the Elastic Beanstalk platform to use with the environment.
    * Type: String
* **Managed Platform Updates**
    * ID: ElasticBeanstalkManagedPlatformUpdates
    * Description: Enable managed platform updates to apply platform updates automatically during a weekly maintenance window that you choose. Your application stays available during the update process.
    * Type: Object
    * Settings:
        * **Enable Managed Platform Updates**
            * ID: ManagedActionsEnabled
            * Description: Do you want to enable Managed Platform Updates?
            * Type: Bool
        * **Preferred Start Time**
            * ID: PreferredStartTime
            * Description: Configure a maintenance window for managed actions in UTC. Valid values are Day and time in the 'day:hour:minute' format. For example, 'Sun:00:00'.
            * Type: String
        * **Update Level**
            * ID: UpdateLevel
            * Description: The highest level of update to apply with managed platform updates. Platforms are versioned major.minor.patch. For example, 2.0.8 has a major version of 2, a minor version of 0, and a patch version of 8.
            * Type: String
* **Enable AWS X-Ray Tracing Support**
    * ID: XRayTracingSupportEnabled
    * Description: AWS X-Ray is a service that collects data about requests that your application serves, and provides tools you can use to view, filter, and gain insights into that data to identify issues and opportunities for optimization. Do you want to enable AWS X-Ray tracing support?
    * Type: Bool
* **Reverse Proxy**
    * ID: ReverseProxy
    * Description: By default Nginx is used as a reverse proxy in front of the .NET Core web server Kestrel. To use Kestrel as the front facing web server then select `none` as the reverse proxy.
    * Type: String
* **Enhanced Health Reporting**
    * ID: EnhancedHealthReporting
    * Description: Enhanced health reporting provides free real-time application and operating system monitoring of the instances and other resources in your environment.
    * Type: String
* **Health Check URL**
    * ID: HealthCheckURL
    * Description: Customize the load balancer health check to ensure that your application, and not just the web server, is in a good state.
    * Type: String
* **Rolling Updates**
    * ID: ElasticBeanstalkRollingUpdates
    * Description: When a configuration change requires replacing instances, Elastic Beanstalk can perform the update in batches to avoid downtime while the change is propagated. During a rolling update, capacity is only reduced by the size of a single batch, which you can configure. Elastic Beanstalk takes one batch of instances out of service, terminates them, and then launches a batch with the new configuration. After the new batch starts serving requests, Elastic Beanstalk moves on to the next batch.
    * Type: Object
    * Settings:
        * **Enable Rolling Updates**
            * ID: RollingUpdatesEnabled
            * Description: Do you want to enable Rolling Updates?
            * Type: Bool
        * **Rolling Update Type**
            * ID: RollingUpdateType
            * Description: This includes three types: time-based rolling updates, health-based rolling updates, and immutable updates. Time-based rolling updates apply a PauseTime between batches. Health-based rolling updates wait for new instances to pass health checks before moving on to the next batch. Immutable updates launch a full set of instances in a new Auto Scaling group.
            * Type: String
        * **Max Batch Size**
            * ID: MaxBatchSize
            * Description: The number of instances included in each batch of the rolling update.
            * Type: Int
        * **Min Instances In Service**
            * ID: MinInstancesInService
            * Description: The minimum number of instances that must be in service within the Auto Scaling group while other instances are terminated.
            * Type: Int
        * **Pause Time**
            * ID: PauseTime
            * Description: The amount of time (in seconds, minutes, or hours) the Elastic Beanstalk service waits after it completed updates to one batch of instances and before it continues on to the next batch. (ISO8601 duration format: PT#H#M#S where each # is the number of hours, minutes, and/or seconds, respectively.)
            * Type: String
        * **Timeout**
            * ID: Timeout
            * Description: The maximum amount of time (in minutes or hours) to wait for all instances in a batch of instances to pass health checks before canceling the update. (ISO8601 duration format: PT#H#M#S where each # is the number of hours, minutes, and/or seconds, respectively.)
            * Type: String
* **CName Prefix**
    * ID: CNamePrefix
    * Description: If specified, the environment attempts to use this value as the prefix for the CNAME in your Elastic Beanstalk environment URL. If not specified, the CNAME is generated automatically by appending a random alphanumeric string to the environment name.
    * Type: String
* **Environment Variables**
    * ID: ElasticBeanstalkEnvironmentVariables
    * Description: Configure environment properties for your application.
    * Type: KeyValue
* **Virtual Private Cloud (VPC)**
    * ID: VPC
    * Description: A VPC enables you to launch the application into a virtual network that you've defined
    * Type: Object
    * Settings:
        * **Use a VPC **
            * ID: UseVPC
            * Description: Do you want to use a Virtual Private Cloud (VPC)?
            * Type: Bool
        * **Create New VPC**
            * ID: CreateNew
            * Description: Do you want to create a new VPC?
            * Type: Bool
        * **VPC ID**
            * ID: VpcId
            * Description: A list of VPC IDs that Elastic Beanstalk should use when it associates your service with a custom Amazon VPC.
            * Type: String
        * **EC2 Instance Subnets**
            * ID: Subnets
            * Description: A list of IDs of subnets that Elastic Beanstalk should use when it associates your environment with a custom Amazon VPC. Specify IDs of subnets of a single Amazon VPC.
            * Type: List
        * **Security Groups**
            * ID: SecurityGroups
            * Description: Lists the Amazon EC2 security groups to assign to the EC2 instances in the Auto Scaling group to define firewall rules for the instances.
            * Type: List
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
