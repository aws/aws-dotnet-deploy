# Installing the deployment tool<a name="deployment-tool-setup"></a>

The following instructions show you how to install, update, and uninstall the deployment tool.

#### Install
To install the deployment tool, use the `dotnet tool install` command:

```
dotnet tool install -g aws.deploy.tools
```

#### Update
To update to the latest version of the deployment tool, use the `dotnet tool update` command.

```
dotnet tool update -g aws.deploy.tools
```

   > **Note**
   > *The deployment tool was initially in a NuGet package called "aws.deploy.cli". If you have this older version of the tool, you'll see a deprecation notice and the version will be 0.40.18 or earlier. Uninstall the older version of the tool and install a new one.*

#### Uninstall
To uninstall it, simply type:

```
dotnet tool uninstall -g aws.deploy.tools
```

#### Help
Once you install the tool, you can view the list of available commands by typing:

```
dotnet aws --help
```

To get help about individual commands like `deploy` or `delete-deployment` you can use the `--help` switch with the commands. For example to get help for the `deploy` command type:

```
dotnet aws deploy --help
```
