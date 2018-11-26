using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rasterizer;
using System.IO;
using System.Drawing;

namespace PoloreceiptServer
{
	public class ApiModule : NancyModule
	{

		public ApiModule() : base("/api")
		{
			Get["/"] = _ => "it's api time!!!!!";

			Get["/rasterizer/convert", true] = async (ctx, ct) =>
			{
				var printer = new RasterPrinter();
				byte[] commands = await Task.Run(() => printer.ImageToPrintCommands(null, new BurkesDitherer()));
				return Response.FromStream(new MemoryStream(commands), "application/octet-stream");
			};
			
		}

		private static bool ProcessFiles(IEnumerable<HttpFile> files, string userHostAddress = "")
		{
			bool anythingUploaded = false;
			foreach(var file in files)
			{
				anythingUploaded = true;
				Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + " =-= " + userHostAddress + " =-= " + file.Name);
				if(file.Value.Length > 10e6)
				{
					continue;
				}
				try
				{
					Bitmap image = Image.FromStream(file.Value) as Bitmap;
					
				}
				catch(Exception e)
				{
					Console.WriteLine("ERROR: Failed to create image from file: {0}", e);
				}
			}
			return anythingUploaded;

		}
	}
}
