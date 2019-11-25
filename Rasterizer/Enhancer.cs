using System;
using System.Drawing;
using System.Collections.Generic;


namespace Rasterizer
{
	public interface IEnhancer
	{
		/// <summary>
		/// Apply an image enhancement algorithm on a grayscale image.
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		GrayscaleImage Enhance(GrayscaleImage image);
	}

	public class HDREnhancer : IEnhancer
	{
		public readonly float factor;
		public readonly float numSegments;

		public HDREnhancer(float factor, float numSegments=4)
		{
			this.factor = factor;
			this.numSegments = numSegments;
		}

		public GrayscaleImage Enhance(GrayscaleImage image)
		{
			float blurFactor = Math.Min(image.size.Width, image.size.Height) / this.numSegments;
			var blurred = image.Blurred(blurFactor);
			var result = new GrayscaleImage(image.size);
			for (int x = 0; x < image.size.Width; x++)
			{
				for (int y = 0; y < image.size.Height; y++)
				{
					result.SetValue(x, y, Lerp(image.GetValue(x, y), blurred.GetValue(x, y), -this.factor));
				}
			}
			return result;
		}

		private float Lerp(float a, float b, float t)
		{
			return a * (1f - t) + b * t;
		}
	}

	public class QuantileEnhancer : IEnhancer
	{
		public GrayscaleImage Enhance(GrayscaleImage image)
		{
			var pixelValues = new List<float>(image.data);
			pixelValues.Sort();
			float[] mapping = new float[256];
			for (int i = 0; i < pixelValues.Count; i++)
			{
				mapping[(int)pixelValues[i]] = (int)(i * 255f / pixelValues.Count);
			}

			GrayscaleImage result = new GrayscaleImage(image.size);
			for (int x = 0; x < result.size.Width; x++)
			{
				for (int y = 0; y < result.size.Height; y++)
				{
					var newValue = mapping[(int)image.GetValue(x, y)];
					result.SetValue(x, y, newValue);
				}
			}
			return result;
		}
	}
}
