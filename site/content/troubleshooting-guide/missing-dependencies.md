
# Missing Dependencies
This section of the troubleshooting guide explains how to determine, diagnose, and fix common issues related to missing external dependencies of AWS.Deploy.Tools

## Node.js not installed

**Why is this happening**: AWS.Deploy.Tools relies on [AWS Cloud Development Kit](https://aws.amazon.com/cdk/) (CDK) to provision resources for your cloud application. AWS CDK requires Node.js to be installed in your machine. See the  [CDK's FAQs](https://aws.amazon.com/cdk/faqs/) for more information about how it uses Node.js.

*Minimum required Node.js version >= 18.0.0*

**Resolution**: See [here](https://nodejs.org/en/download/) to install Node.js on your system.

## Docker not installed
**Why is this happening**: AWS.Deploy.Tool requires Docker to be installed in order to perform containerized deployments.

**Resolution**: See [here](https://docs.docker.com/get-docker/) to install Docker for your operating system.

## Zip utility not installed
**Why is this happening**: Non-container based deployments types (such as deployments to AWS Elastic Beanstalk) create a zip file of the artifacts produced by the `dotnet publish` command.

The zip command line utility is not installed by default on some **Linux** distributions. If you are deploying using a non-container based option, you may encounter an error saying:
```
We were unable to create a zip archive of the packaged application.
Normally this indicates a problem running the \"zip\" utility. Make sure that application is installed and available in your PATH.
```
In this case, it is likely that `zip` is not installed on your system.

We use the Linux zip tool to maintain Linux file permissions.

**Resolution**: To install zip on Linux OS, run the following commands depending on your distribution's package management tool.

For distributions using `apt-get`:
```
sudo apt-get install zip
```

For distributions using `yum`:
```
sudo yum intall zip
```

After installation, use the command to verify that zip was installed correctly.
```
zip
```