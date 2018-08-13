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
		public Module()
		{
			
			var indexHtml = File.ReadAllText("index.html");
			Get["/"] = para => {
				return indexHtml;
			};
			Post["/photoupload"] = para =>
			{
				var file = Request.Files.FirstOrDefault();
				if(file != null)
				{
					byte[] fileContents;
					using(BinaryReader br = new BinaryReader(file.Value))
					{
						fileContents = br.ReadBytes((int)file.Value.Length);
					}
					
					string fileName = DateTime.Now.Ticks.ToString() + file.Name.Replace(' ','-');
					if(!Directory.Exists("photos"))
					{
						Directory.CreateDirectory("photos");
					}
					
					File.WriteAllBytes("photos/" + fileName, fileContents);
					RunCommand("convert", "-normalize " + "photos/" + fileName + " " + "photos/" + "n" + fileName);
					RunCommand("lp", "photos/" + "n" + fileName);
					
					return "ok";
					//return Response.AsImage("photos/" + "n" + fileName);
					
				}
				return "invalid image :[";
				return Response.AsRedirect("/");
			};
			Get["/snel"] = Get["/sneller"] = Get["/fast"] = Get["/faster"] = para => Response.AsRedirect("http://192.168.2.3/");
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
