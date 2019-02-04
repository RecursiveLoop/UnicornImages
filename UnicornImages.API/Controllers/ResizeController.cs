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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace UnicornImages.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResizeController : ControllerBase
    {
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }
        public ResizeController(IConfiguration configuration, ILogger<S3ProxyController> logger, IAmazonS3 s3Client)
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

        public static Stream ResizeImage(Image<Rgba32> fullSizeImage,decimal resizeWidth,decimal resizeHeight)
        {
            MemoryStream ms = new MemoryStream();

            decimal rnd = Math.Min(resizeWidth / (decimal)fullSizeImage.Width, resizeHeight / (decimal)fullSizeImage.Height);
            var newSize = new SixLabors.Primitives.Size(
           (int)Math.Round(fullSizeImage.Width * rnd), (int)Math.Round(fullSizeImage.Height * rnd));

            fullSizeImage.Mutate(ctx => ctx.Resize(newSize));
            fullSizeImage.Save(ms, new JpegEncoder());
           
            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }


        [HttpGet("{key}")]
        public async Task Get(string key)
        {
            // We should really add some checks for the image type or file extension

            try
            {
                var getResponse = await this.S3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = this.BucketName,
                    Key = key
                });
                string strContentType = getResponse.Headers.ContentType;
                MemoryStream ms = new MemoryStream();
                using (Stream responseStream = getResponse.ResponseStream)
                {

                    using (Image<Rgba32> image = Image.Load(responseStream))
                    {
                        decimal resizeWidth = 150;
                        decimal resizeHeight = 150;
                        strContentType = "image/jpeg";
                        ResizeImage(image, resizeWidth, resizeHeight).CopyTo(ms);
                    }
                }
                this.Response.ContentLength = ms.Length;
                this.Response.ContentType = strContentType;
                ms.CopyTo(this.Response.Body);
            }
            catch (AmazonS3Exception e)
            {
                this.Response.StatusCode = (int)e.StatusCode;
                var writer = new StreamWriter(this.Response.Body);
                writer.Write(e.Message);
            }
            catch (Exception ex)
            {
                this.Response.StatusCode = 500;
                var writer = new StreamWriter(this.Response.Body);
                writer.Write(ex.Message);
            }
        }
    }
}