using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasterizer
{
	public interface IDitherer
	{
		/// <summary>
		/// Apply the dithering algorithm on an image.
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		BWImage GetBWImage(Bitmap image);
	}

	public class BurkesDitherer : IDitherer
	{
		public byte threshold;
		private Size size;
		private byte[,] data;

		public BurkesDitherer(byte threshold = 96)
		{
			this.threshold = threshold;
		}

		private static byte ClipToByte(int x)
		{
			if(x < 0)
			{
				return 0;
			}
			if(x > 255)
			{
				return 255;
			}
			return (byte)x;
		}

		// Based on the Burkes implementation by Cyotek: https://github.com/cyotek/Dithering/

		private static readonly byte[] BurkesDistribution = new byte[] { 0, 8, 4, 2, 4, 8, 4, 2 };


		public BWImage GetBWImage(Bitmap image)
		{
			size = image.Size;
			size.Width = (size.Width / 8) * 8;
			BWImage output = new BWImage(size);
			data = new byte[size.Width, size.Height];
			for(int y = 0; y < size.Height; y++)
			{
				for(int x = 0; x < size.Width; x++)
				{
					Color c = image.GetPixel(x, y);
					data[x, y] = (byte)((c.R * 2126 + c.G * 7152 + c.B * 0722) / 10000);
				}
			}

			for(int y = 0; y < size.Height; y++)
			{
				for(int x = 0; x < size.Width; x++)
				{
					byte value = data[x, y];
					bool thresholded = value > threshold;
					int error = thresholded ? value - 255 : value;

					int shifty = 0;
					for(int shiftx = 1; shifty < 2; shiftx++)
					{
						int newx = x + shiftx;
						if(newx >= 0 && newx < size.Width)
						{
							int newy = y + shifty;
							if(newy < size.Height)
							{
								data[newx, newy] = ClipToByte(data[newx, newy] + ((BurkesDistribution[shiftx + 5 * shifty] * error) >> 5));
							}
						}

						if(shiftx == 2)
						{
							shiftx = -3;
							shifty++;
						}
					}

					output.data[x + size.Width * y] = thresholded;
				}
			}
			return output;
		}
	}
}
