using System;
using System.Drawing;

namespace Rasterizer
{
	/// <summary>
	/// A grayscale image represented by a floating-point matrix (0 is black, 255 is white)
	/// </summary>
	public class GrayscaleImage : MonochromeImage<float>
	{
		public GrayscaleImage(Size size, float[] data) : base(size, data){}
		public GrayscaleImage(Size size) : base(size){}
		public GrayscaleImage(MonochromeImage<float> other) : base(other){}
		public GrayscaleImage(Bitmap image, bool visualSpace = true) : base(image, visualSpace){}

		public override int GetValue(int x, int y)
		{
			return Math.Clamp((int)data[x * size.Height + y], 0, 255);
		}

		public override void SetValue(int x, int y, int value)
		{
			data[x * size.Height + y] = value;
		}

		public override void SetValue(int x, int y, float value)
		{
			data[x * size.Height + y] = value;
		}

		public GrayscaleImage Blurred(float factor)
		{
			var smaller = ResizeBitmap(GetBitmap(false), (int)(size.Width / factor), (int)(size.Height / factor));
			var blurred = ResizeBitmap(smaller, size.Width, size.Height);
			return new GrayscaleImage(blurred, false);
		}

		private static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
		{
			var result = new Bitmap(width, height);
			using (Graphics g = Graphics.FromImage(result))
			{
				g.DrawImage(bmp, 0, 0, width, height);
			}
			return result;
		}
	}
}
