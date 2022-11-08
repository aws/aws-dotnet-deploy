# Deploying a sample application

This article teaches you how to deploy a simple “Hello World!" web application to AWS. Install the tool and its pre-requisites before running the steps below.

The command for the deployment tool can be expressed in one of two forms. Either form might be used for command examples.

```
dotnet aws ...
dotnet-aws ...
```

#### Step 1: Create the ASP.NET Web application

```
dotnet new web -n HelloWorld -f net6.0
```

#### Step 2: cd to the project folder

```
cd HelloWorld
```

#### Step 3: Deploy to AWS

```
dotnet aws deploy --profile default --region us-east-1
```

