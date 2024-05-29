# Docker  Issues
This section of the troubleshooting guide explains how to determine, diagnose, and fix common issues related to Docker.

## Docker not running in Linux mode

**Why is this happening**: If you are on a Windows operating system, it is likely that you are running Docker in Windows container mode. AWS.Deploy.Tools requires Docker to be running in Linux container mode.

**Resolution**: See [here](https://docs.docker.com/desktop/windows/#switch-between-windows-and-linux-containers) to switch between Windows and Linux containers.

## Failed to build Docker Image

There are multiple reasons your deployment may fail during the Docker build step.

### **Invalid Dockerfile**
**Why is this happening**: If there is a syntax error or invalid argument in your Dockerfile, the Docker build command may fail with an error message like this:

```
failed to solve with frontend dockerfile.v0: failed to create LLB definition: <additional error message>
```
**Resolution**:
Correct any syntax errors or invalid arguments in your Dockerfile. Consult [Docker's Dockerfile reference](https://docs.docker.com/engine/reference/builder/) for the expected syntax for each instruction.

---

### Invalid Docker execution directory
**Why is this happening**: The Docker execution directory is the [working directory for the Docker build command](https://docs.docker.com/engine/reference/commandline/build/#build-with-path). All relative paths in the Dockerfile are resolved from this directory. By default, the execution directory is set to your project's solution directory. However it is possible that the Dockerfile is referencing projects outside of your solution directory, which may result in an error message like this:

```
failed to compute cache key: "/Path/To/A/Dependency.csproj" not found: not found
```

**Resolution**:
AWS.Deploy.Tools allows you to specify an alternative Docker execution directory. Try setting an execution directory that can correctly evaluate all relative paths in the Dockerfile. If you are using the CLI version of AWS.Deploy.Tools, set the "Docker Execution Directory" under "Advanced Settings." If you are using the "Publish to AWS" feature in the AWS Toolkit for Visual Studio, set the "Docker Execution Directory" under the "Project Build" settings.

---

### Missing project dependencies
**Why is this happening**:
The Docker build command may fail during the `RUN dotnet build` instruction if all of your project dependencies are not specified in the Dockerfile.

**Resolution**:
Ensure all dependencies from your project and solution files are included in your Dockerfile. A good starting point is to inspect the solution file and add the relevant projects to the Dockerfile.

If a custom Dockerfile is not initally provided, AWS.Deploy.Tools will generate one if you select a container-based deployment. The generated Dockerfile will include the projects currently defined in your solution file. This Dockerfile is persisted on disk. If you add a new dependency to the solution file in the future, you must manually add a new entry for it in the persisted Dockerfile.

---

### Failed to restore package references

**Why is this happening**:
The Docker build command may fail during the `RUN dotnet restore` instruction if the container does not have connectivity to the NuGet.

You may see an error message like:

```
Unable to load the service index for source https://api.nuget.org/v3/index.json
```

or

```
Failed to download package <package name> from https://api.nuget.org/
```

**Resolution**:
Your container may be unable to access the internet or NuGet for a variety of network-related reasons. If you are using Docker Desktop, consult the [Docker troubleshooting guide and documentation](https://docs.docker.com/desktop/faqs/general/#where-can-i-find-information-about-diagnosing-and-troubleshooting-docker-desktop-issues).

If your container is able to access the internet but the package restore is failing because you are using a private NuGet feed, you may need to configure credentials for the private feed within the Dockerfile.

---

## Failed to push Docker Image
**Why is this happening**: AWS.Deploy.Tools builds the Docker image and pushes it to Amazon Elastic Container Registry (Amazon ECR).

If you are missing the required AWS Identity and Access Management (IAM) permissions to perform actions against ECR repositories, the deployment may fail the following error message:

```
Failed to push Docker Image
```
**Resolution**: See [here](https://docs.aws.amazon.com/AmazonECR/latest/userguide/repository-policy-examples.html) for guidance on how to set IAM policy statements to allow actions on Amazon ECR repositories.

## Failed to generate a Dockerfile

**Why is this happening** You may see this if your project has project references (.csproj, .vbproj) that are located in a higher folder than the solution file (.sln) that AWS.Deploy.Tools is using to generate a Dockerfile. In this case AWS.Deploy.Tools will not generate a Dockerfile to avoid a large build context that can result in long builds.

**Resolution**: If you would still like to deploy to an [AWS service that requires Docker](../docs/support.md), you must create your own Dockerfile and set an appropriate "Docker Execution Directory" in the deployment options. Alternatively you may choose another deployment target that does not require Docker, such as AWS Elastic Beanstalk.


## Application deployment stuck or fails because of health check

Microsoft has made changes to the base images used in .NET 8 which now expose 8080 as the default HTTP port instead of the port 80 which was used in previous versions. In addition to that, Microsoft now uses a non-root user by default.

As we added support for deploying .NET 8 container-based applications, the container port setting in the recipes that support it now defaults to 8080 for .NET 8 and 80 in previous versions. For applications that do not have a `dockerfile`, we generate one accordingly. However, for applications that have their own `dockerfile`, the user is responsible for setting and exposing the proper port. If the container port is different from the port exposed in the container, the deployment might keep going until it reaches a timeout from the underlying services, or you might receive an error related to the health check.

In the tool, we have added a warning message if we detect that the container port setting is different from the one exposed in the container.