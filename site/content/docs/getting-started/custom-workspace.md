The default workspace used by AWS.Deploy.Tools is `$USERPROFILE/.aws-dotnet-deploy` on Windows and `$USER/.aws-dotnet-deploy` on Unix based OS. This workspace is used to create the CDK project and any other temporary files used by the tool.

You can override the default workspace by the setting the `AWS_DOTNET_DEPLOYTOOL_WORKSPACE` environment variable. 

It must satisfy the following constraints:

* It must point to a valid directory that exists on the disk.
* The directory path must not have any whitespace characters in it.

**Setting up a custom workspace is optional for most users.** However, on Windows OS, if the `$USERPROFILE` path contains a whitespace character then the deployment will fail. 
In that case, users are required to set up a custom workspace that satisfies the above constraints.