using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;
using Textifator;
using Textifator.Controllers;
using Xunit;

namespace TestProject1
{
	public class UnitTest1
	{
		[Fact]
		public async Task Test1()
		{
			var options = new WebOptions {MediaStoragePath= "./app_data/media" };

			var subs = new Subtitles { Id = "0c86a6a1f91a3f806095b165b7df6673.mp4", Lines = new[] { "1", "00:00:00,100 --> 00:00:05,000", "Welkom in deze wereld!" } };
			var mockRedis = new Mock<IConnectionMultiplexer>();
			var modkDb = new Mock<IDatabase>();
			modkDb.Setup(s => s.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(),
				It.IsAny<When>(), It.IsAny<CommandFlags>()));
			modkDb.Setup(s => s.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync((RedisValue)10);
			mockRedis.Setup(m => m.GetDatabase(-1, null)).Returns(modkDb.Object);
			var mediaService = new MediaService(Options.Create(options),Options.Create( new FormOptions() ), new NullLogger<MediaService>(), mockRedis.Object);
			var str = mediaService.AddSubTitles(subs, CancellationToken.None);

		}
	}
}
