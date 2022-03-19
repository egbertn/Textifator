using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Textifator
{
		public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				})
			
			 .ConfigureAppConfiguration((_, builder) =>
			 {
				 //var isService = Environment.GetFolderPath(Environment.SpecialFolder.System);
				 var curDir = Directory.GetCurrentDirectory();
				 var baseDir = AppContext.BaseDirectory;
				 if (!curDir.Equals(baseDir, StringComparison.OrdinalIgnoreCase))
				 {
					 curDir = baseDir.TrimEnd(Path.DirectorySeparatorChar);
					 Console.WriteLine("using {0} as base", curDir);
				 }
				 builder.SetBasePath(curDir);
			 })
			.ConfigureWebHost(host =>
			{
				host.ConfigureKestrel(options =>
				{
					options.AddServerHeader = false;

					//var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localhost.pfx");

					options.ListenAnyIP(5999); //http
				});
			})
			.ConfigureServices((hostContext, services) =>
			{
				//add hosted service 'works' but it fails
				// regarding the Custom CancellationToken
				services.AddHostedService<CustomWebHostService>();
			}).UseConsoleLifetime();

	}

}