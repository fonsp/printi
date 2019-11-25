using System.Drawing;

namespace Rasterizer
{
	/// <summary>
	/// A halftone image represented by a boolean matrix (true is black, false is white)
	/// </summary>
	public class BWImage : MonochromeImage<bool>
	{
		public BWImage(Size size, bool[] data) : base(size, data){}
		public BWImage(Size size) : base(size){}
		public BWImage(MonochromeImage<bool> other) : base(other){}
		public BWImage(Bitmap image, bool visualSpace = true) : base(image, visualSpace){}

		public override int GetValue(int x, int y)
		{
			return data[x * size.Height + y] ? 0 : 255;
		}

		public override void SetValue(int x, int y, int value)
		{
			data[x * size.Height + y] = value < 128;
		}
	}
}
