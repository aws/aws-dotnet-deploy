# Test Plan for M1 Release

### Summary

24 Manual Tests

---

### *Test Suite Name:* **Bean Stalk Happy Path**

*Variations:* 

|	|OS 	|Recipe Customization	|AWS Creds	|Project Setup	|CDK	|
|---	|---	|---	|---	|---	|---	|
|1	|Win 10	|None	|Visual Studio Toolkit	|Single ASP.NET Project Core 3.1	|Not Installed	|
|2	|Ubuntu bash	|None	|Assume Role	|Single **ASP.NET** Project .NET 5	|Version 1.61.0 installed globally (too old)	|
|3	|Mac	|None	|Profile/Region via Cli Argument	|Solution with Multiple Projects	|Version 1.89.0 installed globally (older acceptable)	|
|4	|Win 10	|None	|any	|MVC Project talks to Db	|Latest Version possible (newer than tool expects)	|
|5	|Win 10	|None	|any	|MVC Project with Docker	|any	|
|6	|Win 10	|None	|any	|MVC Project without Docker	|any	|

*Background:*

* All Pre-Reqs installed and running
* Setup *CDK*
* Configure *AWS Creds*
* Docker *not* running

*Template:* 

* Create *Project Setup*
    * ie  `dotnet new webapp ‘`*`test1’`*
* `dotnet aws deploy` (from the .csproj directory)
* select Elastic Beanstalk recipe
* press enter for all default prompts
    * confirm beanstalk url is output 
* iwr/curl to beanstalk url
    * confirm 200
* `dotnet aws list-applications`
    * confirm *‘`test1`’* in list
* `dotnet aws delete-applications *‘test1’*`
* iwr/curl to beanstalk url
    * confirm 404
* dotnet aws list
    * confirm *‘`test1`’* _not_ in list

* * *

### *Test Suite Name:* Fargate Web **Happy Path**

*Variations:* 

|	|OS 	|Recipe Customization	|AWS Creds	|Project Setup	|CDK	|
|---	|---	|---	|---	|---	|---	|
|1	|Win 10	|None	|any	|Single ASP.NET Project Core 3.1	|any	|
|2	|Ubuntu bash	|None	|any	|Single **ASP.NET** Project .NET 5	|any	|
|3	|Mac	|None	|any	|Solution with Multiple Projects	|any	|
|4	|Win 10	|None	|any	|MVC Project talks to Db	|any	|

*Background:*

* Node installed.  Docker installed, running, on Linux mode.  AWS Creds default profile.

*Template:* 

* Create *Project Setup*
    * ie  `dotnet new webapp ‘`*`test2’`*
* `dotnet aws deploy` (from the .csproj directory)
* select Fargate Web recipe
* press enter for all default prompts
    * confirm fargate app url is output 
* iwr/curl to fargate app url
    * confirm 200
* `dotnet aws list-applications`
    * confirm *‘`test2`’* in list
* `dotnet aws delete-applications *‘test2’*`
* iwr/curl to carate app url
    * confirm 404
* dotnet aws list
    * confirm *‘`test2`’* _not_ in list

* * *

### *Test Suite Name:* Blazor WASM **Happy Path**

*Variations:* 

|	|OS 	|Recipe Customization	|AWS Creds	|Project Setup	|CDK	|
|---	|---	|---	|---	|---	|---	|
|1	|Win 10	|None	|any	|Single ASP.NET Project Core 3.1	|any	|
|2	|Ubuntu bash	|None	|any	|Single ASP.NET Project .NET 5	|any	|
|3	|Mac	|None	|any	|Solution with Multiple Projects	|any	|
|4	|Win 10	|None	|any	|MVC Project talks to Db	|any	|

*Background:*

* Node installed.  Docker installed, running, on Linux mode.  AWS Creds default profile.

*Template:* 

* Create *Project Setup*
    * ie  `dotnet new blazorwasm -o‘`*`test3’`*
* `dotnet aws deploy` (from the .csproj directory)
* select Blazor recipe
* press enter for all default prompts
    * confirm blazor s3 public url is output 
* iwr/curl to blazor s3 public url 
    * confirm 200
* `dotnet aws list-applications`
    * confirm *‘`test3`’* in list
* `dotnet aws delete-applications *‘test3’*`
* iwr/curl to blazor s3 public url
    * confirm 404
* dotnet aws list
    * confirm *‘`test3`’* _not_ in list

* * *

### *Test Suite Name:* Fargate Service Happy Path

*Variations:* 

|	|OS 	|Recipe Customization	|AWS Creds	|Project Setup	|CDK	|
|---	|---	|---	|---	|---	|---	|
|1	|Win 10	|None	|any	|Console Project that talks to a database	|any	|
|2	|Ubuntu bash	|None	|any	|Console Project without Docker	|any	|
|3	|Mac	|None	|any	|Console Project with Docker	|any	|

*Background:*

* Node installed.  Docker installed, running, on Linux mode.  AWS Creds default profile.

*Template:* 

* Create *Project Setup*
    * ie  `dotnet new console`
    * Modify Program.cs to `Console.WriteLine("test4")`
