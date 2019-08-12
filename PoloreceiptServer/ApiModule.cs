using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rasterizer;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
// using UrlToImage;

namespace PoloreceiptServer
{
	public class ApiModule : NancyModule
	{
		public static bool fitToPageWidth = true;
		public static bool enablePhotoNormalization = true;

		public static int queueTimeoutInMilliseconds = 30 * 1000;

		/// <summary>
		/// Collection of printer names that are _not_ a printi mini. These printers will use raw bitmaps, instead of rasterising on the server.
		/// </summary>
		public static HashSet<string> printiOriginalPrinterNames = new HashSet<string>(new[] { "", "printi" });

		public ApiModule() : base("/api")
		{
			Get["/"] = _ => Response.AsText("it's api time!!!!!").WithStatusCode(HttpStatusCode.OK);

			Get["/makecoffee"] = _ => Response.AsText("sorry :( 🙅☕ ").WithStatusCode(HttpStatusCode.ImATeapot);

			// Will simply convert the first attached file to h58 print commands
			Post["/rasterizer/convert", true] = async (ctx, ct) =>
			{
				var files = Request.Files;
				if(files == null || !files.Any())
				{
					return Response.AsText("no files posted").WithStatusCode(HttpStatusCode.NoContent);
				}

				var processed = await Task.Run(() => ProcessFiles(Request.Files, "rasterizertester"), ct);

				if(processed.Any())
				{
					return Response.FromStream(new MemoryStream(processed.First().data), "application/octet-stream").WithStatusCode(HttpStatusCode.OK).WithHeader("Content-Disposition", "attachment; filename=\"image.bin\"");
				}
				return Response.AsText("none of the images could be processed").WithStatusCode(HttpStatusCode.BadRequest);

			};

			/*
			 * Submit images to the printi queue. After submitting, a new thread will start to
			 * process the image (rotate, scale, gamma correction, dither). After processing, it
			 * will be added to the choses printer's queue. The printer can request images from
			 * its queue with the /nextinqueue method.
			 */
			Post["/submitimages/{printerName?printi}", true] = async (ctx, ct) =>
			{
				string printer = ctx.printerName;

				List<PrintQueueItem> result;

				if(Request.Headers.ContentType.Contains("json"))
				{
					List<Bitmap> bitmaps = new List<Bitmap>();

					Base64ImagesRequest base64ImagesRequest;
					try
					{
						base64ImagesRequest = this.Bind<Base64ImagesRequest>();
						if(!base64ImagesRequest.images.Any())
						{
							Console.WriteLine("Empty json request.");
							return Response.AsText("Empty json request.").WithStatusCode(HttpStatusCode.BadRequest);
						}
					}
					catch(Exception e)
					{
						Console.WriteLine("Failed to interpret json request: " + e);
						return Response.AsText("Failed to interpret json request: " + e).WithStatusCode(HttpStatusCode.BadRequest);
					}

					try
					{
						bitmaps = await Task.Run(() => ConvertBase64ToBitmaps(base64ImagesRequest.images), ct);
					}
					catch(Exception e)
					{
						Console.WriteLine("Failed to convert base64 strings to bitmaps: " + e);
						return Response.AsText("Failed to convert base64 strings to bitmaps: " + e).WithStatusCode(HttpStatusCode.UnsupportedMediaType);
					}
					/*
					if(printer == "printi")
					{
						try
						{
							int numPrinted = PrintBitmapsOnRoot(bitmaps, Request.UserHostAddress);
							return Response.AsText(numPrinted + " base64 images printed at root").WithStatusCode(HttpStatusCode.OK);
						}
						catch(Exception e)
						{
							Console.WriteLine("Failed to print base64 bitmaps on root: " + e);
							return Response.AsText("Failed to print base64 bitmaps on root: " + e).WithStatusCode(HttpStatusCode.UnsupportedMediaType);
						}
					}
					*/
					result = await Task.Run(() => ProcessBitmaps(bitmaps, printer, Request.UserHostAddress), ct);
				}
				else /* if(true || Request.Headers.ContentType.Contains("form"))*/
				{
					if(Request.Files == null || !Request.Files.Any())
					{
						Console.WriteLine("No files in request");
						return Response.AsText("no files posted").WithStatusCode(HttpStatusCode.NoContent);
					}
					/*
					if(printer == "printi")
					{
						int numPrinted = PrintFilesOnRoot(Request.Files, Request.UserHostAddress);
						return Response.AsText(numPrinted + " files printed at root").WithStatusCode(HttpStatusCode.OK);
					}
					*/
					result = await Task.Run(() => ProcessFiles(Request.Files, printer, Request.UserHostAddress), ct);
				}

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
					Console.WriteLine("{0} image(s) submitted to the queue", result.Count());
					return Response.AsText(result.Count() + " image(s) submitted to the queue").WithStatusCode(HttpStatusCode.OK);
				}

				return Response.AsText("none of the images could be processed").WithStatusCode(HttpStatusCode.UnsupportedMediaType);
			};

