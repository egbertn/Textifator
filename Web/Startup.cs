using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.IO;
using Textifator.Controllers;

namespace Textifator
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }
		//hack to re-register CustomWebHostService. DO NOT USE otherwise
		public static IServiceCollection Services;
		public void ConfigureServices(IServiceCollection services)
		{
			Services = services;
			//services.AddApplicationInsightsTelemetry();
			var corsHosts = Configuration.GetValue<string>("CorsOrigins");
			services.AddCors(options =>
			{
				options.AddDefaultPolicy(
					builder =>
					{
						if (corsHosts == "*")
						{
							builder.AllowAnyOrigin();
						}
						else
						{
							builder.WithOrigins(corsHosts.Split(';'));
						}
						builder.SetIsOriginAllowedToAllowWildcardSubdomains()
						.AllowAnyHeader().
								AllowAnyMethod().
								AllowCredentials();
					});
			});
			services.AddOptions();
			services.Configure<FormOptions>(options => {
				// the max length of individual form values
				options.ValueLengthLimit = 1024;
				// length of the each multipart body
				options.MultipartBodyLengthLimit = 500 * 1000000;
				// this is used for buddering the form body into the memory
				options.MemoryBufferThreshold = 100 * 1000000; //100 mb
			});
			services.Configure<WebOptions>(Configuration.GetSection(nameof(WebOptions)));
			services.AddControllers();
			services.AddAuthorization();
			services.AddTransient<Controllers.MediaService>();
			var config = new DistributedCacheConfig();
			Configuration.GetSection("RedisOptions").Bind(config);


			services.AddSingleton<IConnectionMultiplexer>((e)=>
			{
				var logger = e.GetRequiredService<ILogger<MediaService>>();

				logger.LogInformation($"using redis {config.Endpoint}:{config.EndpointPort}");
				var redisConfig = new ConfigurationOptions
				{
					ConnectRetry = config.ConnectRetry,
					DefaultDatabase = config.DefaultDatabase,
					EndPoints = { { config.Endpoint, config.EndpointPort } },
					Ssl = config.Ssl,
					Password = config.Password,
					ReconnectRetryPolicy = new LinearRetry(2000),
					AbortOnConnectFail = false,
					ConnectTimeout = config.ConnectTimeout
				};
				return ConnectionMultiplexer.Connect(redisConfig);
			});
			var logPath = Configuration.GetValue<string>("Logging:UseFileLogging");
			// if (!string.IsNullOrEmpty(logPath))
			// {
			// 	var serilogLogger = new LoggerConfiguration()
			// 	 .WriteTo.File($"{logPath}/{this.GetType().Assembly.GetName().Name}.log")
			// 	 .CreateLogger();
			// 	services.AddLogging(builder =>
			// 	{
			// 		builder.SetMinimumLevel(LogLevel.Information);
			// 		builder.AddSerilog(logger: serilogLogger, dispose: true);
			// 	});
			// }


			// In production, the React files will be served from this directory
			services.AddSpaStaticFiles(configuration =>
			{
				configuration.RootPath = "ClientApp/build";
			});

			//ConfigureRedisCache(services);
		}
		private static string ExpandPath(string virtPathCheck)
		{
			if (string.IsNullOrEmpty(virtPathCheck))
			{
				return null;
			}
			if (virtPathCheck.StartsWith('.'))
			{
				return Path.GetFullPath(virtPathCheck);
			}
			return virtPathCheck; // is fully qualified already
		}
		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
				//app.UseHttpsRedirection();
			}

			app.UseStaticFiles();
			var webOptions = new WebOptions();
			Configuration.GetSection(nameof(WebOptions)).Bind(webOptions);
			// DownloadedFiles will contain media that will be displayed in a
			// DisplayUnit (kiosk)
			//with a direct file name it could be downloaded this way.
			// eg. http://localhost/DownloadedFiles/hash.jpg
			var uploadedPath = ExpandPath( webOptions.MediaStoragePath) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_data", @"DownloadedFiles");
			Directory.CreateDirectory(uploadedPath);

			app.UseStaticFiles(new StaticFileOptions()
			{
				FileProvider = new PhysicalFileProvider(uploadedPath),
				RequestPath = new PathString("/DownloadedFiles")
			});
			app.UseSpaStaticFiles(new StaticFileOptions {
				OnPrepareResponse = ctx =>
				{
					ctx.Context.Response.Headers.Add(
						 "Cache-Control", $"private, max-age={TimeSpan.FromMinutes(5).TotalSeconds}");
				}
			});

			app.UseRouting();
			//app.UseCors();

			app.UseAuthorization();
			app.UseAuthentication();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
			//app.UseRootRewrite();
			app.UseSpa(spa =>
			{
				spa.Options.SourcePath = "ClientApp";

				if (env.IsDevelopment())
				{
					spa.UseReactDevelopmentServer(npmScript: "start");
				}
			});
		}
	}
}