* `dotnet aws deploy` (from the .csproj directory)
* select Fargate Service recipe
* press enter for all default prompts
* login to AWS Console, navigate to Fargate
    * confirm `test4 `present in app list
    * navigate to CloudWatch, confirm “`test4`” written in logs.
* `dotnet aws list-applications`
    * confirm *‘`test4`’* in list
* `dotnet aws delete-applications *‘test4’*`
* login to AWS Console, navigate to Fargate
    * confirm `test4` *not* present in app list
* dotnet aws list
    * confirm *‘`test4`’* _not_ in list

* * *

### *Test Suite Name:* Fargate Scheduled Task Happy Path

*Variations:* 

|	|OS 	|Recipe Customization	|AWS Creds	|Project Setup	|CDK	|
|---	|---	|---	|---	|---	|---	|
|1	|Win 10	|None	|any	|Console Project that talks to a database	|any	|
|2	|Ubuntu bash	|None	|any	|Console Project without Docker	|any	|
|3	|Mac	|None	|any	|Console Project with Docker	|any	|

*Background:*

* Node installed.  Docker installed, running, on Linux mode.  AWS Creds default profile.

*Template:* 

* Create *Project Setup*
    * ie  `dotnet new console`
    * Modify Program.cs to `Console.WriteLine("test5")`
* `dotnet aws deploy` (from the .csproj directory)
* select Fargate Scheduled Task recipe
* customize Schedule to `rate(1 minute)`
* press enter for all other default prompts
* wait 1 minute so task has time to run
* login to AWS Console, navigate to Fargate
    * confirm `test5` present in app list
    * navigate to CloudWatch, confirm “`test5`” written in logs.
* `dotnet aws list-applications`
    * confirm *‘`test5`’* in list
* `dotnet aws delete-applications *‘test5’*`
* login to AWS Console, navigate to Fargate
    * confirm `test5` *not* present in app list
* dotnet aws list
    * confirm *‘`test5`’* _not_ in list

* * *

### *Test Suite Name:* Advanced Recipe Configuration via Fargate Web

*Variations:* 
 *N/A*

*Tests:*

* Option Settings Navigation
* Type Hints VPC, ECS Service, IAM Role and Docker Build Args
* Can re-deploy basic

*Background:*

* Node installed.  Docker installed, running, on Linux mode.  AWS Creds default profile.
* AWS Account has an existing VPC, ECS Service, and IAM Role with Administrator permissions

*Template:* 

* Create a new Mvc Web App
    * ie  `dotnet new mvc ‘`*`test-config’`*
    * modify Index Controller:

```csharp
#if CUSTOM_SYMBOL
     return Ok();
#else
     return Error("Not compiled with Custom Symbol")
#endif
```

* `dotnet aws deploy` (from the .csproj directory)
* select Fargate Web recipe
* customize VPC
    * select existing VPC
* customize ECS
    * select existing ECS
* customize IAM Role
    * chose existing IAM role
* type ‘more’ to see advanced options
* customize Docker Build Args
    * `/p:DefineConstants=NOT_CUSTOM_SYMBOL_FLAG`
* confirm list of settings includes all customizations made above
* deploy
    * confirm fargate app url is output 
* iwr/curl to fargate app url
    * confirm 500 with error message `"Not compiled with Custom Symbol"`
* redeloy: `dotnet aws deploy` (from the .csproj directory)
* type ‘more’ to see advanced options
    * confirm the value of VPC shows existing VPC Id
    * confirm the value of Docker Build Args is `/p:DefineConstants=NOT_CUSTOM_SYMBOL_FLAG`
* customize Docker Build Args
    * `/p:DefineConstants=CUSTOM_SYMBOL`
* deploy
    * confirm fargate app url is output 
* iwr/curl to fargate app url
    * confirm 200
* `dotnet aws delete-applications *‘test-config’*`
* iwr/curl to fargate app url
    * confirm 404
* confirm pre-existing VPC, ECS Service and IAM Role have not been deleted.

* * *

### *Test Suite Name:* Node not Installed

*Variations:* 
 *N/A*

*Tests:*

* Error Out: Node not installed

*Background:*

* Node not installed.

*Template:* 

* Recommended to use the Linux Docker Manual testing image.  
    * disable installation of node
* Create a new Mvc Web App
    * ie  `dotnet new mvc ‘`*`test-no-node’`*

* `dotnet aws deploy` (from the .csproj directory)
* confirm error message “*Node.js is Required*”  and simple instructions to install.

* * *

### *Test Suite Name:* No Matching Recipe

*Variations:* 

|	|OS 	|AWS Creds	|Project Setup	|CDK	|
|---	|---	|---	|---	|---	|
|1	|any	|any	|.NET Framework Web App	|any	|
|2	|any	|any	|Lambda Project	|any	|

*Background:*

* Node installed.  Docker installed, running, on Linux mode.  AWS Creds default profile.

*Template:* 

* Create *Project Setup*
* `dotnet aws deploy` (from the .csproj directory)
* confirm error message indicating code project was found, but it’s not currently supported.

* * *

