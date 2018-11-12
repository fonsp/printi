using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using System.IO;
using System.Diagnostics;

namespace PoloreceiptServer
{
	public class Module : NancyModule
	{
		bool fitToPageWidth = true;
		bool enablePhotoNormalization = true;

		public Module()
		{
			Get["/"] = para => {
				return File.ReadAllText("index.html");
			};
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
			Get["/snel"] = Get["/sneller"] = Get["/fast"] = Get["/faster"] = para => Response.AsRedirect("http://192.168.2.42/");
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
}
