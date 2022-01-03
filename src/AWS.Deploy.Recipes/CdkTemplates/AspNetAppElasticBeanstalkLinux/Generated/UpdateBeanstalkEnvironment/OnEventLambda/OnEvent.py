import boto3

client = boto3.client('elasticbeanstalk')


def handler(event, context):
    request_type = event['RequestType'].lower()
    if request_type == 'create' or request_type == 'update':
        return on_create_or_update(event)
    if request_type == 'delete':
        # No action will be performed on the existing environment upon custom resource deletetion.
        return
    raise Exception(f'Invalid request type: {request_type}')


def on_create_or_update(event):
    props = event["ResourceProperties"]
    update_environment(props['ApplicationName'], props['EnvironmentName'], props['VersionLabel'])
    physical_id = get_physical_id(props)
    return {'PhysicalResourceId': physical_id}


def update_environment(applicationName, environmentName, versionLabel):
    response = client.update_environment(ApplicationName = applicationName, EnvironmentName = environmentName,VersionLabel=versionLabel)


def get_physical_id(props):
    return 'EBUpdate-' + props['ApplicationName'] + '-' + props['EnvironmentName']