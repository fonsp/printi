using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rasterizer;
using System.IO;
using System.Drawing;
using PoloreceiptServer.Models;
using System.Diagnostics;

namespace PoloreceiptServer
{
	public class ApiModule : NancyModule
	{
		public static bool fitToPageWidth = true;
		public static bool enablePhotoNormalization = true;

		public static int queueTimeoutInMilliseconds = 30 * 1000;

		public ApiModule() : base("/api")
		{
			Get["/"] = _ => Response.AsText("it's api time!!!!!").WithStatusCode(HttpStatusCode.OK);

			Get["/makecoffee"] = _ => Response.AsText("sorry :( 🙅☕ ").WithStatusCode(HttpStatusCode.ImATeapot);
			
			Post["/rasterizer/convert", true] = async (ctx, ct) =>
			{
				var files = Request.Files;
				if(files == null || !files.Any())
				{
					return Response.AsText("no files posted").WithStatusCode(HttpStatusCode.NoContent);
				}

				var processed = await Task.Run(() => ProcessFiles(files.Take(1)), ct);

				if(processed.Any())
				{
					return Response.FromStream(new MemoryStream(processed.First().data), "application/octet-stream").WithStatusCode(HttpStatusCode.OK);
				}
				return Response.AsText("none of the images could be processed").WithStatusCode(HttpStatusCode.UnsupportedMediaType);

			};

			Post["/submitimages/{printerName?printi}", true] = async (ctx, ct) =>
			{
				string printer = ctx.printerName;
				if(Request.Files == null || !Request.Files.Any())
				{
					return Response.AsText("no files posted").WithStatusCode(HttpStatusCode.NoContent);
				}

				if(printer == "printi")
				{
					int numPrinted = PrintFilesOnRoot(Request.Files, Request.UserHostAddress);
					if(numPrinted > 0)
					{
						return Response.AsText(numPrinted + " image printed at root").WithStatusCode(HttpStatusCode.OK);
					}
				}
				else
				{
					List<PrintQueueItem> result = await Task.Run(() => ProcessFiles(Request.Files, Request.UserHostAddress), ct);

					if(result.Any())
					{
						lock(printQueueLock)
						{
							if(!printQueues.ContainsKey(printer))
							{
								printQueues.Add(printer, new List<PrintQueueItem>());
							}
							var queue = printQueues[printer];

							queue.AddRange(result);

							if(queueUpdates.ContainsKey(printer))
							{
								queueUpdates[printer].TrySetResult(true);
							}
						}
						Console.WriteLine("{0} images submitted to the queue", result.Count());
						return Response.AsText(result.Count() + " image submitted to the queue").WithStatusCode(HttpStatusCode.OK);
					}

				}
				return Response.AsText("none of the images could be processed").WithStatusCode(HttpStatusCode.UnsupportedMediaType);
			};

			Get["/nextinqueue/{printerName?printi}", true] = async (ctx, ct) =>
			{
				string printer = ctx.printerName;
				TaskCompletionSource<bool> tcs;

				lock(printQueueLock)
				{
					if(queueUpdates.ContainsKey(printer))
					{
						queueUpdates[printer].TrySetResult(false);
						queueUpdates[printer] = new TaskCompletionSource<bool>();
					}
					else
					{
						queueUpdates.Add(printer, new TaskCompletionSource<bool>());
					}
					tcs = queueUpdates[printer];

					if(printQueues.ContainsKey(printer) && printQueues[printer].Any())
					{
						tcs.TrySetResult(true);
					}
				}

				await Task.WhenAny(tcs.Task, Task.Delay(queueTimeoutInMilliseconds));

				if(tcs.Task.IsCompleted && tcs.Task.Result)
				{
					lock(printQueueLock)
					{
						if(printQueues.ContainsKey(printer) && printQueues[printer].Any())
						{
							var queue = printQueues[printer];
							var first = queue.First();
							queue.RemoveAt(0);
							if(!queue.Any())
							{
								printQueues.Remove(printer);
							}

							return Response.FromStream(new MemoryStream(first.data), "application/octet-stream").WithStatusCode(HttpStatusCode.Found);
						}
					}
				}

				return Response.AsText("nothing found").WithStatusCode(HttpStatusCode.NotFound);
			};

		}

