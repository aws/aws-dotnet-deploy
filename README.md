## AWS .NET Deploy CLI Tool
Initial prototyping for the AWS .NET CLI Tool

## Local Testing

You use `/test/ManualLinuxTesting/Dockerfile` to create a local sandbox for testing.  This image will build from source and install the aws cli deployment tool.

1.  From the root directory, run 
```
docker build -f ./test/ManualLinuxTesting/Dockerfile/ . -t 'aws-deploy:local'
```

2. _Grab a caffeinated beverage_.  The first run can take several minutes.

3.  Run the docker image in interactive mode: 
```
docker run --privileged -it --entrypoint bash aws-deploy:local
 ```

4. Enter your aws profile file using the aws cli and follow the prompts:

```
aws configure
```

5. Optional: Start docker: `service docker start`.  This is only required to deploy projects via containers.

For more assitance, follow the guide here: https://cdkworkshop.com/15-prerequisites/200-account.html

You are now free to play with the cli.  There are several sample applications in `/testapps` that you can deploy:

```shell
root@562bccd30027:/testapps# cd WebAppNoDockerFile/
root@562bccd30027:/testapps/WebAppNoDockerFile# dotnet aws deploy
AWS .NET Suite for deploying .NET Core applications to AWS
```

## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This project is licensed under the Apache-2.0 License.

