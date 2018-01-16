using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class HeaderTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private string _downstreamPath;

        public HeaderTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_transform_upstream_header()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            DownstreamPort = 51879,
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            UpstreamHeaderTransform = new Dictionary<string,string>
                            {
                                {"Laz", "D, GP"}
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", "/", 200, "Laz"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenIAddAHeader("Laz", "D"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("GP"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _builder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   
                        if(context.Request.Headers.TryGetValue(responseBody, out var values))
                        {
                            var result = values.First();
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(result);
                        }   
                    });
                })
                .Build();

            _builder.Start();
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPath)
        {
            _downstreamPath.ShouldBe(expectedDownstreamPath);
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}