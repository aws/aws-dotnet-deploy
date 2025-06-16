# Setting up your environment

To run the AWS Deploy Tool, you need the following pre-requisites set up in your environment:

#### AWS Account
* An *AWS account* with a local credential profile configured in the shared AWS config and credentials files. For information on setting up a profile, see our [SDK Reference Guide](https://docs.aws.amazon.com/sdkref/latest/guide/access-users.html).

* The local credential profile can be configured by a variety of tools. For example, the credential profile can be configured with the [AWS Toolkit for Visual Studio](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/credentials.html) or the [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html), among others.

  > Note: You need to make sure to add the appropriate AWS permissions to your credentials’ profile / assumed role. See [Setting up Credentials](setup-creds.md)

#### .NET 8 or later
* .NET CLI - the deployment tool can be used from the .NET command-line interface (CLI) - a cross-platform toolchain for developing, building, running, and publishing .NET applications.

* The .NET CLI is included with the [.NET SDK](https://docs.microsoft.com/en-us/dotnet/core/sdk). For information about how to install or update .NET, see [https://dotnet.microsoft.com/](https://dotnet.microsoft.com/).

* The deployment tool requires .NET 8 or later to be installed. However, the deployment tool supports deploying applications built using .NET Core 3.1 or later (for example, .NET Core 3.1, .NET 5.0, .NET 6.0, .NET 7, and newer). To see what version you have, run the following on the command prompt or in a terminal:

```
dotnet --version
```

#### Node.js

* The deployment tool requires the [AWS Cloud Development Kit (CDK)](https://docs.aws.amazon.com/cdk/latest/guide/), and the AWS CDK requires [Node.js](https://nodejs.org/en/download/). AWS CDK requires Node.js, versions 18.x (or later) - we recommend installing the latest LTS version.

* To install Node.js, go to  [Node.js downloads](https://nodejs.org/en/download/)

* To see which version of Node.js you have installed, run the following command at the command prompt or in a terminal:

```
node --version
```

   > ***Note:***

>*If the AWS CDK isn't installed on your machine or if the AWS CDK that's installed is earlier than the required minimum version (2.13.0), the deployment tool will install a **temporary and "private" copy of the CDK** that will be used only by the tool, leaving the global configuration of your machine untouched.*

>*If instead you want to install the AWS CDK, see [Install the AWS CDK](https://docs.aws.amazon.com/cdk/latest/guide/getting_started.html#getting_started_install) in the [AWS Cloud Development Kit (CDK) Developer Guide](https://docs.aws.amazon.com/cdk/latest/guide/)*


#### Docker (Optional)
* Docker - required when deploying to a container based service like Amazon Elastic Container Service (Amazon ECS) or AWS App Runner.

* To install Docker, go to [https://docs.docker.com/engine/install/](https://docs.docker.com/engine/install/).

#### ZIP CLI (Linux and macOS)
* Mac / Linux only. Used when creating zip packages for deployment bundles. The zip cli is used to maintain Linux file permissions.
