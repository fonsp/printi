import requests as r
import os.path
from io import BytesIO

def setup():
    ## Printer
    printerPath = "/dev/usb/lp0"
    fh = os.open(printerPath, os.O_WRONLY)
    #os.write(fh,bytearray([ord(x) for x in "Hello!"]))

    logoRequest = r.get("https://printi.me/Content/logo.h58")
    os.write(fh,bytearray(logoRequest.content))

    os.close(fh)



setup()
