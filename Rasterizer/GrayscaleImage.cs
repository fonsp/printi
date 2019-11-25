namespace Rasterizer
{
	/// <summary>
	/// A grayscale image represented by a floating-point matrix (0 is black, 255 is white)
	/// </summary>
	public class GrayscaleImage : MonochromeImage<float>
	{
		public int GetValue(int x, int y)
		{
			return Math.Clamp((int)data[x * size.Width + y], 0, 255);
		}

		public void SetValue(int x, int y, int value)
		{
			data[x * size.Width + y] = value;
		}

		public void SetValue(int x, int y, float value)
		{
			data[x * size.Width + y] = value;
		}
	}
}
