from PIL import Image
import hitherdither
import numpy as np
import os, sys


def printRaster(raster, device = "output.txt"):
    height, width = raster.shape
    if width < 384:
        print("raster will be cropped")
    if width < 384:
        print("raster will be clipped")

    printDevice = os.open(device, os.O_WRONLY)
    put = lambda data: os.write(printDevice, data)
    putb = lambda data: os.write(printDevice, bytearray(data))

    # initialize the printer in graphics mode
    putb([0x1b,0x40])
    dotsPerLine = min(width, 384)
    bytesPerLine = dotsPerLine // 8
    dotsPerLine = bytesPerLine * 8

    croppedData = raster[:,:dotsPerLine]

    for y in range(0,height,24):
        sliceHeight = min(24, height - y)
        putb([0x1d, 0x76, 0x30, 0x00])
        putb([bytesPerLine % 256, bytesPerLine // 256])
        putb([sliceHeight % 256, sliceHeight // 256])

        slicedData = croppedData[y:y+sliceHeight,:]
        toSend = np.packbits(slicedData).flatten().tolist()
        putb(toSend[0])

        putb([0x1b,0x4a,0x15])

    os.fsync(printDevice)

    # shut down device
    putb([0x1b,0x40])
    os.close(printDevice)


def imageToRaster(img, order):
    img = img.resize((384, int(384 * img.height // img.width )) )
    img = img.convert(mode="L")
    imgArray = np.asarray(img)
    img.close()

    #mult = np.array([[[0.2126, 0.7152, 0.0722]]])
    #bw = np.dot(imgArray,mult[0,0,:]) / 256.0
    bw = imgArray

    bwSquared = np.multiply(bw, bw)
    bayerMatrix = hitherdither.ordered.bayer.B(order)
    xx, yy = np.meshgrid(range(bwSquared.shape[1]), range(bwSquared.shape[0]))
    xx %= order
    yy %= order

    dithered = bwSquared < bayerMatrix[xx,yy]
    return dithered


def showRaster(raster):
    bwPalette = hitherdither.palette.Palette(
        [0x000000, 0xffffff]
    )
    derp = np.tile(np.expand_dims(np.invert(raster)*255.0,axis=2),(1,1,3))
    img_dithered = bwPalette.create_PIL_png_from_rgb_array(derp)
    img_dithered.show()
    img_dithered.close()

img = Image.open("logoBW.png")
rastertje = imageToRaster(img, 16)
img.close()
printRaster(rastertje)
showRaster(rastertje)
