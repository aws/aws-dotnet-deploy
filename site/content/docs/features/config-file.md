# Working with JSON configuration files

When you run the `deploy` command of the AWS .NET deployment tool, you can select deployment options in response to prompts from the tool.

Alternatively, you can provide a JSON configuration file by using the `--apply` option of the command. The tool reads deployment options from the JSON parameters that are specified in the file, and uses those options in its prompts. It uses default values for any parameters that aren't specified.

If you use a JSON configuration file in conjunction with the `-s` (`--silent`) option, you can specify deployment options without being prompted by the deployment tool at all.

This section documents the JSON definitions and syntax that you use to construct a configuration file.

## Common JSON parameters

The parameters you can include in a JSON configuration file depend on the type of deployment you're doing, as shown later in this topic. The following parameters are common to all deployment types.

_**AWSProfile**_  
The name of the AWS profile to use if not using the `[default]` profile.

_**AWSRegion**_  
The name of the AWS Region to use if not using the region from the `[default]` profile.

_**StackName**_  
The name of the AWS CloudFormation stack to use for your application. It can be the name of an existing stack or the name of a new stack to create.

_**RecipeId**_  
A value that identifies the type of deployment you want to perform. See the individual JSON syntax sections below for more information.

## JSON syntax for Amazon ECS deployments

