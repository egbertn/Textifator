using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Textifator
{
	public class CustomWebHostService : BackgroundService
	{
		private readonly ILogger<CustomWebHostService> _logger;
		private CancellationToken CancellationToken;
		private readonly IConnectionMultiplexer _connectionMultiplexer;
		public CustomWebHostService(
			ILogger<CustomWebHostService> logger, IConnectionMultiplexer connectionMultiplexer)
		{
			_logger = logger;
			_connectionMultiplexer = connectionMultiplexer;
		}
		private static string AssemblyName()
		{
			return typeof(Startup).Assembly.GetName().Name;
		}
			protected async override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			//await DbTasks();
			this.CancellationToken = stoppingToken;
			await Task.Run(() =>
			{

				//	CancellationToken = stoppingToken;
				Task.Run(PingRedis);
				_logger.LogInformation("{0} started at: {1}", AssemblyName(), DateTimeOffset.Now);
			});
			try
			{
				await Task.Delay(-1, stoppingToken);
			}
			// we don't want an exception
			catch (TaskCanceledException)
			{

			}
		}

		private async Task PingRedis()
		{
			while(!CancellationToken.IsCancellationRequested)
			{
				await _connectionMultiplexer.GetDatabase().PingAsync();
				await Task.Delay(TimeSpan.FromSeconds(10));

			}
		}

	}
}