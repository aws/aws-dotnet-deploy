// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Orchestration.ServiceHandlers
{
    public interface IS3Handler
    {
        Task UploadToS3Async(string bucket, string key, string filePath);
    }

    public class AWSS3Handler : IS3Handler
    {
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IFileManager _fileManager;

        private const int UPLOAD_PROGRESS_INCREMENT = 10;

        public AWSS3Handler(IAWSClientFactory awsClientFactory, IOrchestratorInteractiveService interactiveService, IFileManager fileManager)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _fileManager = fileManager;
        }

        public async Task UploadToS3Async(string bucket, string key, string filePath)
        {
            using (var stream = _fileManager.OpenRead(filePath))
            {
                _interactiveService.LogMessageLine($"Uploading to S3. (Bucket: {bucket} Key: {key} Size: {_fileManager.GetSizeInBytes(filePath)} bytes)");

                var request = new TransferUtilityUploadRequest()
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = stream
                };

                request.UploadProgressEvent += CreateTransferUtilityProgressHandler();

                try
                {
                    var s3Client = _awsClientFactory.GetAWSClient<IAmazonS3>();
                    await new TransferUtility(s3Client).UploadAsync(request);
                }
                catch (Exception e)
                {
                    throw new S3Exception(DeployToolErrorCode.FailedS3Upload, $"Error uploading to {key} in bucket {bucket}", innerException: e);
                }
            }
        }

        private EventHandler<UploadProgressArgs> CreateTransferUtilityProgressHandler()
        {
            var percentToUpdateOn = UPLOAD_PROGRESS_INCREMENT;
            EventHandler<UploadProgressArgs> handler = ((s, e) =>
            {
                if (e.PercentDone != percentToUpdateOn && e.PercentDone <= percentToUpdateOn) return;

                var increment = e.PercentDone % UPLOAD_PROGRESS_INCREMENT;
                if (increment == 0)
                    increment = UPLOAD_PROGRESS_INCREMENT;
                percentToUpdateOn = e.PercentDone + increment;
                _interactiveService.LogMessageLine($"... Progress: {e.PercentDone}%");
            });

            return handler;
        }
    }
}
