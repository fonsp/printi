Installing the printi client on the Onion Omega 2+
================

To run the printi client, we will need:
- Python3
- CUPS
- CUPS-filters?
- specifics for our printer (HOIN HOP-H58, which uses the same firmware as the )

## Installing Python3
On your Omega, run
```
opkg install python3-light
```
(`python3-base` might be enough, not yet tested.)

## Notes on printing
Before installing your printer on the Omega, make sure you can install it to a regular Linux machine first. In my case, after running the linux driver script provided by the manufacturer, I had to manually add the printer to CUPS, making sure to use the `usb://...` address instead of `192.168.1.100` (my printer wasn't even connected to the LAN), and uploading the .PPD file. You can then print files using the `lp filename.jpg` command.
Then connect your pinter to the Omega, and make sure that you can find the device in the `dev/` folder. 
In my case, the printer was available at `/dev/usb/lp0` (only when plugged in and powered). Try printing ASCII text: (you might need to change `lp0`'s permissions first)
```
chmod a+xrw /dev/usb/lp0
echo "hey fonsi" > /dev/usb/lp0
```
In my case, this worked without installing anything.
As we can see, `lp0` is a 'file', and any bytes written to it get sent to the printer. In my case, the printer will treat ASCII characters as expected, and print them.

## Notes on installing your printer driver
A CUPS printer driver consists of (I think):
- a .ppd file, describing printer characteristics (supported color modes, page sizes, etc.). When a printer is installed, this `.ppd` file will be added to the `/etc/cups/ppd/` folder.
- possible CUPS filters. 

A filter is a binary program (stored in `/usr/lib/cups/filter/`) that converts between file formats. These formats include:
- common formats such as png, jpg and pdf
- a 'universal' CUPS raster format (a kind of uncompressed bitmap, with (usually) the same width as the number of dots that your raster printer can output). Thermal printers are raster printers.
- printer specific formats. This 'file' contains the stream of bytes that will be sent to the printer to produce your image.

When a file is printed using CUPS (with the `lp` command), CUPS will determine the chain of filters required to get to the desired output format.
With CUPS and your printer installed, you can try this out with the `cupsfilter` command:
```
cupsfilter -m "application/vnd.cups-raster" -p /etc/cups/ppd/pos58.ppd --list-filters cats.jpg
```
We can run these filters and save the output:

```
cupsfilter -m "application/vnd.cups-raster" -p /etc/cups/ppd/pos58.ppd cats.jpg > outputfile
```

Unfortunately, I am not able to run the final filter that converts the raster file to printer commands (`rastertopos58`) using `cupsfilter`. We can run it manually though: (running `rastertopos58` gives us the required arguments)

```
cat outputfile | /usr/lib/cups/filter/rastertopos58 999 printi coolimage 1 "" > printercommands
```
and we can send these commands to the printer (after moving the `printercommands` file to your Omega):
```
cat printercommands > /dev/usb/lp0
```
(Of course, you can skip saving the raster and command files by piping these commands together.)


## Installing CUPS (and CUPS-filters)
