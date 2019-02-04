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
    public class FilterController : ControllerBase
    {
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }
        public FilterController(IConfiguration configuration, ILogger<S3ProxyController> logger, IAmazonS3 s3Client)
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

        [HttpGet("{filtertype}/{maxwidth}/{key}")]
        public async Task Get(string key, string filtertype, int maxwidth)
        {

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

                        var newSize = new SixLabors.Primitives.Size(
                       (int)maxwidth, (int)(maxwidth/image.Width  * image.Height));
                        image.Mutate(ctx => ctx.Resize(newSize));
                        FilterImage(image, filtertype);
                        strContentType = "image/jpeg";
                        image.Save(ms, new JpegEncoder());
                        ms.Seek(0, SeekOrigin.Begin);
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

        public static void FilterImage(Image<Rgba32> image, string filtertype)
        {
            switch (filtertype.ToLower().Trim())
            {
                case "sepia":
                    image.Mutate(ctx => ctx.Sepia(0.8f));
                    break;
                case "lomo":
                    image.Mutate(ctx => ctx.Lomograph());
                    break;
                case "koda":
                    image.Mutate(ctx => ctx.Kodachrome());
                    break;
                case "polaroid":
                    image.Mutate(ctx =>  ctx.Polaroid());
                    break;
                case "blur":
                    image.Mutate(ctx =>  ctx.GaussianBlur());
                    break;
                case "glow":
                    image.Mutate(ctx =>  ctx.Glow());
                    break;
                case "oilpaint":
                    image.Mutate(ctx => ctx.OilPaint());
                    break;
                case "vignette":
                    image.Mutate(ctx => ctx.Vignette());
                    break;
            }
        }

        [HttpGet("{filtertype}/{key}")]
        public async Task Get(string key, string filtertype)
        {
            if (string.IsNullOrEmpty(filtertype))
                filtertype = "None";
            // We should really add some checks for the image type or file extension

            try
            {
                var getResponse = await this.S3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = this.BucketName,
                    Key = key
                });
                MemoryStream ms = new MemoryStream();
                using (Stream responseStream = getResponse.ResponseStream)
                {
                    using (Image<Rgba32> image = Image.Load(responseStream))
                    {
                        FilterImage(image, filtertype);

                        
                        image.SaveAsJpeg(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                    }
                }

                this.Response.ContentType = getResponse.Headers.ContentType;
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