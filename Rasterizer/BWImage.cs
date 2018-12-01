using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasterizer
{
	/// <summary>
	/// An image represented by a boolean matrix (black/white)
	/// </summary>
	public class BWImage
	{
		public Size size;
		public bool[] data;

		public BWImage(Size size, bool[] data)
		{
			this.size = size;
			this.data = data;
		}

		public BWImage(Size size)
		{
			this.size = size;
			data = new bool[size.Width * size.Height];
		}

		/// <summary>
		/// Bitmap rendering of the image; true -> #fff, false -> #000
		/// </summary>
		/// <returns></returns>
		public Bitmap GetBitmap()
		{
			var output = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			for(int y = 0; y < size.Height; y++)
			{
				for(int x = 0; x < size.Width; x++)
				{
					int value = data[x + size.Width * y] ? 0 : 255;
					Color c = Color.FromArgb(value, value, value);
					output.SetPixel(x, y, c);
				}
			}
			return output;
		}
	}
}
