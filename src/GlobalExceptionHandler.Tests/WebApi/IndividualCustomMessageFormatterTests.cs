using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GlobalExceptionHandler.Tests.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace GlobalExceptionHandler.Tests.WebApi
{
    public class IndividualCustomMessageFormatterTests : IClassFixture<WebApiServerFixture>
    {
        private readonly HttpResponseMessage _response;

        public IndividualCustomMessageFormatterTests(WebApiServerFixture fixture)
        {
            // Arrange
            const string requestUri = "/api/badrequest";
            var webHost = fixture.CreateWebHost();

            webHost.Configure(app =>
            {
                app.UseWebApiGlobalExceptionHandler(x =>
                {
                    x.ContentType = "application/json";
                    x.ForException<ArgumentException>().ReturnStatusCode(HttpStatusCode.BadRequest).UsingFormatter(
                        exception => JsonConvert.SerializeObject(new
                        {
                            error = new
                            {
                                message = "Oops, something went wrong"
                            }
                        }));
                    x.MessageFormatter(exception => "This will be overriden");
                });

                app.Map(requestUri, config =>
                {
                    config.Run(context => throw new ArgumentException("An invalid argument supplied"));
                });
            });

            // Act
            var server = new TestServer(webHost);
            using (var client = server.CreateClient())
            {
                var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), requestUri);
                _response = client.SendAsync(requestMessage).Result;
            }
        }

        [Fact]
        public void Should_return_correct_response_type()
        {
            _response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public void Should_return_correct_status_code()
        {
            _response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_override_global_custom_message()
        {
            var content = await _response.Content.ReadAsStringAsync();
            content.ShouldBe("Oops, something went wrong");
        }
    }
}