using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnicornImages.API.Tests
{
    public class FilterControllerTest
    {
        string BucketName { get; set; }
        IAmazonS3 S3Client { get; set; }

        IConfigurationRoot Configuration { get; set; }


        public FilterControllerTest()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            this.Configuration = builder.Build();

            // Use the region and possible profile specified in the appsettings.json file to construct an Amaozn S3 service client.
            this.S3Client = Configuration.GetAWSOptions().CreateServiceClient<IAmazonS3>();

            // Create a bucket used for the test which will be deleted along with any data in the bucket once the test is complete.
            this.BucketName = "unicornimages-api-bucket-17b5w25fk1v3v";
           
        }

        [Fact]
        public async Task TestSuccessWorkFlow()
        {
         
            Startup.Configuration[Startup.AppS3BucketKey] = this.BucketName;
            var lambdaFunction = new LambdaEntryPoint();

            var requestStr = File.ReadAllText("./SampleRequests/ValuesController-Get.json");
            var request = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(requestStr);
            var context = new TestLambdaContext();
            var response = await lambdaFunction.FunctionHandlerAsync(request, context);
        }
        }
}
