using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Drawing;
using System.IO;
using OpenQA.Selenium.Remote;

namespace UrlToImage
{
	public static class PageExporter 
	{
		private static ChromeDriver driver;
		private static ChromeMobileEmulationDeviceSettings deviceOptions;

		public static void Initialize(int deviceWidth = 400, int maxPageHeight = 4000, string userAgent = "Mozilla / 5.0(Linux; Android 5.0; SM - G900P Build / LRX21T) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 72.0.3604.0 Mobile Safari/ 537.36")
		{
			var options = new ChromeOptions();
			options.AddArgument("headless");
			//options.AddArgument("log-level=3");
			//options.AddArgument("silent");
			deviceOptions = new ChromeMobileEmulationDeviceSettings();
			deviceOptions.PixelRatio = 3;
			deviceOptions.Width = deviceWidth;
			deviceOptions.Height = maxPageHeight;
			deviceOptions.UserAgent = userAgent;
			options.EnableMobileEmulation(deviceOptions);
			options.SetLoggingPreference(LogType.Browser, LogLevel.Severe);

			var chromeDriverService = ChromeDriverService.CreateDefaultService();
			chromeDriverService.HideCommandPromptWindow = true;
			chromeDriverService.SuppressInitialDiagnosticInformation = true;

			driver = new ChromeDriver(options);
		}

		public static void Dispose()
		{
			driver.Dispose();
		}

		public static Bitmap GetBitmap(Uri pageUrl)
		{
			if(driver == null)
			{
				Initialize();
			}

			driver.Navigate().GoToUrl(pageUrl);

			var screenshot = driver.GetScreenshot();
			var pageWidth = (long)driver.ExecuteScript("return document.body.offsetWidth");
			var pageHeight = (long)driver.ExecuteScript("return document.body.offsetHeight");

			//Console.WriteLine("Page size: {0} x {1}", pageWidth, pageHeight);
			Bitmap screenshotBitmap = (Bitmap)Image.FromStream(new MemoryStream(screenshot.AsByteArray));
			var gu = GraphicsUnit.Pixel;
			var ssBounds = screenshotBitmap.GetBounds(ref gu);

			var zoomLevel = (double)pageWidth / deviceOptions.Width;

			ssBounds.Height = Math.Min(ssBounds.Height, (float)Math.Floor(deviceOptions.PixelRatio * pageHeight / zoomLevel));

			var croppedScreenshot = screenshotBitmap.Clone(ssBounds, screenshotBitmap.PixelFormat);
			screenshotBitmap.Dispose();
			//croppedScreenshot.Save("C:/Users/fonsv/Desktop/hothothotcrop.png");
			//Console.WriteLine(driver.PageSource);

			return croppedScreenshot;
		}
	}
}
