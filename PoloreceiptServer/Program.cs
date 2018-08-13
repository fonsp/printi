using System;
using System.Diagnostics;
using Nancy.Hosting.Self;

namespace PoloreceiptServer
{
	public class Program
	{
		static void Main(string[] args)
		{
			int port = 80;
			var url = "http://" + "localhost" + ":" + port;
			HostConfiguration hostConfigs = new HostConfiguration();
			hostConfigs.UrlReservations.CreateAutomatically = true;
			using(var nancyHost = new NancyHost(hostConfigs, new Uri(url)))
			{
				nancyHost.Start();

				Console.WriteLine("Nancy now listening - navigating to {0}. Press enter to stop", url);
				try
				{
					//Process.Start("http://localhost:3579/");
				}
				catch(Exception)
				{
				}
				Console.ReadKey();
			}

			Console.WriteLine("Stopped. Good bye!");
		}
	}
}
