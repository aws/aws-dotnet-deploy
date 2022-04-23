# Docker  Issues
This section of the troubleshooting guide explains how to determine, diagnose, and fix common issues related to Docker.

## Docker not running in Linux mode

**Why is this happening**: If you are on a Windows operating system, it is likely that you are running Docker in a Windows container. AWS.Deploy.Tools requires Docker to be running in Linux container mode.

**Resolution**: See [here](https://docs.docker.com/desktop/windows/#switch-between-windows-and-linux-containers) to switch between Windows and Linux containers.

## Failed to build Docker Image

**Why is this happening**:
Sometimes the Docker build command may fail due to the following reasons:

* **Invalid Docker execution directory**
The Docker execution directory is the working directory for the Docker build command and this is where all relative paths in the Dockerfile are resolved from. By default, the execution directory is set to the project solution directory. However, it is possible that the Dockerfile is referencing projects outside the solution directory. Setting an invalid Docker execution directory would result in Docker build failure

* **Missing project dependencies**
The Docker build command may also fail if all project dependencies are not specified in the Dockerfile. 

**Resolution**:

* AWS.Deploy.Tools gives you the ability to specify a Docker execution directory of your choice. Try setting a different execution directory that can correctly evaluate all relative paths in the Dockerfile.
* A good starting point to include all dependencies would be to inspect the solution file and add the relevant projects in the Dockerfile. If a custom Dockerfile is not provided, AWS.Deploy.Tools can generate one for container based deployments. It will inspect the solution file and include all projects defined in it to the generated Dockerfile. This Dockerfile is persisted on disk. In the future, if you add a new project to the solution file, you must manually add a new entry for it in the persisted Dockerfile.

## Failed to push Docker Image
**Why is this happening**: AWS.Deploy.Tools builds the Docker image and pushes it to Amazon Elastic Container Registry (Amazon ECR). 

If you are missing the required AWS Identity and Access Management (IAM) permissions to perform actions against ECR repositories, the deployment may fail the following error message:

```
Failed to push Docker Image
```
**Resolution**: See [here](https://docs.aws.amazon.com/AmazonECR/latest/userguide/repository-policy-examples.html) for guidance on how to set IAM policy statements to allow actions on Amazon ECR repositories.