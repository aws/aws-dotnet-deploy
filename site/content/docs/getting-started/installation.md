# Installing the deployment tool<a name="deployment-tool-setup"></a>


****  

The following instructions show you how to install, update, and uninstall the deployment tool\.

**To remove an older version of the deployment tool from your system**

The deployment tool was initially in a NuGet package called "aws.deploy.cli". If you have this older version of the tool, you must remove it before installing or updating the current tool package.

1. Open a command prompt or terminal.
1. Get the version of the deployment tool that you have installed: `dotnet-aws --version`
1. If you have the older version of the tool installed, you'll see a deprecation notice and the version will be 0.40.18 or earlier.
1. If the above is true, uninstall the older version of the tool: `dotnet tool uninstall -g aws.deploy.cli`

**To install the deployment tool**

1. Open a command prompt or terminal.

1. Install the tool: `dotnet tool install --global aws.deploy.tools`

1. Verify the installation by checking the version: `dotnet-aws --version`

**To update the deployment tool**

1. Open a command prompt or terminal\.

1. Check the version: `dotnet-aws --version`
   > **Note**
   > If you see a deprecation notice, you must first uninstall the older version of the tool and install the current version. To do so, see the previous sections of this topic instead.

1. (Optional) Check to see if a later version of the tool is available on the [NuGet page for the deployment tool](https://www.nuget.org/packages/aws.deploy.tools/).

1. Update the tool: `dotnet tool update -g aws.deploy.tools`

1. Verify the installation by checking the version again: `dotnet-aws --version`

**To remove the deployment tool from your system**

1. Open a command prompt or terminal.

1. Uninstall the tool: `dotnet tool uninstall -g aws.deploy.tools`

1. Verify that the tool is no longer installed: `dotnet-aws --version`

## Next steps

- [Set up credentials](setup-creds.md)
- See how to [run the tool](run-tool.md)
