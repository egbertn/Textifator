using System.Collections.Generic;

namespace Textifator
{
/*
	 * {
    "programs": [

    ],
    "streams": [
        {
            "width": 1920,
            "height": 1080,
            "side_data_list": [
                {

                }
            ]
        },
        {

        }
    ]
}
	 * */
	/// <summary>
	///
	/// </summary>
	public class MediaFileMeta
	{
		public IEnumerable<Stream> Streams { get; set; }
	}
    public class Stream
	{
        public int Width { get; set; }
        public int Height { get; set; }
        public double Duration {get;set;}
	}
}