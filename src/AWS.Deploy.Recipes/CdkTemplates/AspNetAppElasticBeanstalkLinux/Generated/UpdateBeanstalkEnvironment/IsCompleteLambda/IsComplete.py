import boto3

client = boto3.client('elasticbeanstalk')


def handler(event, context):
    request_type = event['RequestType'].lower()
    props = event['ResourceProperties']
    applicationName, environmentName, versionLabel = props['ApplicationName'], props['EnvironmentName'], props['VersionLabel']

    if request_type == 'delete':
        # No action will be performed on the existing environment upon custom resource deletetion. Hence there is no process to monitor
        return { 'IsComplete': True }
    
    if request_type == 'create' or request_type == 'update':
        return { 'IsComplete': is_complete(applicationName, environmentName, versionLabel) }

    
def is_complete(applicationName, environmentName, versionLabel):
    response = client.describe_environments(ApplicationName = applicationName, EnvironmentNames = [environmentName])
    for environment in response['Environments']:
        if environment['VersionLabel'] == versionLabel and environment['Health'] == 'Green':
            return True
    return False