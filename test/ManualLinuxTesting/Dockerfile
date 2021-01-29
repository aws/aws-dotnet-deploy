# Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
# SPDX-License-Identifier: Apache-2.0

####################################
## Run at repository root with
## docker build -f /test/ManualLinuxTesting/Dockerfile/ . -t 'aws-deploy:local'
##
## Execute with
## docker run -v $HOME/.aws/:/root/.aws --privileged -it --entrypoint bash aws-deploy:local
##
## See the /README.md file for more details
####################################

ARG AWS_CLI_VERSION=0.0.1-local

FROM ubuntu as base-updates
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
RUN apt-get update

FROM base-updates as base
RUN apt-get install -y wget unzip

#install aws cli
RUN wget https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip -O "awscliv2.zip"
RUN unzip awscliv2.zip
RUN ./aws/install
RUN aws --version

# install node/npm
RUN apt-get install -y nodejs
RUN apt-get install -y npm

#install cdk
RUN npm install -g aws-cdk

# install docker
RUN apt-get update; \
    apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg-agent \
    software-properties-common
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add -
RUN apt-get update
RUN add-apt-repository \
   "deb [arch=amd64] https://download.docker.com/linux/ubuntu \
   $(lsb_release -cs) \
   stable"
RUN apt-get update
RUN apt-get install -y docker-ce docker-ce-cli containerd.io

# start docker on login
RUN echo 'service docker start' >> ~/.bashrc

# install dotnet sdk
RUN wget https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get update; \
  apt-get install -y apt-transport-https && \
  apt-get update && \
  apt-get install -y dotnet-sdk-3.1
RUN dotnet --list-sdks

# manually add dotnet tools path to path variable
# https://github.com/dotnet/dotnet-docker/issues/520
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim-amd64 AS build
ARG AWS_CLI_VERSION
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

WORKDIR /src
COPY . .

RUN echo $AWS_CLI_VERSION

RUN dotnet restore "src/AWS.Deploy.CLI/AWS.Deploy.CLI.csproj"
WORKDIR "/src/src/AWS.Deploy.CLI"
RUN dotnet build "AWS.Deploy.CLI.csproj" -c Release -o /app/build -p:Version=$AWS_CLI_VERSION

#FROM build as test
#RUN dotnet test AWS.Deploy.sln

FROM build AS publish
ARG AWS_CLI_VERSION
RUN dotnet pack "AWS.Deploy.CLI.csproj" -c Release -o /app/publish -p:PackageVersion=$AWS_CLI_VERSION -p:Version=$AWS_CLI_VERSION

FROM base AS final
ARG AWS_CLI_VERSION
WORKDIR /app
COPY --from=publish /app/publish .

#install aws deploy
RUN dotnet tool install AWS.Deploy.CLI -g --add-source /app --version $AWS_CLI_VERSION

# verify it worked
RUN dotnet aws --version

#copy in the testing apps
COPY /testapps /testapps

WORKDIR /testapps
ENTRYPOINT ["echo", "\nMake sure to start in Interactive mode:  docker run --privileged -it --entrypoint bash <imagename>'\n\nAdd your AWS creds via `aws configure`\n\nThen try `dotnet aws --help`\n\n"]
