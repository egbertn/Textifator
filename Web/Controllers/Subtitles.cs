using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Textifator.Controllers
{
	public class Subtitles
	{
		//the movie
		public string Id { get; set; }
		public ICollection<string> Lines { get; set; }

	}
	public class CSRLine
	{
		public string TimeLine { get; set; }
		public string[] Subtitles { get; set; }
	}

}
