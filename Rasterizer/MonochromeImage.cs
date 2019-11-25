using System;
using System.Drawing;

namespace Rasterizer
{
	/// <summary>
	/// A grayscale image represented by a matrix
	/// </summary>
	public abstract class MonochromeImage<T> where T : new()
	{
		public Size size;
		public T[] data;

		public MonochromeImage(Size size, T[] data)
		{
			this.size = size;
			this.data = data;
		}

		public MonochromeImage(Size size)
		{
			this.size = size;
			this.data = new T[size.Width * size.Height];
		}

		public MonochromeImage(MonochromeImage<T> other)
		{
			this.size = other.size;
			this.data = new T[size.Width * size.Height];
			for (int x = 0; x < this.size.Width; x++)
			{
				for (int y = 0; y < this.size.Height; y++)
				{
					this.data[x * this.size.Width + y] = other.data[x * this.size.Width + y];
				}
			}
		}

		public MonochromeImage(Bitmap image)
		{
			this.size = image.Size;
			this.data = new T[size.Width * size.Height];
			for(int y = 0; y < this.size.Height; y++)
			{
				for(int x = 0; x < this.size.Width; x++)
				{
					Color c = image.GetPixel(x, y);
					float pixelValue = (c.R * 2126 + c.G * 7152 + c.B * 0722)/2550000f;
					float invValue = 1f - pixelValue;
					this.SetValue(x, y, (1f - invValue * invValue) * 255);
				}
			}
		}

		/// <summary>
		/// Bitmap rendering of the image
		/// </summary>
		/// <returns></returns>
		public Bitmap GetBitmap()
		{
			var output = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			for(int y = 0; y < size.Height; y++)
			{
				for(int x = 0; x < size.Width; x++)
				{
					int value = (int)(Math.Sqrt(1f - GetValue(x, y) / 255f) * 255f);
					Color c = Color.FromArgb(value, value, value);
					output.SetPixel(x, y, c);
				}
			}
			return output;
		}

		public abstract int GetValue(int x, int y);
		public abstract void SetValue(int x, int y, int value);

		public virtual void SetValue(int x, int y, float value)
		{
			SetValue(x, y, (int)value);
		}
	}
}
