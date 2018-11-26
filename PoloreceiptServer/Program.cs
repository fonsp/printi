using System;
using System.Diagnostics;
using Nancy.Hosting.Self;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace PoloreceiptServer
{

	public class CustomBootstrapper : DefaultNancyBootstrapper
	{
		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			///// whatever /////
			pipelines.AfterRequest += (ctx) => {
				ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
							.WithHeader("Access-Control-Allow-Methods", "POST, GET, DELETE, PUT, OPTIONS, PATCH")
							.WithHeader("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization")
							.WithHeader("Access-Control-Max-Age", "3600");
				
			};
		}
	}

	public class Program
	{
		static void Main(string[] args)
		{
			int port = 3579;
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
