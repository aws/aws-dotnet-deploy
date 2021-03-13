## Local Testing

You use `/test/ManualLinuxTesting/Dockerfile` to create a local sandbox for testing.  This image will build from source and install the aws cli deployment tool.

1.  From the root directory, run 
```
docker build -f ./test/ManualLinuxTesting/Dockerfile/ . -t 'aws-deploy:local'
```

2. _Grab a caffeinated beverage_.  The first run can take several minutes.

3.  Run the docker image in interactive mode: 
```
docker run -v $HOME/.aws/:/root/.aws --privileged -it --entrypoint bash aws-deploy:local
 ```
 _Note:_ the above command assumes you are running in powershell and have the `$HOME` variable availabe and that you've [saved your aws credentials](https://cdkworkshop.com/15-prerequisites/200-account.html#configure-your-credentials) in your home directory.

You are now free to play with the cli.  There are several sample applications in `/testapps` that you can deploy:

```shell
root@562bccd30027:/testapps# cd WebAppNoDockerFile/
root@562bccd30027:/testapps/WebAppNoDockerFile# dotnet aws deploy
AWS .NET deployment tool for deploying .NET Core applications to AWS
```
