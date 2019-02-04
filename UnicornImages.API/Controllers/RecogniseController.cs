using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Amazon.S3;
using Amazon.S3.Model;

using Newtonsoft.Json;

namespace UnicornImages.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecogniseController : ControllerBase
    {
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }
        public RecogniseController(IConfiguration configuration, ILogger<S3ProxyController> logger, IAmazonS3 s3Client)
        {
            this.Logger = logger;
            this.S3Client = s3Client;

            this.BucketName = configuration[Startup.AppS3BucketKey];
            if (string.IsNullOrEmpty(this.BucketName))
            {
                logger.LogCritical("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
                throw new Exception("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
            }

            logger.LogInformation($"Configured to use bucket {this.BucketName}");
        }

        [HttpGet("{key}")]
        public async Task Get(string key)
        {
            // Use the AWS Rekognition service to return a response



            // API is available at https://docs.aws.amazon.com/rekognition/latest/dg/API_DetectFaces.html

        }
    }
}