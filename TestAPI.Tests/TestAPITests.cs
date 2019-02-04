using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

using TestAPI;

namespace TestAPI.Tests
{
    public class TestAPITests
    {
        public TestAPITests()
        {
        }

        [Fact]
        public void TestFunctionHandlerMethod()
        {
            var function = new Function();
            var request = new APIGatewayProxyRequest();
            var context = new TestLambdaContext();
            
            var response = function.FunctionHandler(request, context);
            
            Assert.Equal(200, response.StatusCode);
            Assert.StartsWith("{\"message\":\"hello world\",\"location\":", response.Body);
        }
    }
}
