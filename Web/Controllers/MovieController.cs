using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Textifator.Controllers
{

	[ApiController]
	//[EnableCors]
	// [Authorize(AuthenticationSchemes = Helpers.BasicAuthenticationHandler.AuthenticationScheme)]
	[Route("[controller]")]

	public class MovieController : ControllerBase
	{
		private readonly ILogger<MovieController> _logger;
		private readonly MediaService _mediaService;
		public MovieController(ILogger<MovieController> logger, MediaService mediaService)
		{
			_mediaService = mediaService;
			_logger = logger;
		}
		private CancellationToken GetCancellation()
		{
			return HttpContext.RequestAborted;
		}
		[HttpPost("Burn")]
		public  IActionResult ProduceMovieWithSubitles([FromBody] Subtitles subtitles)
		{
			var file =  _mediaService.AddSubTitles(subtitles, GetCancellation());
			return Ok(new FileResponse { File = file });

		}
		[HttpGet("Progress/{id}")]
		public async Task<IActionResult> GetProgress(int id)
		{
			var perc = await _mediaService.GetProgress(id);
			return Ok(new ProgressResponse{ Perc = perc });
		}
			/// <summary>
		/// Needs multipart/form-data
		/// bundleid=id of existing bundleid (optional)
		/// file=base content-disposition attachment can be basically any file.
		/// </summary>
		[HttpPost("UploadMedia")]
		[DisableRequestSizeLimit]
		[ProducesResponseType(typeof(Medium), StatusCodes.Status201Created )]
		public async Task<IActionResult> UploadMedia()
		{
			try
			{
				// 1. get the file form the request
				var postedFile = Request.Form.Files[0];
				var name = postedFile.FileName;
			 	var media = await _mediaService.AddMediaFile(postedFile, GetCancellation());

				return Created($"/DownloadedFiles/{string.Concat(media.HashKey, media.Ext)}", media);

			}
			catch (Exception ex)
			{
				_logger.LogError("UploadMedia {0}", ex);
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}
	}
}