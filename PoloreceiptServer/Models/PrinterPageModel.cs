using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoloreceiptServer.Models
{
	public class PrinterPageModel
	{
		private string printerSubDir;
		public string PrinterSubDir
		{
			get
			{
				if(IsRootPrinter)
				{
					return "";
				}
				return printerSubDir ?? "/" + PrinterName;
			}
			set
			{
				printerSubDir = value;
			}
		}
		public string PrinterName;
		public bool IsRootPrinter;

		public PrinterPageModel(string printerName, bool isRootPrinter = false)
		{
			PrinterName = printerName;
			IsRootPrinter = isRootPrinter;
		}
	}
}
