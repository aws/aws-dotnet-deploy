**Recipe ID:** PushContainerImageEcr

**Recipe Description:** This .NET application will be built using an existing Dockerfile. The Docker container image will then be pushed to Amazon ECR, a fully managed container registry.

**Settings:**

* **Image Tag**
    * ID: ImageTag
    * Description: This tag will be associated to the container images which are pushed to Amazon Elastic Container Registry.
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
