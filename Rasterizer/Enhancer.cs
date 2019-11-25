using System;
using System.Drawing;


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
}
