
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace Textifator.Controllers
{
	public static class MimeTypeHelper
	{
		public async static Task<(int width, int height, string mimeType)> DetectImageType(string fileName, CancellationToken cancellationToken = default)
		{
			var blob = await File.ReadAllBytesAsync(fileName, cancellationToken);
			return await blob.DetectImageType(cancellationToken);
		}
		/// <summary>
		/// detects image type from binary data
		/// </summary>
		/// <param name="blob">must be supplied</param>
		public async static Task<(int width, int height, string mimeType)> DetectImageType(this byte[] blob, CancellationToken  cancellationToken= default)
		{
			if (blob == null)
			{
				throw new ArgumentNullException(nameof(blob));
			}
			if (blob.Length > 20000000)
			{
				return (0, 0, "application/octet");
			}
			var config = new Configuration();

			var formatsDetectionsWanted = new IImageFormatDetector[] { new JpegImageFormatDetector(), new PngImageFormatDetector(), new SixLabors.ImageSharp.Formats.Gif.GifImageFormatDetector(), new SixLabors.ImageSharp.Formats.Tga.TgaImageFormatDetector() };
			var decodersWanted = new (IImageDecoder decoder, IImageFormat format)[] {
				(new JpegDecoder(), JpegFormat.Instance),
				(new PngDecoder(), PngFormat.Instance),
				(new GifDecoder(), GifFormat.Instance),
				(new TgaDecoder(), TgaFormat.Instance) };

			IImageFormat detected = default;
			foreach (var fmt in formatsDetectionsWanted)
			{
				config.ImageFormatsManager.AddImageFormatDetector(fmt);
				if (detected == null)
				{
					detected = fmt.DetectFormat(blob);
				}
			}
			foreach (var (decoder, format) in decodersWanted)
			{
				config.ImageFormatsManager.SetDecoder(format, decoder);
			}
			if (detected != null)
			{
				var image = await Image.LoadAsync(config, new MemoryStream(blob), cancellationToken);

				return (image.Width, image.Height, detected.DefaultMimeType);
			}
			return default;
		}
		public static async Task<(int width, int height, double duration)> GetResolution(string mediaFile, CancellationToken cancellationToken = default)
		{
			//ffprobe -v error -show_entries stream=width,height -of default=noprint_wrappers=1  VID_20210308_140325.mp4

			if (!File.Exists(mediaFile))
			{
				throw new FileNotFoundException($"{mediaFile} not found");
			}
			using var proc = new Process();

			proc.StartInfo.FileName = "ffprobe";

			proc.StartInfo.Arguments = $"-v error -show_entries stream=width,height,duration -of json {mediaFile}";
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardInput = true;
			proc.StartInfo.RedirectStandardOutput = true;
		
			proc.Start();
			try
			{
				await proc.WaitForExitAsync(cancellationToken);
				//
				// Read in all the text from the process with the StreamReader.
				//
				using var reader = proc.StandardOutput;
				var meta = await JsonSerializer.DeserializeAsync<MediaFileMeta>(reader.BaseStream, new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
				var streamInfo = meta.Streams?.FirstOrDefault();
				if (streamInfo != null)
				{
					return (streamInfo.Width, streamInfo.Height, streamInfo.Duration);
				}

			}
			catch (System.ComponentModel.Win32Exception ex) when (ex.Message == "The system cannot find the file specified")
			{
				return default;
			}
			catch (Exception ex)
			{
				Trace.TraceError("GetResolution failed with {0}", ex.Message);
				throw;
			}
			return default;
		}

		
		/// <summary>
		/// uses ffmpeg using a shell command
		/// Note that this tool needs to be installed on linux
		/// using apt install ffmpeg
		/// On windows choco install ffmpeg does the same job
		/// </summary>
		/// <param name="mediaFile"></param>
		/// <param name="cancelation"></param>
		public static async Task<string> CreateThumbNail(string mediaFile, CancellationToken cancelation = default)
		{
			var path = Path.GetDirectoryName(mediaFile);
			var fileName = Path.GetFileNameWithoutExtension(mediaFile);
			var newFile = Path.Combine(path, fileName+ ".thumb.jpeg");
			using var proc = new Process();

			proc.StartInfo.FileName = "ffmpeg";
			//ffmpeg -i inputvideo.mp4 -ss 00:00:03 -frames:v 1 foobar.jpeg
			proc.StartInfo.Arguments = $"-i {mediaFile} -ss 00:00:03 -qscale:v 4 -frames:v 1 -vf \"scale=iw*.5:ih*.5\" -y {newFile} ";
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardInput = true;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.Start();
			try
			{
				await proc.WaitForExitAsync(cancelation);
			}
			catch (System.ComponentModel.Win32Exception ex) when (ex.Message == "The system cannot find the file specified")
			{
				return null;
			}
			catch
			{
				throw;

			}
			return newFile;
		}
		/// <summary>
		/// packs an image to inline data: format
		/// data:image/png;base64,...[content]
		/// </summary>
		/// <param name="blob"></param>
		public static async Task<string> ImageInlineEncode(this byte[] blob)
		{
			if (blob == null)
			{
				throw new ArgumentNullException(nameof(blob));
			}
			var (_,_, imageType) = await blob.DetectImageType();
			return string.Concat("data:", imageType, ";base64,", Convert.ToBase64String(blob));
		}
		/// <summary>
		/// Decodes a base64 encoded string to a byte array and a mimeType
		/// </summary>
		/// <param name="content">a valid base64 inlined image of type png/gif/jpg etc</param>
		/// <returns></returns>
		public static (byte[] content, string mimeType) ImageInlineDecode(this string content)
		{
			if (string.IsNullOrEmpty(content))
			{
				throw new ArgumentNullException(nameof(content));
			}
			var parts = content.Split(';');

			string mimeType;
			if (parts.Length == 2)
			{
				if (parts[0].StartsWith("data:"))
				{
					mimeType = parts[0].Split("data:").Last();
					if (parts[1].StartsWith("base64", StringComparison.CurrentCultureIgnoreCase))
					{
						return (Convert.FromBase64String(parts[1].Split(',').Last()), mimeType);
					}
				}
			}

			return default;
		}

		public async static Task<string> CreateImageThumbnail(string finalPath, int newWidth,  CancellationToken cancellationToken)
		{
			var path = Path.GetDirectoryName(finalPath);
			var fileName = Path.GetFileNameWithoutExtension(finalPath);
			var newFile = Path.Combine(path, fileName + ".thumb.png");

			using var image = await Image.LoadAsync(finalPath, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return null;
			}
			//aspect is kept
			image.Mutate(i => i.Resize(newWidth,0));
			using var newImage = File.Create(newFile, 4096, FileOptions.WriteThrough);
			await image.SaveAsync(newImage, PngFormat.Instance, cancellationToken);
			return newFile;
		}
	}

}