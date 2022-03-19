using System;
namespace Textifator.Controllers
{
	public class Medium
	{
		public override bool Equals(object obj)
		{
			if (obj is Medium media)
			{
				return media.GetHashCode() == this.GetHashCode();
			}
			return false;
		}
		public override int GetHashCode()
		{
			return Id;
		}
		public int Id { get; set; }
		public string Name { get; set; }
		public string Ext { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public string MediaType { get; set; }
		/// <summary>
		/// size in bytes
		/// </summary>
		public long Size { get; set; }
		public string HashKeyThumbNail { get; set; }
		//calculated hash for this media
		public string HashKey { get; set; }
		public DateTimeOffset Created {get;set;}

	}
}