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
using PoloreceiptServer.Models;

namespace PoloreceiptServer
{
	public class Module : NancyModule
	{
		public Module()
		{
			After.AddItemToEndOfPipeline((ctx) => ctx.Response.WithHeader("Access-Control-Allow-Origin", "*").WithHeader("Access-Control-Allow-Methods", "POST,GET").WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
			Options["/"] = _ => new Response();

			Get["/{printerName?printi}"] = (ctx) =>
			{
				var model = new PrinterPageModel(ctx.printerName, ctx.printerName == "printi");
				return View["Index", model];
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

			
			Get["/snel"] = Get["/sneller"] = Get["/fast"] = Get["/faster"] = para => Response.AsRedirect("http://192.168.2.42/");

			//After.AddItemToEndOfPipeline((ctx) => ctx.Response.WithHeader("Access-Control-Allow-Origin", "*").WithHeader("Access-Control-Allow-Methods", "POST,GET").WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
		}
	}
}
