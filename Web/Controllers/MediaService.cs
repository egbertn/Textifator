using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Textifator.Controllers
{
	public class MediaService
	{
		private readonly ILogger<MediaService> _logger;
		private readonly WebOptions _options;
		private readonly FormOptions _formOptions;


		private double duration;
		private readonly IConnectionMultiplexer _connection;
		public MediaService(IOptions<WebOptions> options,
			IOptions<FormOptions> formOptions, ILogger<MediaService> logger,
			IConnectionMultiplexer connection)
		{
			_options = options.Value;
			_formOptions = formOptions.Value;
			_logger = logger;
			_connection = connection;
		}


		internal static string MimeMap(string mediaType)
		{
			var result = MimeTypes.TryGetExtension(mediaType, out string extension);
			return result ? extension : null;
		}
		//returns physical file which is uploaded
		public async Task<Medium> AddMediaFile(IFormFile postedFile, CancellationToken cancellationToken = default)
		{
			try
			{
				var name = postedFile.FileName;

				var uploadFolder = _options.MediaStoragePath ?? (Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_data", @"UploadedFiles"));
				var fileLen = postedFile.Length;
				var contentType = postedFile.ContentType;
				string finalPath;
				if (fileLen > 0)
				{
					var fileName = ContentDispositionHeaderValue.Parse(postedFile.ContentDisposition)
						.FileName.Trim('"');

					using var createHash = MD5.Create();
					var bytes = RandomNumberGenerator.GetBytes(32);

					string hash = null;
					if (fileLen < _formOptions.MemoryBufferThreshold) //fast hash otherwise, we'll break memory
					{
						using var memStream = new MemoryStream();

						await postedFile.CopyToAsync(memStream, cancellationToken);
						if (cancellationToken.IsCancellationRequested)
						{
							return null;
						}
						memStream.Position = 0;
						hash = Convert.ToHexString(bytes).ToLowerInvariant();
						if (cancellationToken.IsCancellationRequested)
						{
							return null;
						}
						memStream.Position = 0;
						finalPath = Path.Combine(uploadFolder, string.Concat(hash, MimeMap(contentType)));

						using var fileStream = File.Create(finalPath, (int)postedFile.Length, FileOptions.WriteThrough);
						await memStream.CopyToAsync(fileStream, cancellationToken);
						if (cancellationToken.IsCancellationRequested)
						{
							return null;
						}
					}
					//slower but on disk
					else
					{
						var tempPath = Path.Combine(uploadFolder, $"{new Random().Next(int.MaxValue)}.tmp");

						using (var fileStream = File.Create(tempPath, 4096 * 8, FileOptions.SequentialScan))
						{
							await postedFile.CopyToAsync(fileStream, cancellationToken);
							if (cancellationToken.IsCancellationRequested)
							{
								return null;
							}
							fileStream.Position = 0;
							hash = Convert.ToHexString(await createHash.ComputeHashAsync(fileStream, cancellationToken)).ToLowerInvariant();
							if (cancellationToken.IsCancellationRequested)
							{
								return null;
							}
						}
						finalPath = Path.Combine(uploadFolder, string.Concat(hash, MimeMap(contentType)));
						File.Move(tempPath, finalPath, true);
					}
					string thumbNail = null;
					(int width, int height, double duration) meta = default;
					if (!string.IsNullOrEmpty(finalPath))
					{
						if (contentType.StartsWith("image"))
						{
							thumbNail = await MimeTypeHelper.CreateImageThumbnail(finalPath, 500, cancellationToken);
							var (width, height, mimeType) = await MimeTypeHelper.DetectImageType(finalPath, cancellationToken);
							meta = (width, height, 0F);
						}
						else if (contentType.StartsWith("video"))
						{
							thumbNail = await MimeTypeHelper.CreateThumbNail(finalPath, cancellationToken);
							meta = await MimeTypeHelper.GetResolution(finalPath, cancellationToken);

						}
						if (thumbNail == null)
						{
							_logger.LogError($"cannot create CreateThumbNail for {finalPath} is ffmpeg correctly installed");
						}
					}

					var media = new Medium
					{
						Id = new Random().Next(),
						Size = fileLen,
						Name = name,
						Ext = MimeMap(contentType),
						Height = meta.height,
						Width = meta.width,
						HashKeyThumbNail = thumbNail != null ? Path.GetFileName(thumbNail) : null,
						MediaType = contentType,
						HashKey = hash,

						Created = DateTimeOffset.UtcNow
					};


					return media;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("AddMediaFile failed {0}", ex);
			}
			return null;

		}
		public async Task<int> GetProgress(int fileId)
		{


			var data = await _connection.GetDatabase().StringGetAsync($"textifator:{fileId}");
			if (data.IsNullOrEmpty)
			{
				return -1;
			}
			return (int)data;

		}
		public  int AddSubTitles(Subtitles subtitles, CancellationToken cancellationToken = default)
		{
			int random = new Random().Next();
			var randomNewFile = random + ".mp4";
			string randomFile = Path.Combine(_options.MediaStoragePath, randomNewFile);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Task.Run(async () =>
			{
				var info = new ProcessStartInfo();
				var perc = 0;
				var prevPerc = 0;
				var curDir = Directory.GetCurrentDirectory();
				var srtFile = $"{_options.MediaStoragePath}/{new Random().Next()}.srt";
				await File.WriteAllLinesAsync(srtFile, subtitles.Lines, cancellationToken);
				info.FileName = "ffmpeg";

				info.Arguments = $" -i {Path.Combine(_options.MediaStoragePath, subtitles.Id)}  -vf subtitles={srtFile} {randomFile} -y -progress pipe:1";
				info.UseShellExecute = false;
				info.RedirectStandardInput = true;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				/*
				 *
					frame=12444 fps= 14 q=29.0 size=   35328kB time=00:00:41.33 bitrate=7002.1kbits/s speed=0.469x
					fps=14.11
					stream_0_0_q=29.0
					bitrate=7002.1kbits/s
					total_size=36175920
					out_time_us=41331519
					out_time_ms=41331519
					out_time=00:00:41.331519
					dup_frames=0
					drop_frames=0
					speed=0.469x
					progress=continue
				 */

				using var proc = Process.Start(info);
				proc.EnableRaisingEvents = true;
				proc.Start();
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
				proc.ErrorDataReceived += (s, e) =>
				{
					if (e == null || string.IsNullOrEmpty(e.Data))
					{
						return;
					}
					var stringD = e.Data;
					if (stringD.Contains("Duration:", StringComparison.OrdinalIgnoreCase))
					{//Duration: 00:00:06.34, start: 0.000000, bitrate: 21303 kb/s
						var inv = System.Globalization.NumberFormatInfo.InvariantInfo;
						var parseit = stringD.TrimStart(' ')[10..];
						var lines = parseit.Split(',');
						var time = lines[0];
						var timeparts = time.Split(':').Select(s => float.Parse(s, inv)).ToArray();
						var secs = Math.Floor(timeparts[2]);
						var ms = Math.Floor((timeparts[2] - secs) * 1000);
						var ts = new TimeSpan(0, (int)timeparts[0], (int)timeparts[1], (int)secs, (int)ms);
						duration = ts.TotalMilliseconds;
						_logger.LogInformation("Found duration {0}", duration);
					}
					//Trace.TraceInformation("My error {0}", stringD);
				};
				var baseData = proc.StandardOutput.BaseStream;
			
				proc.OutputDataReceived += async (s, e) =>
				{
					if (e == null || string.IsNullOrEmpty(e.Data))
					{
						return;
					}
					var stringD = e.Data; 
					
					if(stringD.StartsWith("progress"))
					{
						var splits = stringD.Split('=');
						if (splits[1] == "end")
						{
							perc = 100;
						}
					}
					if (perc != 100 && stringD.StartsWith("out_time_us"))
					{
						var splits = stringD.Split('=');
						var current = long.Parse(splits[1]) / 1000;
						//todo use Redis
						perc = (int)Math.Floor((current / duration) * 100);
					}
					if (perc != prevPerc)
					{
						prevPerc = perc;
						var key = $"textifator:{random}";
						_logger.LogInformation("{0}% key={1}", perc, key);
						await _connection.GetDatabase().StringSetAsync(key, perc, expiry: TimeSpan.FromHours(24), flags: CommandFlags.FireAndForget).ConfigureAwait(false);
					}
				};


				await proc.WaitForExitAsync(cancellationToken);
			});
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			return random;


		}
	
	}
}
