**Recipe ID:** AspNetAppExistingBeanstalkEnvironment

**Recipe Description:** This ASP.NET Core application will be built and deployed to an existing AWS Elastic Beanstalk environment. Recommended if you want to deploy your application directly to EC2 hosts, not as a container image.

**Settings:**

* **Enhanced Health Reporting**
    * ID: EnhancedHealthReporting
    * Description: Enhanced health reporting provides free real-time application and operating system monitoring of the instances and other resources in your environment.
    * Type: String
* **Enable AWS X-Ray Tracing Support**
    * ID: XRayTracingSupportEnabled
    * Description: AWS X-Ray is a service that collects data about requests that your application serves, and provides tools you can use to view, filter, and gain insights into that data to identify issues and opportunities for optimization. Do you want to enable AWS X-Ray tracing support?
    * Type: Bool
* **Reverse Proxy**
    * ID: ReverseProxy
    * Description: By default Nginx is used as a reverse proxy in front of the .NET Core web server Kestrel. To use Kestrel as the front facing web server then select `none` as the reverse proxy.
    * Type: String
* **Health Check URL**
    * ID: HealthCheckURL
    * Description: Customize the load balancer health check to ensure that your application, and not just the web server, is in a good state.
    * Type: String
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
