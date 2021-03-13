# Run this script to create a consto university database in your aws account.
# It will then output the connetion string you can use to set populate your appsetting.json file.
#
# Requires:
# * aws cli installed
# * aws credentials configured (run aws configure)

Install-Module sqlserver
Import-Module sqlserver

$databaseServerName = "ContosoUniversity"
$databaseName = "ContosoUniversityDb"
$masterUserName = "cli_testing"
$masterUserPassword = "Test1ng!043"

Write-Host ""

$database = (aws rds describe-db-instances | ConvertFrom-Json).DBInstances | where DBInstanceIdentifier -eq "$databaseServerName"

if (!($database)) {
    Write-Host "Creating new Database Server $databaseServerName"

    (aws rds create-db-instance --db-instance-identifier "$databaseServerName" --db-instance-class "db.t3.small" --engine "sqlserver-ex" --allocated-storage 20 --master-username "$masterUserName" --master-user-password "$masterUserPassword" )  | Out-Null

    # give aws a few seconds to provision
    Start-Sleep -s 10

    $database = (aws rds describe-db-instances | ConvertFrom-Json).DBInstances | where DBInstanceIdentifier -eq "$databaseServerName"
} else {
    Write-Host "Found existing Database Server $databaseServerName"
}

while ($database.DBInstanceStatus -ne "available" -and $database.DBInstanceStatus -ne "backing-up") {
    Write-Host "Waiting for Database to repost as 'availabe' ..."

    Start-Sleep -s 10

    $database = (aws rds describe-db-instances | ConvertFrom-Json).DBInstances | where DBInstanceIdentifier -eq "$databaseServerName"
}

Write-Host

$address = $database.Endpoint.Address
$port = $database.Endpoint.Port

Write-Host "Ensuring Database Exists: $databaseName"

$createDbSql =
@"
USE MASTER
GO

IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{0}')
  BEGIN
    CREATE DATABASE [{0}]
   END
GO
"@ -f $databaseName

Write-Host $createDbSql

Invoke-Sqlcmd -Query $createDbSql -ConnectionString "Server=${address},${port};User Id=$masterUserName;Password=$masterUserPassword;"

Write-Host "Connection String (add to appsettings.json):"
Write-Host "Server=${address},${port};Database=$databaseName;User Id=$masterUserName;Password=$masterUserPassword;"



