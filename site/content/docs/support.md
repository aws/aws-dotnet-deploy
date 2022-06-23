# Support Matrix

The table below provides a matrix of supported .NET application types and AWS Computes.

|                   | .NET Console App   | 	ASP.NET Core    | Blazor WebAssembly   |
| :---              |    :----:     |          :---:    |    :---:  |
| Amazon Elastic Container Service (ECS) service (Linux)| X             | X                 |           |
| Amazon Elastic Container Service (ECS) task (Linux)	| X             | X                 |           |
| AWS App Runner (Linux)   |              | X                 |           |
| AWS Elastic Beanstalk (Linux and Windows)     |               | X                 |           |
| Amazon S3 & Amazon CloudFront        |               |                   |   X       |


### Amazon ECS using AWS Fargate

* Supports deployments of .NET applications as a service (e.g. web application or a background processor) or as a scheduled task (e.g. end-of-day process) to Amazon Elastic Container Service (Amazon ECS) with compute power managed by AWS Fargate serverless compute engine.
* Recommended if you want to deploy a service or a scheduled task as a container image on Linux.

> **Note: This compute requires a Dockerfile. IF YOUR PROJECT DOES NOT CONTAIN A DOCKERFILE, THE DEPLOYMENT TOOL WILL AUTOMATICALLY GENERATE IT,** otherwise an existing Dockerfile will be used.

[**Amazon Elastic Container Service (Amazon ECS)**](https://aws.amazon.com/ecs/) is a fully managed container orchestration service that helps you easily deploy, manage, and scale containerized applications.

[**AWS Fargate**](https://aws.amazon.com/fargate/) is a serverless, pay-as-you-go compute engine that lets you focus on building applications without managing servers.

### AWS App Runner

* Supports deployments of containerized ASP.NET Core applications to AWS App Runner.
* Recommended if you want to deploy your application as a container image on a fully managed environment.

> **Note: This compute requires a Dockerfile. IF YOUR PROJECT DOES NOT CONTAIN A DOCKERFILE, THE DEPLOYMENT TOOL WILL AUTOMATICALLY GENERATE IT,** otherwise an existing Dockerfile will be used.

[**AWS App Runner**](https://aws.amazon.com/apprunner/) is a fully managed service that makes it easy for developers to quickly deploy containerized web applications and APIs, at scale and with no prior infrastructure experience required. With App Runner, rather than thinking about servers or scaling, you have more time to focus on your applications.

### AWS Elastic Beanstalk

* Supports deployments of ASP.NET Core applications to AWS Elastic Beanstalk on Linux and Windows.
* Recommended if you want to deploy your application directly to EC2 hosts.

[**AWS Elastic Beanstalk**](https://aws.amazon.com/elasticbeanstalk/) is an easy-to-use service for deploying and scaling web applications and services. AWS Elastic Beanstalk automatically handles the deployment, from capacity provisioning, load balancing, auto-scaling to application health monitoring.


### Hosting Blazor WebAssembly applications using Amazon S3 and Amazon CloudFront

Blazor WebAssembly applications can be deployed to an Amazon S3 bucket for web hosting. The Amazon S3 bucket will be created and configured automatically by the tool, which will then upload your Blazor application to the S3 bucket.
