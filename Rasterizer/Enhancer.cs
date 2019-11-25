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

	public class HDREnhancer : BlurBasedEnhancer
	{
		public readonly float factor;

		public HDREnhancer(float factor, float numSegments = 4) : base(numSegments)
		{
			this.factor = factor;
		}

		protected override float EnhancePixel(float sharp, float blurred)
		{
			return sharp + (127 - blurred) * factor;
		}
	}

	public class ContrastEnhancer : BlurBasedEnhancer
	{
		public readonly float factor;

		public ContrastEnhancer(float factor, float numSegments = 4) : base(numSegments)
		{
			this.factor = factor;
		}

		protected override float EnhancePixel(float sharp, float blurred)
		{
			return sharp * (1f + factor) - blurred * factor;
		}
	}

	public abstract class BlurBasedEnhancer : IEnhancer
	{
		public readonly float numSegments;

		public BlurBasedEnhancer(float numSegments=4)
		{
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
					result.SetValue(x, y, EnhancePixel(image.GetValue(x, y), blurred.GetValue(x, y)));
				}
			}
			return result;
		}

		protected abstract float EnhancePixel(float sharp, float blurred);
	}

	public class QuantileEnhancer : IEnhancer
	{
		public readonly float factor;

		public QuantileEnhancer(float factor)
		{
			this.factor = factor;
		}

		public GrayscaleImage Enhance(GrayscaleImage image)
		{
			var pixelValues = new List<float>(image.data);
			pixelValues.Sort();
			float[] mapping = new float[256];
			for (int i = 0; i < pixelValues.Count; i++)
			{
				mapping[(int)Math.Clamp(pixelValues[i], 0, 255)] = (int)(i * 255.5f / pixelValues.Count);
			}

			GrayscaleImage result = new GrayscaleImage(image.size);
			for (int x = 0; x < result.size.Width; x++)
			{
				for (int y = 0; y < result.size.Height; y++)
				{
					var mappedValue = mapping[(int)image.GetValue(x, y)];
					var newValue = image.GetValue(x, y) * (1f - factor) + mappedValue * factor;
					result.SetValue(x, y, newValue);
				}
			}
			return result;
		}
	}
}