			/*
			 * Returns the next processed image (h58 commands) in the chosen printer's queue.
			 * If no image is found, the server will (asynchronously) wait before responding with
			 * NotFound. If, while waiting, an image is submitted to this printer's queue (by
			 * someone uploading an image from the website), the wait will be aborted, and the
			 * processed image is returned.
			 * Using this method, we can maintain an open TCP connection with the client (i.e.
			 * printer), using regular HTTP communication, while minimizing the number of
			 * requests that the server has to handle every second.
			 * To reduce the number of concurrent Tasks (threads), we only have 1 Task per
			 * printer name, and the previously running Task is aborted when a new GET request
			 * is sent to the server.
			 */
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

							string filename = first.fileNamePreference ?? (first.isRasterized ? "image.bin" : "image.jpg");
							return Response.FromStream(new MemoryStream(first.data), "application/octet-stream").WithStatusCode(HttpStatusCode.OK).WithHeader("Content-Disposition", "attachment; filename=\"" + filename + "\"");
						}
					}
				}

				return Response.AsText("nothing found").WithStatusCode(HttpStatusCode.NotFound);
			};

			Post["/url/{printerName?printi}"] = ctx =>
				{
					return Response.AsText("URL support has been suspended 😢")
						.WithStatusCode(HttpStatusCode.MethodNotAllowed);
				};

			/*
			Post["/url/{printerName?printi}", true] = async (ctx, ct) =>
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
							System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
							HttpResponseMessage response = await new HttpClient().PostAsync("https://printi.me/api/submitimages/" + ctx.printerName, formContent);
							if(!response.IsSuccessStatusCode)
							{
								Console.WriteLine("Failed to send page to printi.me: ");
								Console.WriteLine(response.ReasonPhrase);
								return Response.AsText("connection error: " + response.ReasonPhrase).WithStatusCode(HttpStatusCode.ServiceUnavailable);
							}
						}
						return Response.AsText("page sent to printer").WithStatusCode(HttpStatusCode.OK);
					}
					else
					{
						Console.WriteLine("bad URL");
						return Response.AsText("invalid URL provided").WithStatusCode(HttpStatusCode.BadRequest);
					}

				}
				catch(Exception e)
				{
					Console.WriteLine("URL could not be processed: " + e.Message);
					return Response.AsText("URL could not be processed: " + e.Message).WithStatusCode(HttpStatusCode.BadRequest);
				}
				return Response.AsText("something went wrong").WithStatusCode(HttpStatusCode.ImATeapot);
			};
			*/

			Post["/clean"] = ctx =>
			{
				int oldCount;
				lock(printQueueLock)
				{
					oldCount = printQueues.Count;
				}
				int oldSize = totalPrintQueueItems;

				CleanQueue();

				int newCount;
				lock(printQueueLock)
				{
					newCount = printQueues.Count;
				}
				int newSize = totalPrintQueueItems;
				return Response.AsText("Removed " + (oldCount - newCount) + " queues and " + (oldSize - newSize) + " items.").WithStatusCode(HttpStatusCode.OK);
			};

			Get["/queuesize"] = ctx =>
			{
				int count;
				lock(printQueueLock)
				{
					count = printQueues.Count;
				}
				int size = totalPrintQueueItems;
				return Response.AsText(count + " queues and " + size + " items.").WithStatusCode(HttpStatusCode.OK);
			};
		}

		public static object printQueueLock = new object();
		public static Dictionary<string, List<PrintQueueItem>> printQueues = new Dictionary<string, List<PrintQueueItem>>();
		public static Dictionary<string, TaskCompletionSource<bool>> queueUpdates = new Dictionary<string, TaskCompletionSource<bool>>();

		public static int totalPrintQueueItems
		{
			get
			{
				return printQueues.Select(pair => pair.Value.Count()).Sum();
			}
		}

		/// <summary>
		/// Will delete any expired images, and remove queues that have nothing in them. This will
		/// also stop all running Tasks (if there are no queued images for that printer).
		/// </summary>
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
						// TODO: smart?
						queueUpdates[key].TrySetResult(false);
						queueUpdates.Remove(key);
					}
				}

			}
			Console.WriteLine("Queue cleaned succesfully.");
		}

		private static List<PrintQueueItem> ProcessBitmaps(IEnumerable<Bitmap> bitmaps, string printer, string userHostAddress = "")
		{
			var output = new List<PrintQueueItem>();

			bool isPrintiOriginal = printiOriginalPrinterNames.Contains(printer);

			foreach(Bitmap bitmap in bitmaps)
			{
				Console.WriteLine("BASE64: " + DateTime.Now.ToLocalTime().ToString() + " =-= " + userHostAddress);

				byte[] data;
				if(isPrintiOriginal)
				{
					MemoryStream ms = new MemoryStream();
					bitmap.Save(ms, ImageFormat.Jpeg);
					data = ms.ToArray();
				}
				else
				{
					data = new RasterPrinter().ImageToPrintCommands(bitmap, new BurkesDitherer());
				}

				var queueItem = new PrintQueueItem(data, !isPrintiOriginal, DateTime.Now, DateTime.Now + new TimeSpan(30, 0, 0, 0));

				output.Add(queueItem);
			}
			return output;
		}

		private static List<PrintQueueItem> ProcessFiles(IEnumerable<HttpFile> files, string printer, string userHostAddress = "")
		{
			var output = new List<PrintQueueItem>();

			bool isPrintiOriginal = printiOriginalPrinterNames.Contains(printer);

			foreach(var file in files)
			{
				Console.WriteLine("FILE: " + DateTime.Now.ToLocalTime().ToString() + " =-= " + userHostAddress + " =-= " + file.Name);

				if(file.Value.Length > 10e7)
				{
					Console.WriteLine("File too big: {0}", file.Value.Length);
					continue;
				}

				byte[] data = null;
				string fileNamePreference = LegalizeFilename(file.Name);

				if(isPrintiOriginal)
				{
					using(BinaryReader br = new BinaryReader(file.Value))
					{
						data = br.ReadBytes((int)file.Value.Length);
					}
				}
				else
				{
					Bitmap bitmap = ConvertFileToBitmap(file);
					if(bitmap != null)
					{
						data = new RasterPrinter().ImageToPrintCommands(bitmap, new BurkesDitherer());
					}

					fileNamePreference = fileNamePreference + ".bin";
				}

				if(data != null)
				{
					var queueItem = new PrintQueueItem(data, !isPrintiOriginal, DateTime.Now, DateTime.Now + new TimeSpan(30, 0, 0, 0), fileNamePreference);
					output.Add(queueItem);
				}
			}
			return output;
		}

		private static List<Bitmap> ConvertBase64ToBitmaps(IEnumerable<string> imageStrings)
		{
			var output = new List<Bitmap>();
			foreach(var imageString in imageStrings)
			{
				try
				{
					byte[] imb = Convert.FromBase64String(imageString);
					Bitmap bitmap = new ImageConverter().ConvertFrom(imb) as Bitmap;
					output.Add(bitmap);
				}
				catch(Exception e)
				{
					Console.WriteLine("ERROR: Failed to create image from base64 string: {0}", e);
				}
			}
			return output;
		}

		private static Bitmap ConvertFileToBitmap(HttpFile file)
		{
			try
			{
				Bitmap bitmap;
				// Mono is having trouble creating an Image directly from the file.Value stream.
				using(var ms = new MemoryStream())
				{
					file.Value.CopyTo(ms);
					bitmap = new ImageConverter().ConvertFrom(ms.ToArray()) as Bitmap;
				}

				return bitmap;
			}
			catch(Exception e)
			{
				Console.WriteLine("ERROR: Failed to create image from file: {0}", e);
			}

			return null;
		}

		/*private static int PrintBitmapsOnRoot(IEnumerable<Bitmap> bitmaps, string userHostAddress = "")
		{
			int anythingUploaded = 0;
			foreach(Bitmap bitmap in bitmaps)
			{
				string fileName = DateTime.Now.Ticks.ToString() + ".jpg";
				if(!Directory.Exists("photos"))
				{
					Directory.CreateDirectory("photos");
				}
				bitmap.Save("photos/" + fileName, ImageFormat.Jpeg);

				ConvertAndPrintLocalFile(fileName);

				anythingUploaded++;
			}

			return anythingUploaded;
		}*/

		/*
		/// <summary>
		/// Instead of rasterizing the files and adding them to the queue, the files are saved and
		/// printed using CUPS on the machine running the server.
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

				ConvertAndPrintLocalFile(fileName);

				anythingUploaded++;
			}
			return anythingUploaded;
		}
		*/

		/*
		private static void ConvertAndPrintLocalFile(string fileName)
		{
			if(IsUnix)
			{
				try
				{
					RunCommand("convert", (enablePhotoNormalization ? "-normalize " : "") + ("photos/" + fileName) + " " + ("photos/" + "n" + fileName));
					RunCommand("lp", (fitToPageWidth ? "-o fit-to-page " : "") + "photos/" + "n" + fileName);
				}
				catch (Exception e)
				{
					Console.WriteLine("ERROR: Failed to run print commands at root: {0}", e);
				}
			}
			else
			{
				Console.WriteLine("beep boop printing beep 🖨 " + fileName);
			}
		}
		*/

		// private static bool IsUnix = new PlatformID[] { PlatformID.MacOSX, PlatformID.Unix }.Contains(Environment.OSVersion.Platform);


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

		/*private static void RunCommand(string programName, string arguments)
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
		}*/
	}

	public struct PrintQueueItem
	{
		public byte[] data;
		public DateTime submittedDateTime;
		public DateTime expirationDateTime;

		public bool isRasterized;
		public string fileNamePreference;

		/// <summary>
		///
		/// </summary>
		/// <param name="data"></param>
		/// <param name="isRasterized"></param>
		/// <param name="submittedDateTime"></param>
		/// <param name="expirationDateTime">Date after which the item will be deleted from the queue</param>
		/// <param name="fileNamePreference">If specified, this filename will be used for non-rasterized images. Important for preserving file extension.</param>
		public PrintQueueItem(byte[] data, bool isRasterized, DateTime submittedDateTime, DateTime expirationDateTime, string fileNamePreference=null)
		{
			this.data = data;
			this.isRasterized = isRasterized;
			this.submittedDateTime = submittedDateTime;
			this.expirationDateTime = expirationDateTime;
			this.fileNamePreference = fileNamePreference;
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

	public class Base64ImagesRequest
	{
		public IEnumerable<string> images;

		public override string ToString()
		{
			return "base64 images request containing " + images.Count() + " images.";
		}
	}
}
