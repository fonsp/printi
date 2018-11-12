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

				Console.WriteLine("Server running on {0}", url);
				string input = "";
				while(input != "exit")
				{
					System.Threading.Thread.Sleep(5000);
					Console.WriteLine("Server running...");
					//input = Console.ReadLine();
				}
			}

			Console.WriteLine("Stopped. Good bye!");
		}
	}
}
