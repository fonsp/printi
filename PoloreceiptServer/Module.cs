using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using System.IO;
using System.Diagnostics;
using UrlToImage;
using System.Net.Http;
using System.Net;
using System.Security.Authentication;



namespace PoloreceiptServer
{
	public class Module : NancyModule
	{
		static bool fitToPageWidth = true;
		static bool enablePhotoNormalization = true;

		public Module()
		{
			After.AddItemToEndOfPipeline((ctx) => ctx.Response.WithHeader("Access-Control-Allow-Origin", "*").WithHeader("Access-Control-Allow-Methods", "POST,GET").WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
			Options["/"] = route => new Response();
			Get["/"] = para =>
			{
				Console.WriteLine(Request.Url);
				return File.ReadAllText("index.html");
			};
			Get["/ping"] = para => "OK";
			/*
			Get["/normalize/{yn:bool}"] = para =>
			{
				enablePhotoNormalization = para.yn;
				return "enable photo normalization: " + enablePhotoNormalization;
			};
			Get["/normalize/{yn?}"] = para =>
			{
				return "Usage: visit thiswebsite.com/normalize/true or thiswebsite.com/normalize/false";
			};
			*/
			Post["/photoupload"] = para =>
			{
				bool anythingUploaded = false;
				foreach(var file in Request.Files)
				{
					anythingUploaded = true;
					Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + " =-= " + Request.UserHostAddress + " =-= " + file.Name);
					byte[] fileContents;
					using(BinaryReader br = new BinaryReader(file.Value))
					{
						fileContents = br.ReadBytes((int)file.Value.Length);
					}

					string fileName = DateTime.Now.Ticks.ToString() + LegalizeFilename(file.Name);
					if(!Directory.Exists("photos"))
					{
						Directory.CreateDirectory("photos");
					}

					// Only works on UNIX

					File.WriteAllBytes("photos/" + fileName, fileContents);
					RunCommand("convert", (enablePhotoNormalization ? "-normalize " : "") + ("photos/" + fileName) + " " + ("photos/" + "n" + fileName));
					RunCommand("lp", (fitToPageWidth ? "-o fit-to-page " : "") + "photos/" + "n" + fileName);

					//return Response.AsImage("photos/" + "n" + fileName);

				}
				if(anythingUploaded)
				{
					return "ok";
				}
				return "invalid image :[";
				//return Response.AsRedirect("/");
			};
			Post["/url", true] = async (x, ct) =>
			{
				try
				{
					var urlRequest = this.Bind<UrlRequest>();
					var urlQuery = urlRequest.url;
					Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + " =-= " + Request.UserHostAddress + " =-= " + urlQuery);

					Uri url = new UriBuilder(urlQuery).Uri;
					string urlString = url.ToString();
					if(url.IsAbsoluteUri && !url.IsFile)
					{
						for(int i = 0; i < 4; i++)
						{
							if(urlString[i] != "http"[i])
							{
								throw new Exception("not http");
							}
						}
						if(urlString[6] != '/')
						{
							throw new Exception("not http");
						}

						Console.WriteLine("resolved url: " + urlString);

						var image = PageExporter.GetBitmap(url);

						using(var memoryStream = new MemoryStream())
						{
							image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
							var formContent = new MultipartFormDataContent();
							formContent.Add(new ByteArrayContent(memoryStream.ToArray()), "page-print", "page-print-" + Guid.NewGuid() + ".png");
							System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
							var response = await new HttpClient().PostAsync("http://printi.me/photoupload", formContent);
						}
					}
					else
					{
						Console.WriteLine("bad URL");
					}

				} catch (Exception e)
				{
					Console.WriteLine("URL could not be processed: " + e.Message);
				}
				return Response.AsRedirect("/");
			};
			Get["/snel"] = Get["/sneller"] = Get["/fast"] = Get["/faster"] = para => Response.AsRedirect("http://192.168.2.42/");

			//After.AddItemToEndOfPipeline((ctx) => ctx.Response.WithHeader("Access-Control-Allow-Origin", "*").WithHeader("Access-Control-Allow-Methods", "POST,GET").WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
		}

		// To protect against injection attacks
		private static string LegalizeFilename(string fileName)
		{
			if(string.IsNullOrEmpty(fileName))
			{
				return "noname";
			}
			var legal = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_0123456789().".ToList();
			fileName = fileName.Replace(' ', '-');
			var buffer = from c in fileName where legal.Contains(c) select c;
			return new string(buffer.ToArray());
		}

		private void RunCommand(string a, string b)
		{
			ProcessStartInfo psi = new ProcessStartInfo(a, b);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = false;

			Process proc = new Process();
			proc.StartInfo = psi;

			proc.Start();
			proc.WaitForExit();
		}
	}

	public class UrlRequest
	{
		public string url;

		public override string ToString()
		{
			return "URL Requst for " + url;
		}
	}
}