Use this JSON definition to deploy your application to [Amazon Elastic Container Service (Amazon ECS)](https://docs.aws.amazon.com/ecs) with compute power managed by AWS Fargate compute engine. If your project doesn't contain a Dockerfile, it will be automatically generated.

This definition covers the following deployment types, which are provided in the `RecipeId` parameter:

* `AspNetAppEcsFargate`: An ASP.NET Core application. Recommended if you want to deploy your application as a container image on Linux.
* `ConsoleAppEcsFargateService`: A .NET console application built using a Docker file and deployed as a long running service app. Recommended if you want to deploy a service as a container image on Linux.
* `ConsoleAppEcsFargateScheduleTask`: A .NET console application built using a Docker file and deployed as a scheduled task. Recommended if you want to deploy a scheduled task as a container image on Linux.


```
{
    "AWSProfile": "string",
    "AWSRegion": "string",
    "StackName": "string",
    "RecipeId": "AspNetAppEcsFargate" | "ConsoleAppEcsFargateService" | "ConsoleAppEcsFargateScheduleTask",
    "OptionSettingsConfig":
    {
        "ECSCluster":
        {
            "CreateNew": boolean,
            "NewClusterName": "string",
            "ClusterArn": "string"
        },
        "ApplicationIAMRole":
        {
            "CreateNew": boolean,
            "RoleArn": "string"
        },
        "Vpc":
        {
            "IsDefault": boolean,
            "CreateNew": boolean,
            "VpcId": "string"
        },
        "TaskCpu": integer,
        "TaskMemory": integer,
        "ECSEnvironmentVariables": {
            "key1": "value1",
            "key2": "value2"
        },
        "DockerExecutionDirectory": "string",

        // For "AspNetAppEcsFargate" and "ConsoleAppEcsFargateService" recipes...
        "ECSServiceName": "string",
        "DesiredCount": integer,
        "AutoScaling":
        {
            "Enabled": boolean,
            "MinCapacity": integer,
            "MaxCapacity": integer
            "ScalingType": "string",
            "CpuTypeTargetUtilizationPercent": integer,
            "CpuTypeScaleInCooldownSeconds": integer,
            "CpuTypeScaleOutCooldownSeconds": integer,
            "MemoryTypeTargetUtilizationPercent": integer,
            "MemoryTypeScaleInCooldownSeconds": integer,
            "MemoryTypeScaleOutCooldownSeconds": integer,
            "RequestTypeRequestsPerTarget": integer,
            "RequestTypeScaleInCooldownSeconds": integer,
            "RequestTypeScaleOutCooldownSeconds": integer
        },

        // For "AspNetAppEcsFargate" recipes...
        "LoadBalancer":
        {
            "CreateNew": boolean,
            "ExistingLoadBalancerArn": "string",
            "DeregistrationDelayInSeconds": integer,
            "HealthCheckPath": "string",
            "HealthCheckInternval": integer,
            "HealthyThresholdCount": integer,
            "UnhealthyThresholdCount": integer,
            "ListenerConditionType": "string",
            "ListenerConditionPathPattern": "string",
            "ListenerConditionPriority": integer
        },
        "AdditionalECSServiceSecurityGroups": "string",

        // For "ConsoleAppEcsFargateService" recipes...
        "ECSServiceSecurityGroups": "string",

        // For "ConsoleAppEcsFargateScheduleTask" recipes...
        "Schedule": "string"
    }
}
```

The following parameter definitions are specific to the JSON syntax for an Amazon ECS deployment. Also see [Common JSON parameters](#common-json-parameters).

### Parameters for all three ECS recipes
---
_**ECSCluster**_  
The ECS cluster to use for your deployment. It can be a new cluster (the default) or an existing one.

- If this parameter isn't present, a new cluster is created with the same name as the AWS CloudFormation stack that will be used for your application. If you want to give the new cluster a different name, provide the name in `NewClusterName`.

- If you're using an existing cluster, set `CreateNew` to `false` and include the cluster's ARN in `ClusterArn`.

_**ApplicationIAMRole**_  
The IAM role that provides AWS credentials to the application to access AWS services. You can create a new role (the default) or use an existing role. To use an existing role, set `CreateNew` to `false` and include the role's ARN in `RoleArn`.

_**Vpc**_  
The Amazon Virtual Private Cloud (VPC) in which to launch the application. It can be the Default VPC (the default behavior), a new VPC, or a VPC that you've already created. To create a new VPC, set `IsDefault` to `false` and `CreateNew` to `true`. To use an existing VPC, set `IsDefault` to `false` and include the VPC ID in `VpcId`.

_**TaskCpu**_  
The number of vCPU units used by the task. The following are Valid values:  
-- "256" (the default): .25 vCPU  
-- "512": .5 vCPU  
-- "1024": 1 vCPU  
-- "2048": 2 vCPU  
-- "4096": 4 vCPU  
For more information, see [Fargate task definition considerations](https://docs.aws.amazon.com/AmazonECS/latest/userguide/fargate-task-defs.html) in the [Amazon ECS User Guide for AWS Fargate](https://docs.aws.amazon.com/AmazonECS/latest/userguide/), specifically, **Task CPU and memory**.

_**TaskMemory**_  
The amount of memory (in MB) used by the task. Valid values are 512 (the default), 1024, 2048, 3072, 4096, 5120, 6144, 7168, 8192, 9216, 10240, 11264, 12288, 13312, 14336, 15360, 16384, 17408, 18432, 19456, 20480, 21504, 22528, 23552, 24576, 25600, 26624, 27648, 28672, 29696, and 30720. For more information, see [Fargate task definition considerations](https://docs.aws.amazon.com/AmazonECS/latest/userguide/fargate-task-defs.html) in the [Amazon ECS User Guide for AWS Fargate](https://docs.aws.amazon.com/AmazonECS/latest/userguide/), specifically, **Task CPU and memory**.

_**ECSEnvironmentVariables**_  
Environment properties for your application. Include the properties as key/value pairs of strings.

_**DockerExecutionDirectory**_  
If you're using Docker, the path to the Docker execution environment, formatted as a string that's properly escaped for your operating system (for example on Windows: "C:\\codebase").

### Parameters for the `AspNetAppEcsFargate` and `ConsoleAppEcsFargateService` recipes
---
_**ECSServiceName**_  
The name of the ECS service running in the cluster. If this parameter isn't present, the service will be named "&lt;StackName&gt;-service".

_**DesiredCount**_  
The number of ECS tasks you want to run for the service. The valid range of values is 1 through 5000. The default is 3 for the `AspNetAppEcsFargate` recipe and 1 for the `ConsoleAppEcsFargateService` recipe.

_**AutoScaling**_  
Parameters for configuring automatic scaling in the ECS service. For more information, see [Service auto scaling](https://docs.aws.amazon.com/AmazonECS/latest/userguide/service-auto-scaling.html) in the [Amazon ECS User Guide for AWS Fargate](https://docs.aws.amazon.com/AmazonECS/latest/userguide/).

By default, auto scaling is disabled. To enable auto scaling, set `Enabled` to `true` and use the other parameters of this object (`MinCapacity`, etc.) to configure auto scaling. These parameters are described as follows:

* `MinCapacity`  
The minimum number of ECS tasks that will handle the demand for the ECS service. The valid range of values is 1 through 5000, and the default is 3.

* `MaxCapacity`  
The maximum number of ECS tasks that will handle the demand for the ECS service. The valid range of values is 1 through 5000, and the default is 6.

* `ScalingType`  
The metric to monitor to determine whether scaling changes are required. The metrics that can be monitored are "Cpu" (the default), "Memory", and "Request" (for `AspNetAppEcsFargate` only). Each of these metrics has additional configuration parameters as shown next.

* `CpuTypeTargetUtilizationPercent`  
For the "Cpu" metric, the target CPU utilization percentage that triggers a scaling change. The valid range of values is 1 through 100, and the default is 70.

* `CpuTypeScaleInCooldownSeconds`  
For the "Cpu" metric, the amount of time, in seconds, after a scale-in activity completes before another scale-in activity can start. The valid range of values is 0 through 3600, and the default is 300.

* `CpuTypeScaleOutCooldownSeconds`  
For the "Cpu" metric, the amount of time, in seconds, after a scale-out activity completes before another scale-out activity can start. The valid range of values is 0 through 3600, and the default is 300.

* `MemoryTypeTargetUtilizationPercent`  
For the "Memory" metric, the target memory utilization percentage that triggers a scaling change. The valid range of values is 1 through 100, and the default is 70.

* `MemoryTypeScaleInCooldownSeconds`  
For the "Memory" metric, the amount of time, in seconds, after a scale-in activity completes before another scale-in activity can start. The valid range of values is 0 through 3600, and the default is 300.

* `MemoryTypeScaleOutCooldownSeconds`  
For the "Memory" metric, the amount of time, in seconds, after a scale-out activity completes before another scale-out activity can start. The valid range of values is 0 through 3600, and the default is 300.

* `RequestTypeRequestsPerTarget`  
This parameter is available only for the `AspNetAppEcsFargate` recipe.  
For the "Request" metric, the number of requests per ECS task that triggers a scaling change. The minimum value you can specify is 1, and the default is 1000.

* `RequestTypeScaleInCooldownSeconds`  
This parameter is available only for the `AspNetAppEcsFargate` recipe.  
For the "Request" metric, the amount of time, in seconds, after a scale-in activity completes before another scale-in activity can start. The valid range of values is 0 through 3600, and the default is 300.

* `RequestTypeScaleOutCooldownSeconds`  
This parameter is available only for the `AspNetAppEcsFargate` recipe.  
For the "Request" metric, the amount of time, in seconds, after a scale-out activity completes before another scale-out activity can start. The valid range of values is 0 through 3600, and the default is 300.

### Parameters for the `AspNetAppEcsFargate` recipe only
---
_**LoadBalancer**_  
The load balancer that the ECS Service will register tasks to. For more information about load balancers, see [Service load balancing](https://docs.aws.amazon.com/AmazonECS/latest/userguide/service-load-balancing.html) and [Application Load Balancer](https://docs.aws.amazon.com/AmazonECS/latest/userguide/load-balancer-types.html#alb) in the [Amazon ECS User Guide for AWS Fargate](https://docs.aws.amazon.com/AmazonECS/latest/userguide/).

You can create a new load Balancer (the default) or use an existing one. To use an existing load Balancer, set `CreateNew` to `false` and include the load Balancer's ARN in `ExistingLoadBalancerArn`. Use the other parameters of this object (`DeregistrationDelayInSeconds`, etc.) to configure the load balancer. These parameters are described as follows:

* `DeregistrationDelayInSeconds`  
The amount of time to allow for requests to finish before deregistering a load balancer target. The valid range of values is 0 through 3600, and the default is 60.

* `HealthCheckPath`  
The ping-path destination where Elastic Load Balancing sends health check requests. The default is "/".

* `HealthCheckInternval`  
The approximate interval, in seconds, between health checks of an individual instance. The valid range of values is 5 through 300, and the default is 30.

* `HealthyThresholdCount`  
The number of consecutive successful health checks required before considering an unhealthy target healthy. If health-check successes consecutively exceed `HealthyThresholdCount`, the load balancer puts the target back in service. The valid range of values is 2 through 10, and the default is 5.

* `UnhealthyThresholdCount`  
The number of consecutive failed health checks required before considering a healthy target unhealthy. If health-check failures consecutively exceed `UnhealthyThresholdCount`, the load balancer takes the target out
of service. The valid range of values id 2 through 10, and the default is 2.

* `ListenerConditionType`  
The type of listener rule to create to direct traffic to the ECS service. This parameter is valid only if you specify an existing load balancer (`CreateNew` set to `false`). If set to "None" (the default), no rule is created. If set to "Path", a rule is created with the path pattern specified in the next parameter.

* `ListenerConditionPathPattern`  
The pattern to use in the listener rule, which defines the path to resources. This parameter is valid only if you specify an existing load balancer (`CreateNew` set to `false`) and if `ListenerConditionType` is set to "Path". The value is case sensitive, can be up to 128 characters, starts with forward slash ("/"), and consists of the following characters:  
-- Alpha-numeric characters (A–Z, a–z, 0–9);  
-- The following special characters: '_-.$/~"'@:+';  
-- An ampersand, '&', (using `&amp;`');  
-- Wildcard '*' (matches 0 or more characters);  
-- Wildcard '?' (matches exactly 1 character).

* `ListenerConditionPriority`  
The priority of the rule. The value must be unique for the load-balancer listener. This parameter is valid only if you specify an existing load balancer (`CreateNew` set to `false`) and if `ListenerConditionType` is set to "Path". The valid range of values is 1 through 50000, and the default is 100.

_**AdditionalECSServiceSecurityGroups**_  
A comma-delimited list of EC2 security groups to assign to the ECS service.

### Parameters for the `ConsoleAppEcsFargateService` recipe only
---
_**ECSServiceSecurityGroups**_  
A comma-delimited list of EC2 security groups to assign to the ECS service.

### Parameters for the `ConsoleAppEcsFargateScheduleTask` recipe only
---
_**Schedule**_  
The schedule or rate (frequency) that determines when Amazon CloudWatch Events runs the task. For details about the format of this value, see [Schedule Expressions for Rules](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/ScheduledEvents.html) in the [Amazon CloudWatch Events User Guide](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/).

## JSON syntax for AWS Elastic Beanstalk deployments

Use this JSON definition to build and deploy an ASP.NET Core application to [AWS Elastic Beanstalk](https://docs.aws.amazon.com/elastic-beanstalk) on Linux (`RecipeId` set to "AspNetAppElasticBeanstalkLinux"). Recommended if you don't want to deploy your application as a container image.

```
{
    "AWSProfile": "string",
    "AWSRegion": "string",
    "StackName": "string",
    "RecipeId": "AspNetAppElasticBeanstalkLinux",
    "OptionSettingsConfig":
    {
        "BeanstalkApplication":
        {
            "CreateNew": boolean,
            "ApplicationName": "string"
            "ExistingApplicationName": "string"
        },
        "BeanstalkEnvironment":
        {
            "EnvironmentName": "string"
        },
        "InstanceType": "string",
        "EnvironmentType": "string",
        "LoadBalancerType": "string",
        "ApplicationIAMRole":
        {
            "CreateNew": boolean,
            "RoleArn": "string"
        },
        "ServiceIAMRole":
        {
            "CreateNew": boolean,
            "RoleArn": "string"
        },
        "EC2KeyPair": "string",
        "ElasticBeanstalkPlatformArn": "string",
        "ElasticBeanstalkManagedPlatformUpdates":
        {
            "ManagedActionsEnabled": boolean,
            "PreferredStartTime": "string",
            "UpdateLevel": "string"
        },
        "XRayTracingSupportEnabled": boolean,
        "ReverseProxy": "string",
        "EnhancedHealthReporting": "string",
        "HealthCheckURL": "string,
        "ElasticBeanstalkRollingUpdates":
        {
            "RollingUpdatesEnabled": boolean,
            "RollingUpdateType": "string",
            "MaxBatchSize": integer,
            "MinInstancesInService": integer,
            "PauseTime": "string",
            "Timeout": "string"
        },
        "CNamePrefix": "string,
        "ElasticBeanstalkEnvironmentVariables": {
            "key1": "value1",
            "key2": "value2"
        },
        "VPC":
        {
            "UseVPC": boolean,
            "VpcId": "string",
            "Subnets": array,
            "SecurityGroups": array
        }
    }
}
```

The following parameter definitions are specific to the JSON syntax for an Elastic Beanstalk deployment. Also see [Common JSON parameters](#common-json-parameters).

_**BeanstalkApplication**_  
The name of the Elastic Beanstalk application. By default, a new application will be created with the name provided in `ApplicationName`. If you want to use an existing application, set `CreateNew` to `false`, and provide a name through `ExistingApplicationName`. The Application name can contain up to 100 Unicode characters, not including forward slash (/). If you don't provide a name, the application will have the same value as `StackName`.

_**BeanstalkEnvironment**_  
Information about the [Elastic Beanstalk environment](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/concepts.html#concepts-environment). The `EnvironmentName` parameter specifies the name of the environment in which to run the application. If this parameter isn't present, the environment will be named "&lt;StackName&gt;-dev". The name must be from 4 to 40 characters, can contain only letters, numbers, and hyphens, and can't start or end with a hyphen.

_**InstanceType**_  
The [Amazon EC2 instance type](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/instance-types.html) of the EC2 instances created for the environment; for example, "t2.micro". If this parameter isn't included, an instance type is chosen based on the requirements of your project.

_**EnvironmentType**_  
The [type of Elastic Beanstalk environment](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features-managing-env-types.html) to create. Valid values are "SingleInstance", which is a single instance for development work (the default), or "LoadBalanced" for production.

_**LoadBalancerType**_  
The [type of load balancer](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features.managing.elb.html) you want for your environment: "classic", "application" (the default), or "network".  
This parameter is valid only if the value of `EnvironmentType` is "LoadBalanced".

_**ApplicationIAMRole**_  
The IAM role that provides AWS credentials to the application to access AWS services. You can create a new role (the default) or use an existing role. To use an existing role, set `CreateNew` to `false` and include the role's ARN in `RoleArn`.

_**ServiceIAMRole**_  
The IAM role that Elastic Beanstalk assumes when calling other services on your behalf. You can create a new role (the default) or use an existing role. To use an existing role, set `CreateNew` to `false` and include the role's ARN in `RoleArn`.

_**EC2KeyPair**_  
The EC2 key pair that you can use to SSH into EC2 instances for the Elastic Beanstalk environment. If you don't include this parameter and you don't choose a key pair interactively when the deployment tool is running, you won't be able to SSH into the EC2 instance.

_**ElasticBeanstalkPlatformArn**_  
The ARN of the AWS Elastic Beanstalk platform to use with the environment. If this parameter isn't present, the ARN of the latest Elastic Beanstalk platform is used.

For information about how to construct this ARN, see [ARN format](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/AWSHowTo.iam.policies.arn.html) in the [AWS Elastic Beanstalk Developer Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/).

_**ElasticBeanstalkManagedPlatformUpdates**_  
Use this parameter to configure automatic updates for your Elastic Beanstalk platform using [managed platform updates](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/environment-platform-update-managed.html), as described in the [AWS Elastic Beanstalk Developer Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/). If `ManagedActionsEnabled` is set to `true` (the default), you can specify the weekly maintenance window through `PreferredStartTime`, which defaults to "Sun:00:00". Additionally, you can use `UpdateLevel` to specify the patch level to apply: "minor" (the default) or "patch". These options are described in [Managed action option namespaces](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/environment-platform-update-managed.html#environment-platform-update-managed-namespace) in the Elastic Beanstalk developer guide.

Your application continues to be available during the update process.

_**XRayTracingSupportEnabled**_  
To enable [AWS X-Ray](https://docs.aws.amazon.com/xray/latest/devguide/aws-xray.html) support, set this parameter to `true`. X-Ray support is disabled by default.

_**ReverseProxy**_  
The reverse proxy to use in front of the .NET Core web server, Kestrel. This parameter can be set to "nginx" (the default), or it can be set to "none" to use Kestrel as the front facing web server. 

_**EnhancedHealthReporting**_  
Enables enhanced health reporting, which provides free, real-time application and operating-system monitoring of the instances and other resources in your environment. Valid values are "Enhanced" (the default) and "Basic".

This parameter is valid only if `ManagedActionsEnabled` under `ElasticBeanstalkManagedPlatformUpdates` (shown above) is set to `false`.

_**HealthCheckURL**_  
Customize the load balancer health check to ensure that your application is in a good state in addition to the web server. The default value for this parameter is "/".

This parameter is valid only if `EnvironmentType` (shown above) is set to "LoadBalanced".

_**ElasticBeanstalkRollingUpdates**_  
A collection of parameters that define how AWS Elastic Beanstalk performs rolling updates for the environment in which your application operates. For more information, see [Rolling updates](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features.rollingupdates.html) in the [AWS Elastic Beanstalk Developer Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/).

This object is valid only if `EnvironmentType` (shown above) is set to "LoadBalanced".

By default, rolling updates are disabled. To enable rolling updates, set `RollingUpdatesEnabled` to `true`. Then use the other parameters of this object (`RollingUpdateType`, etc.) to configure the behavior. These parameters are described as follows:

* `RollingUpdateType`  
The type of rolling update to implement.  
-- Use a value of "Time" (the default) for time-based rolling updates. These updates wait a certain amount of time between batches. The amount of time is specified by the `PauseTime` parameter shown below.  
-- Use a value of "Health" for health-based rolling updates. These updates wait for new instances to pass health checks before moving on to the next batch.  
-- Use a value of "Immutable" for immutable updates. These updates launch a full set of instances in a new Auto Scaling group.

* `MaxBatchSize`  
The number of instances included in each batch of the rolling update. The valid range of values is 1 through 10000. For information on the default value, see [General options](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/command-options-general.html#command-options-general-autoscalingupdatepolicyrollingupdate) in the [AWS Elastic Beanstalk Developer Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/).

* `MinInstancesInService`  
The minimum number of instances that must be in service within the Auto Scaling group while other instances are terminated. The valid range of values is 0 through 9999. For information on the default value, see [General options](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/command-options-general.html#command-options-general-autoscalingupdatepolicyrollingupdate) in the [AWS Elastic Beanstalk Developer Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/).

* `PauseTime`  
The amount of time that the Elastic Beanstalk service waits after completing updates to one batch of instances before it continues with the next batch. The format of this parameter must be "PT#H#M#S" where each '#' is the number of hours, minutes, and seconds, each of which is optional. For information on the default value, see [General options](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/command-options-general.html#command-options-general-autoscalingupdatepolicyrollingupdate) in the [AWS Elastic Beanstalk Developer Guide](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/).

* `Timeout`  
The maximum amount of time to wait for all instances in a batch of instances to pass health checks before canceling the update. The format of this parameter must be PT#H#M#S where each '#' is the number of hours, minutes, and seconds, each of which is optional. The default value is "PT30M".

_**CNamePrefix**_  
The prefix for the CNAME in your Elastic Beanstalk environment URL. If this parameter isn't specified or its value is an empty string (the default), the CNAME is generated automatically by appending a random alphanumeric string to the environment name.

_**ElasticBeanstalkEnvironmentVariables**_  
Environment properties for your application. Include the properties as key/value pairs of strings.

_**VPC**_  
A collection of parameters that define the Amazon Virtual Private Cloud (VPC) into which you want to launch your application. By default, the application isn't launched into a VPC. If you want to launch into a VPC, set `UseVPC` to `true` and include the following parameters:

* `VpcId`  
The ID of an existing Amazon VPC into which you want to launch your application. It can be the Default VPC for the account (the default behavior), or a VPC that you've created. For more information about VPCs, see the [Amazon Virtual Private Cloud User Guide](https://docs.aws.amazon.com/vpc/latest/userguide/what-is-amazon-vpc.html).

* `Subnets`  
A list of subnet IDs that AWS Elastic Beanstalk should use when it associates your environment with a custom Amazon VPC. You must only specify the IDs of subnets that are in the VPC specified by `VpcId` (above). For more information about subnets, see [Subnets for your VPC](https://docs.aws.amazon.com/vpc/latest/userguide/configure-subnets.html) in the [Amazon Virtual Private Cloud User Guide](https://docs.aws.amazon.com/vpc/latest/userguide/what-is-amazon-vpc.html).  
This parameter is valid only if the `VpcId` parameter (shown above) is set to a valid VPC ID.

* `SecurityGroups`  
A list of the Amazon EC2 security groups to assign to the EC2 instances in the Auto Scaling group. For more information about security groups, see **Security groups** in the [Amazon EC2 User Guide for Linux Instances](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/ec2-security-groups.html) or the [Amazon EC2 User Guide for Windows Instances](https://docs.aws.amazon.com/AWSEC2/latest/WindowsGuide/ec2-security-groups.html).  
This parameter is valid only if the `VpcId` parameter (shown above) is set to a valid VPC ID.


## JSON syntax for Blazor WebAssembly app deployments

Use this JSON definition to build and deploy a Blazor WebAssembly application to a new Amazon Simple Storage Service (Amazon S3) bucket (`RecipeId` set to "BlazorWasm"). The Blazor applications will be exposed publicly through a CloudFront distribution using the S3 bucket as the origin.

```
{
    "AWSProfile": "string",
    "AWSRegion": "string",
    "StackName": "string",
    "RecipeId": "BlazorWasm",
    "OptionSettingsConfig":
    {
        "IndexDocument": "string",
        "ErrorDocument": "string",
        "Redirect404ToRoot": boolean
        "BackendApi":
        {
            "Enable": boolean,
            "Uri": "string",
            "ResourcePathPattern": "string"
        },
        "AccessLogging":
        {
            "Enable": boolean,
            "LogIncludesCookies": boolean,
            "CreateLoggingS3Bucket": boolean,
            "ExistingS3LoggingBucket": "string",
            "LoggingS3KeyPrefix": "string"
        },
        "PriceClass": "string",
        "EnableIpv6": boolean,
        "MaxHttpVersion": "string",
        "WebAclId": "string"
    }
}
```

> **Note**  
> This deployment task deploys a Blazor WebAssembly application to an Amazon S3 bucket. The bucket created during deployment is configured for web hosting and its contents are open to the public with read access.

The following parameter definitions are specific to the JSON syntax for a Blazor WebAssembly deployment. Also see [Common JSON parameters](#common-json-parameters).

_**IndexDocument**_  
The name of the web page to use when the endpoint for your WebAssembly app is accessed with no resource path. The default page name is `index.html`.

_**ErrorDocument**_  
The name of the web page to use when an error occurs while accessing a resource path. The default value is an empty string.

_**Redirect404ToRoot**_  
If this parameter is set to `true` (the default), requests that result in a 403 or a 404 are redirected the index document of the web app, which is specified by `IndexDocument`.

_**BackendApi**_  
The URI to a backend REST API that's added as an origin to the [Amazon CloudFront](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/Introduction.html) distribution; for example, an API Gateway endpoint. By default, this feature is disabled. You can enable the feature by setting `Enable` to `true`. If the feature is enabled, provide the actual URI in the `Uri` parameter and use `ResourcePathPattern` to provide a resource-path pattern that defines which requests go to the backend REST API; for example: "/api/*".

_**AccessLogging**_  
Use this object to configure whether access logs are written for the Amazon CloudFront distribution and how they are written. For more information about CloudFront access logs, see [Using standard logs (access logs)](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/AccessLogs.html) in the [Amazon CloudFront Developer Guide](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/Introduction.html).

By default, access logging is disabled. To enable it, set `Enable` to `true`. Then use the other parameters of this object (`LogIncludesCookies`, etc.) to configure the behavior. These parameters are described as follows:

* `LogIncludesCookies`  
By default, cookies aren't included in access logs. Set this parameter to `true` to include cookies.

* `CreateLoggingS3Bucket`  
If this parameter is set to `true` (the default), a new Amazon S3 bucket for access logs is created.   
> **Note**
> The bucket and logs will be retained after the deployment is deleted.

* `ExistingS3LoggingBucket`  
Specify an existing Amazon S3 bucket instead of creating a new one. The parameter is valid only if `CreateLoggingS3Bucket` is set to `false`.

* `LoggingS3KeyPrefix`  
A key prefix for the S3 bucket in which logs are stored; for example, "app-name/". By default, no key prefix is specified.

_**PriceClass**_  
The edge locations that will respond to requests for the CloudFront distribution. The following are valid values:  
-- "PRICE_CLASS_100": North America and Europe edge locations.  
-- "PRICE_CLASS_200": North America, Europe, Asia, Middle East, and Africa edge locations.  
-- "PRICE_CLASS_ALL" (the default): All edge locations (best performance).

_**EnableIpv6**_  
Specifies whether IPv6 is enabled for the CloudFront distribution. IPv6 is enabled by default. To disable, set this parameter to `false`.

_**MaxHttpVersion**_  
The maximum HTTP version that users can use to communicate with the CloudFront distribution. The following values are valid:  
-- "HTTP2" for HTTP version 2 (the default).  
-- "HTTP1_1" for HTTP version 1.1.

_**WebAclId**_  
The ARN for the AWS WAF (web application firewall) ACL. For more information, see [Managing and using a web access control list (web ACL)](https://docs.aws.amazon.com/waf/latest/developerguide/web-acl.html) in the  [AWS WAF](https://docs.aws.amazon.com/waf/latest/developerguide/waf-chapter.html) developer guide.

## JSON syntax for ASP.NET Core App to AWS App Runner

Use this JSON definition to build an ASP.NET Core application as a container image and deploy it to [AWS App Runner](https://docs.aws.amazon.com/apprunner) (`RecipeId` set to "AspNetAppAppRunner"). If your project doesn't contain a Dockerfile, it will be automatically generated. Recommended if you want to deploy your application as a container image on a fully managed environment.

```
{
    "AWSProfile": "string",
    "AWSRegion": "string",
    "StackName": "string",
    "RecipeId": "AspNetAppAppRunner",
    "OptionSettingsConfig":
    {
        "ServiceName": "string",
        "Port": integer,
        "StartCommand": "string",
        "ApplicationIAMRole":
        {
            "CreateNew": boolean,
            "RoleArn": "string"
        },
        "ServiceAccessIAMRole":
        {
            "CreateNew": boolean,
            "RoleArn": "string"
        },
        "Cpu": "string",
        "Memory": "string",
        "EncryptionKmsKey": "string",
        "HealthCheckProtocol": "string",
        "HealthCheckPath": "string",
        "HealthCheckInterval": integer,
        "HealthCheckTimeout": integer,
        "HealthCheckHealthyThreshold": integer,
        "HealthCheckUnhealthyThreshold": integer,
        "VPCConnector":
            "UseVPCConnector": boolean,
            "CreateNew": boolean,
            "VpcConnectorId": "string",
            "VpcId": "string",
            "Subnets": array,
            "SecurityGroups": array
        }
        "AppRunnerEnvironmentVariables": {
            "key1": "value1",
            "key2": "value2"
        }
    }
}
```

The following parameter definitions are specific to the JSON syntax for AWS App Runner. Also see [Common JSON parameters](#common-json-parameters).

_**ServiceName**_  
The name of the App Runner service. If this parameter isn't present, the service will be named "&lt;`StackName`&gt;-service".

_**Port**_  
The port on which the container is listening for requests. The valid range of values is 0 through 51200, and the default is 80.

_**StartCommand**_  
A command that overrides the image's default start command.

_**ApplicationIAMRole**_  
The IAM role that provides AWS credentials to the application to access AWS services. You can create a new role (the default) or use an existing role. To use an existing role, set `CreateNew` to `false` and include the role's ARN in `RoleArn`.

_**ServiceAccessIAMRole**_  
The IAM role that provides AWS credentials to the App Runner service so that it can pull the container image from [Amazon Elastic Container Registry (Amazon ECR)](https://docs.aws.amazon.com/ecr). You can create a new role (the default) or use an existing role. To use an existing role, set `CreateNew` to `false` and include the role's ARN in `RoleArn`.

_**Cpu**_  
The number of CPU units reserved for each instance of your App Runner service. The following values are valid:  
-- "1024" (the default): 1 vCPU.  
-- "2048": 2 vCPU.

_**Memory**_  
The amount of memory reserved for each instance of your App Runner service. Valid values are "2048" (the default), "3072", and "4096".

_**EncryptionKmsKey**_  
The ARN of the [AWS Key Management Service (KMS)](https://docs.aws.amazon.com/kms) key that's used to encrypt application logs.

_**HealthCheckProtocol**_  
The IP protocol that AWS App Runner uses to perform health checks for your service. Valid values are "HTTP" and "TCP" (the default).

_**HealthCheckPath**_  
The URL that health check requests are sent to. This parameter is valid only if `HealthCheckProtocol` is set to "HTTP".

_**HealthCheckInterval**_  
The time interval, in seconds, between health checks. The valid range of values if 1 through 20, and the default is 5.

_**HealthCheckTimeout**_  
The time, in seconds, to wait for the response before deciding that a health check failed. The valid range of values if 1 through 20, and the default is 2.

_**HealthCheckHealthyThreshold**_  
The number of consecutive checks that must succeed before AWS App Runner decides that the service is healthy. The valid range of values if 1 through 20, and the default is 3.

_**HealthCheckUnhealthyThreshold**_  
The number of consecutive checks that must fail before AWS App Runner decides that the service is unhealthy. The valid range of values if 1 through 20, and the default is 3.

_**VPCConnector**_  
The VPC connector that associates your App Runner service with an Amazon VPC. For more information about VPC connectors, see [Networking with App Runner](https://docs.aws.amazon.com/apprunner/latest/dg/network.html) in the [AWS App Runner Developer Guide](https://docs.aws.amazon.com/apprunner/latest/dg/what-is-apprunner.html).

If the `UseVPCConnector` parameter isn't provided or is set to `false` (the default), the deployment tool doesn't associate your service with a VPC. To associate your service with a VPC, set `UseVPCConnector` to `true` and also provide information about the VPC to use. By default, the deployment tool will not create a new VPC connector, but expects the ARN of an existing VPC connector in `VpcConnectorId`.  
To create a new VPC connector, set `CreateNew` to `true` and provide the following information:

* `VpcId`  
The ID of the VPC that AWS App Runner should use when it creates a new VPC connector. It can be the Default VPC for the account (the default behavior), or a VPC that you've created.
* `Subnets`  
A list of subnet IDs that AWS App Runner should use when it associates your service with an Amazon VPC. Specify IDs of subnets of a single Amazon VPC.
* `SecurityGroups`  
A list of IDs of security groups that AWS App Runner should use for access to AWS resources under the specified subnets. If not specified, AWS App Runner uses the default security group of the Amazon VPC. The default security group allows all outbound traffic.

_**AppRunnerEnvironmentVariables**_  
Environment properties for your application. Include the properties as key/value pairs of strings.

## JSON syntax for Docker container image to Amazon ECR

Use this JSON definition to push a Docker container image to [Amazon Elastic Container Registry (Amazon ECR)](https://docs.aws.amazon.com/ecr) (`RecipeId` set to "PushContainerImageEcr").

```
{
    "AWSProfile": "string",
    "AWSRegion": "string",
    "StackName": "string",
    "RecipeId": "PushContainerImageEcr",
    "OptionSettingsConfig":
    {
        "ImageTag": "string",
    }
}
```

The following parameter definitions are specific to the JSON syntax for pushing a Docker container image to Amazon ECR. Also see [Common JSON parameters](#common-json-parameters).

_**ImageTag**_  
The tag associated with the container images that are pushed to Amazon ECR.  
The image tag can contain only the following characters: uppercase and lowercase letters, digits, dashes, periods (.) and underscores (_). It can contain a maximum of 128 characters and cannot start with a special character.