// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace AWS.Deploy.ServerMode.Client
{
    public class CertificateVerificationEngine
    {
        private const string DEFAULT_DEPLOY_TOOL_ROOT = "dotnet aws";
        private const string CERTIFICATE_SUBJECT_NAME = "Amazon Web Services, Inc.";
        private const string CERTIFICATE_ISSUER_NAME = "DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1";

        public virtual void VerifyCertificate(string deployToolRoot)
        {
            var deployToolDllPath = GetDeployToolDllPath(deployToolRoot);
            var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(deployToolDllPath));
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;

            // verifies that no certificate in the certificate chain is expired or revoked.
            if (!chain.Build(cert))
            {
                throw new InvalidOperationException("The security certificate associated with AWS.Deploy.CLI.dll is either expired or revoked");
            }
            if (!string.Equals(cert.GetNameInfo(X509NameType.SimpleName, false), CERTIFICATE_SUBJECT_NAME, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The security certificate associated with AWS.Deploy.CLI.dll is not authotized by {CERTIFICATE_SUBJECT_NAME}");
            }
            if (!string.Equals(cert.GetNameInfo(X509NameType.SimpleName, true), CERTIFICATE_ISSUER_NAME, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The security certificate associated with AWS.Deploy.CLI.dll is not issued by {CERTIFICATE_ISSUER_NAME}");
            }
        }

        private string GetDeployToolDllPath(string deployToolRoot)
        {
            var deployToolRootPath = string.Empty;
            if (string.Equals(deployToolRoot, DEFAULT_DEPLOY_TOOL_ROOT, StringComparison.Ordinal))
            {
                foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(';'))
                {
                    if (path.Contains(".dotnet" + Path.DirectorySeparatorChar + "tools"))
                    {
                        deployToolRootPath = Path.Combine(path, ".store", "aws.deploy.cli");
                        break;
                    }
                }
            }
            else
            {
                deployToolRootPath = new DirectoryInfo(deployToolRoot).Parent.FullName;
            }

            if (string.IsNullOrEmpty(deployToolRootPath) || !Directory.Exists(deployToolRootPath))
            {
                throw new FailedToFindDeployToolPathException("Could not find path to AWS.Deploy.CLI.dll");
            }

            var deployToolDllPath = Directory.GetFiles(deployToolRootPath, "AWS.Deploy.CLI.dll", SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(deployToolDllPath))
            {
                throw new FailedToFindDeployToolPathException("Could not find path to AWS.Deploy.CLI.dll");
            }
            return deployToolDllPath;
        }
    }
}
