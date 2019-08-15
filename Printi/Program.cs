using System;
using System.Diagnostics;
using Nancy.Hosting.Self;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Printi
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            //TODO: Nancy.Json.JsonSettings.MaxJsonLength = int.MaxValue;

            ///// whatever /////
            pipelines.AfterRequest += (ctx) => {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                            .WithHeader("Access-Control-Allow-Methods", "POST, GET, DELETE, PUT, OPTIONS, PATCH")
                            .WithHeader("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization")
                            .WithHeader("Access-Control-Max-Age", "3600");

            };

            pipelines.BeforeRequest += (ctx) =>
            {
                if (/*ctx.Request.Url.HostName == "api.printi.me" || */ctx.Request.Url.HostName.Contains("api."))
                {
                    Console.WriteLine("changed path from api.printi.me to printi.me/api");
                    ctx.Request.Url.HostName = ctx.Request.Url.HostName.Replace("api.", "");
                    ctx.Request.Url.Path = "/api" + ctx.Request.Url.Path;
                }
                return null;
            };
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var portEnv = Environment.GetEnvironmentVariable("PORT");
            var port = portEnv ?? "3579";
            var url = "http://" + "localhost" + ":" + port;
            HostConfiguration hostConfigs = new HostConfiguration();
            hostConfigs.UrlReservations.CreateAutomatically = true;

            using (var nancyHost = new NancyHost(hostConfigs, new Uri(url)))
            {
                nancyHost.Start();

                Console.WriteLine("Server running on {0}", url);
                string input = "";
                while (input != "exit")
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
