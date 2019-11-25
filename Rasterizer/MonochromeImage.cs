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

		public MonochromeImage(Bitmap image, bool visualSpace = true)
		{
			this.size = image.Size;
			this.data = new T[size.Width * size.Height];
			for(int x = 0; x < size.Width; x++)
			{
				for(int y = 0; y < size.Height; y++)
				{
					Color color = image.GetPixel(x, y);
					float gray = visualSpace ? ColorToGray(color) : (color.R + color.G + color.B) / 3;
					this.SetValue(x, y, ColorToGray(image.GetPixel(x, y)));
				}
			}
		}

		/// <summary>
		/// Bitmap rendering of the image
		/// </summary>
		/// <returns></returns>
		public Bitmap GetBitmap(bool visualSpace = true)
		{
			var output = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			for(int x = 0; x < size.Width; x++)
			{
				for(int y = 0; y < size.Height; y++)
				{
					int gray = GetValue(x, y);
					Color color = visualSpace ? ColorFromGray(gray) : Color.FromArgb(gray, gray, gray);
					output.SetPixel(x, y, color);
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

		private static float ColorToGray(Color color)
		{
			float pixelValue = (color.R * 2126 + color.G * 7152 + color.B * 0722)/2550000f;
			float invValue = 1f - pixelValue;
			return (1f - invValue * invValue) * 255;
		}

		private static Color ColorFromGray(int gray)
		{
			var invValue = Math.Sqrt(1f - gray / 255f);
			int value = (int)((1f - invValue) * 255f);
			return Color.FromArgb(value, value, value);
		}
	}
}
