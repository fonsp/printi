import requests as r
import os.path
from io import BytesIO

def setup():
    ## Printer
    printerPath = "/dev/usb/lp0"
    fh = os.open(printerPath, 'w')
    fh.write(bytearray([ord(x) for x in "Hello!"]))

    logoRequest = r.get("https://printi.me/Content/logo.h58")
    fh.write(BytesIO(logoRequest.content))

    fh.close()



setup()