		public static object printQueueLock = new object();
		public static Dictionary<string, List<PrintQueueItem>> printQueues = new Dictionary<string, List<PrintQueueItem>>();
		public static Dictionary<string, TaskCompletionSource<bool>> queueUpdates = new Dictionary<string, TaskCompletionSource<bool>>();

		public static void CleanQueue()
		{
			Console.WriteLine("Cleaning queue...");
			lock(printQueueLock)
			{
				var oldKeys = printQueues.Keys.ToArray();
				foreach(var key in oldKeys)
				{
					var newQueue = from queueItem in printQueues[key] where queueItem.expirationDateTime > DateTime.Now select queueItem;
					if(newQueue.Any())
					{
						printQueues[key] = newQueue.ToList();
					}
					else
					{
						printQueues.Remove(key);
					}
				}

				oldKeys = queueUpdates.Keys.ToArray();
				foreach(var key in oldKeys)
				{
					if(!printQueues.ContainsKey(key))
					{
						queueUpdates[key].TrySetResult(false);
						queueUpdates.Remove(key);
					}
				}

			}
			Console.WriteLine("Queue cleaned succesfully.");
		}

		private static List<PrintQueueItem> ProcessFiles(IEnumerable<HttpFile> files, string userHostAddress = "")
		{
			var output = new List<PrintQueueItem>();
			foreach(var file in files)
			{
				Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + " =-= " + userHostAddress + " =-= " + file.Name);
				if(file.Value.Length > 10e6)
				{
					continue;
				}
				try
				{
					Bitmap image = Image.FromStream(file.Value) as Bitmap;

					byte[] data = new RasterPrinter().ImageToPrintCommands(image, new BurkesDitherer());
					var queueItem = new PrintQueueItem(data, DateTime.Now, DateTime.Now + new TimeSpan(30, 0, 0, 0));

					output.Add(queueItem);
				}
				catch(Exception e)
				{
					Console.WriteLine("ERROR: Failed to create image from file: {0}", e);
				}
			}
			return output;
		}

		/// <summary>
		/// Instead of rasterizing the files and adding them to the queue, the files are saved and printed using CUPS
		/// </summary>
		/// <param name="files"></param>
		/// <param name="userHostAddress"></param>
		/// <returns></returns>
		private static int PrintFilesOnRoot(IEnumerable<HttpFile> files, string userHostAddress = "")
		{
			int anythingUploaded = 0;
			foreach(var file in files)
			{
				Console.WriteLine(DateTime.Now.ToLocalTime().ToString() + " =-= " + userHostAddress + " =-= " + file.Name);
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

				File.WriteAllBytes("photos/" + fileName, fileContents);

				if(IsUnix)
				{
					RunCommand("convert", (enablePhotoNormalization ? "-normalize " : "") + ("photos/" + fileName) + " " + ("photos/" + "n" + fileName));
					RunCommand("lp", (fitToPageWidth ? "-o fit-to-page " : "") + "photos/" + "n" + fileName);
				}
				else
				{
					Console.WriteLine("beep boop printing beep 🖨 " + fileName);
				}
				anythingUploaded++;
			}
			return anythingUploaded;
		}

		private static bool IsUnix = new PlatformID[] { PlatformID.MacOSX, PlatformID.Unix }.Contains(Environment.OSVersion.Platform);


		/// <summary>
		/// Remove all characters from a file name that are not letters or numbers.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
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


		private static void RunCommand(string programName, string arguments)
		{
			if(!IsUnix)
			{
				throw new PlatformNotSupportedException("only works on UNIX");
			}
			ProcessStartInfo psi = new ProcessStartInfo(programName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = false;

			Process proc = new Process();
			proc.StartInfo = psi;

			proc.Start();
			proc.WaitForExit();
		}
	}

	public struct PrintQueueItem
	{
		public byte[] data;
		public DateTime submittedDateTime;
		public DateTime expirationDateTime;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="submittedDateTime"></param>
		/// <param name="expirationDateTime">Date after which the item will be deleted from the queue</param>
		public PrintQueueItem(byte[] data, DateTime submittedDateTime, DateTime expirationDateTime)
		{
			this.data = data;
			this.submittedDateTime = submittedDateTime;
			this.expirationDateTime = expirationDateTime;
		}
	}
}
