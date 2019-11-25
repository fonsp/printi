using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasterizer
{
	public class RasterPrinter
	{
		private int pageWidth;

		/// <summary>
		/// Number of dots per line that the printer can output.
		/// </summary>
		/// <param name="pageWidth">384 for the 58mm printi</param>
		public RasterPrinter(int pageWidth = 384)
		{
			this.pageWidth = pageWidth;
		}

		/// <summary>
		/// Returns an array of bytes to be sent to the /dev/usb/lp0 device. Will rotate and scale the image before dithering.
		/// </summary>
		/// <param name="inputImage"></param>
		/// <param name="ditherer">The ditherer to be used. Use new BurkesDitherer() if unsure.</param>
		/// <param name="rotateForLargerPrint">If true, the image will be rotated 90 degrees if that would increase the printed size.</param>
		/// <param name="enhancers">Array of image enhancers to use - they will be applied in this order.</param>
		/// <returns></returns>
		public byte[] ImageToPrintCommands(Bitmap inputImage, IDitherer ditherer, bool rotateForLargerPrint = true, IEnhancer[] enhancers = null)
		{
			var resized = ScaleToFitPage(inputImage, rotateForLargerPrint);
			var enhanced = new GrayscaleImage(resized);
			if (enhancers != null)
			{
				foreach (IEnhancer enhancer in enhancers)
				{
					enhanced = enhancer.Enhance(enhanced);
				}
			}
			BWImage result = ditherer.GetBWImage(enhanced);
			byte[] printCommands = RasterToPrintCommands(result);
			return printCommands;
		}

		/// <summary>
		/// Possibly downscale and possibly rotate a bitmap to guarantee that it fits the page.
		/// </summary>
		/// <param name="inputImage"></param>
		/// <param name="rotateForLargerPrint"></param>
		/// <returns></returns>
		public Bitmap ScaleToFitPage(Bitmap inputImage, bool rotateForLargerPrint = true)
		{
			Size originalSize = inputImage.Size;
			Size newSize = originalSize;
			bool rotate = false;
			if(originalSize.Width > originalSize.Height && rotateForLargerPrint)
			{
				if(originalSize.Height > pageWidth)
				{
					newSize.Width = pageWidth * originalSize.Width / originalSize.Height;
					newSize.Height = pageWidth;
					rotate = true;
				}
				else if(originalSize.Width > pageWidth)
				{
					rotate = true;
				}
			}
			else
			{
				if(originalSize.Width > pageWidth)
				{
					newSize.Width = pageWidth;
					newSize.Height = pageWidth * originalSize.Height / originalSize.Width;
				}
				else if(originalSize.Height <= pageWidth) // save paper for very small images
				{
					rotate = true;
				}
			}

			Bitmap resized = new Bitmap(inputImage, newSize);
			if(rotate)
			{
				resized.RotateFlip(RotateFlipType.Rotate90FlipNone);
			}
			return resized;
		}

		/// <summary>
		/// Returns an array of bytes to be sent to the /dev/usb/lp0 device.
		/// </summary>
		/// <param name="raster"></param>
		/// <returns></returns>
		public byte[] RasterToPrintCommands(BWImage raster)
		{

			int width = raster.size.Width;
			int height = raster.size.Height;
			int dotsPerLine = Math.Min(width, pageWidth);
			int bytesPerLine = dotsPerLine / 8;
			if(dotsPerLine != bytesPerLine * 8)
			{
				throw new InvalidDataException("raster width should be a multiple of 8");
			}

			List<byte> output = new List<byte>(bytesPerLine * height + 1);

			IEnumerable<bool> rasterData;
			int byteCount = raster.data.Length / 8;

			if(raster.data.Length % 8 != 0)
			{
				int needed = 8 - (raster.data.Length % 8);
				var rasterDataList = raster.data.ToList();
				for(int i = 0; i < needed; i++)
				{
					rasterDataList.Add(false);
				}
				rasterData = rasterDataList.AsEnumerable().Reverse();
				byteCount++;
			}
			else
			{
				rasterData = raster.data.Reverse();
			}

			BitArray compactedBits = new BitArray(rasterData.ToArray());
			byte[] compactedByteBufferArray = new byte[byteCount];
			compactedBits.CopyTo(compactedByteBufferArray, 0);

			var compactedByteBuffer = compactedByteBufferArray.Reverse();

			output.AddRange(new byte[] { 0x1b, 0x40 });
			for(int y = 0; y < height; y += 24)
			{
				int sliceHeight = Math.Min(24, height - y);

				output.AddRange(new byte[] { 0x1d, 0x76, 0x30, 0x00 });
				output.Add((byte)(bytesPerLine % 256));
				output.Add((byte)(bytesPerLine / 256));
				output.Add((byte)(sliceHeight % 256));
				output.Add((byte)(sliceHeight / 256));

				output.AddRange(compactedByteBuffer.Skip(y * bytesPerLine).Take(bytesPerLine * sliceHeight));

				output.AddRange(new byte[] { 0x1b, 0x4a, 0x15 });
			}

			output.AddRange(new byte[] { 0x1b, 0x40 });

			return output.ToArray();
		}
	}
}
