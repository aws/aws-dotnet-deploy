**Recipe ID:** AspNetAppAppRunner

**Recipe Description:** This ASP.NET Core application will be built as a container image on Linux and deployed to AWS App Runner, a fully managed service for web applications and APIs. If your project does not contain a Dockerfile, it will be automatically generated, otherwise an existing Dockerfile will be used. Recommended if you want to deploy your web application as a Linux container image on a fully managed environment.

**Settings:**

* **Service Name**
    * ID: ServiceName
    * Description: The name of the AWS App Runner service.
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
