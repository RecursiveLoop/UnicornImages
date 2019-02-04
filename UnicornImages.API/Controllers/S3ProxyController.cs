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
    /// <summary>
    /// ASP.NET Core controller acting as a S3 Proxy.
    /// </summary>
    [Route("api/[controller]")]
    public class S3ProxyController : Controller
    {
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }

        public S3ProxyController(IConfiguration configuration, ILogger<S3ProxyController> logger, IAmazonS3 s3Client)
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

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            var listResponse = await this.S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = this.BucketName
            });

            try
            {
                this.Response.ContentType = "text/json";
                return new JsonResult(listResponse.S3Objects, new JsonSerializerSettings { Formatting = Formatting.Indented });
            }
            catch (AmazonS3Exception e)
            {
                this.Response.StatusCode = (int)e.StatusCode;
                return new JsonResult(e.Message);
            }
        }

        [HttpGet("{key}")]
        public async Task Get(string key)
        {
            try
            {
                var getResponse = await this.S3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = this.BucketName,
                    Key = key
                });

                this.Response.ContentType = getResponse.Headers.ContentType;
                getResponse.ResponseStream.CopyTo(this.Response.Body);
            }
            catch (AmazonS3Exception e)
            {
                this.Response.StatusCode = (int)e.StatusCode;
                var writer = new StreamWriter(this.Response.Body);
                writer.Write(e.Message);
            }
        }

        class PutObjectResult
        {
            public string Key { get; set; }

            public string ETag { get; set; }

            public List<string> FilterUrls = new List<string>();
        }

        [HttpPut("{key}")]
        public async Task<JsonResult> Put(string key)
        {

            List<PutObjectResult> lst = new List<PutObjectResult>();

            foreach (var file in Request.Form.Files)
            {
                // Copy the request body into a seekable stream required by the AWS SDK for .NET.
                var seekableStream = new MemoryStream();
                file.CopyTo(seekableStream);
                seekableStream.Seek(0, SeekOrigin.Begin);


                var putRequest = new PutObjectRequest
                {
                    BucketName = this.BucketName,
                    ContentType = file.ContentType,
                    Key = key,
                    InputStream = seekableStream
                };

                try
                {
                    var response = await this.S3Client.PutObjectAsync(putRequest);
                    Logger.LogInformation($"Uploaded object {key} to bucket {this.BucketName}. Request Id: {response.ResponseMetadata.RequestId}");

                    PutObjectResult result = new PutObjectResult { ETag = response.ETag, Key = key };

                    string[] filters = new string[] { "sepia", "lomo", "koda", "polaroid","blur","vignette","glow","oilpaint" };

                    foreach (var filter in filters)
                        result.FilterUrls.Add(Url.Action("Get", "Filter", new { key = key, filtertype = filter, maxwidth = 500 }, Request.Scheme));

                    lst.Add(result);

                }
                catch (Exception e)
                {
                    this.Response.StatusCode = 500;
                    this.Response.ContentType = "text/json";
                    return new JsonResult(new { Error = e.Message }, new JsonSerializerSettings { Formatting = Formatting.Indented });


                }

            }


            // Probably want to return more information here so the front-end knows what to do next
            this.Response.ContentType = "text/json";

            return new JsonResult(lst, new JsonSerializerSettings { Formatting = Formatting.Indented });


        }

        [HttpDelete("{key}")]
        public async Task Delete(string key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = this.BucketName,
                Key = key
            };

            try
            {
                var response = await this.S3Client.DeleteObjectAsync(deleteRequest);
                Logger.LogInformation($"Deleted object {key} from bucket {this.BucketName}. Request Id: {response.ResponseMetadata.RequestId}");
            }
            catch (AmazonS3Exception e)
            {
                this.Response.StatusCode = (int)e.StatusCode;
                var writer = new StreamWriter(this.Response.Body);
                writer.Write(e.Message);
            }
        }
    }
}
