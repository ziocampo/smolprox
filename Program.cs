using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Yarp.ReverseProxy.Forwarder;
using NLog.Web;

Parser
  .Default
  .ParseArguments<CommandLineOptions>(args)
  .WithParsed(o =>
  {

      var webhost = WebHost
                    .CreateDefaultBuilder()
                    .UseKestrel(
                        options =>
                        {
                            options.AddServerHeader = false;
                            options.ListenAnyIP(o.Listen);
                            options.AllowSynchronousIO = true;
                        }
                    )
                    .ConfigureServices((IServiceCollection services) => {
                        services.AddHttpContextAccessor();
                        services.AddHttpLogging(options => {
                            options.LoggingFields = HttpLoggingFields.All;
                        });
                        services.AddHttpForwarder();
                    })
                    .Configure((IApplicationBuilder app) => {

                        IHttpForwarder forwarder = app.ApplicationServices.GetRequiredService<IHttpForwarder>();

                        app.UseRouting();

                        app.UseHttpLogging();
                        app.UseForwardedHeaders(new ForwardedHeadersOptions
                        {
                            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                        });

                        var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
                        {
                            UseProxy = false,
                            AllowAutoRedirect = false,
                            AutomaticDecompression = DecompressionMethods.None,
                            UseCookies = false
                        });
                        var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };
                        var remoteBaseUri = $"http{(o.Https ? "s": string.Empty)}://{o.Host}:{o.Port}";
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.Map($"/{{**catch-all}}", async httpContext =>
                            {
                                var error = await forwarder.SendAsync(httpContext, remoteBaseUri, httpClient, requestOptions);
                                if (error != ForwarderError.None)
                                {
                                    var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
                                    var exception = errorFeature.Exception;
                                }
                            });

                        });
                    })
                    .UseNLog()
                    .Build();

      webhost.Run();

  });

