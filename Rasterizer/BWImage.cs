namespace Rasterizer
{
	/// <summary>
	/// A halftone image represented by a boolean matrix (true is black, false is white)
	/// </summary>
	public class BWImage : MonochromeImage<bool>
	{
		public int GetValue(int x, int y)
		{
			return data[x * size.Width + y] ? 0 : 255;
		}

		public void SetValue(int x, int y, int value)
		{
			data[x * size.Width + y] = value < 128;
		}
	}
}
