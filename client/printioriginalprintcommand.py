import StarTSPImage
from PIL import Image, ImageDraw

assert sys.argv.length == 2

image_path = sys.argv[-1]

image = Image.open(image_path)

# in the future here i will rotate the image als ie te groot is

raster = StarTSPImage.imageToRaster(image, cut=True)

with open('/dev/usb/lp0', "wb") as printer
    printer.write(raster)
