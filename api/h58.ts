import {
    createCanvas,
    ImageData,
    EmulatedImageData,
    ImageDataConstructor,
    loadImage,
    EmulatedCanvas2D,
    EmulatedCanvas2DContext,
} from "https://deno.land/x/canvas@v1.4.1/mod.ts"
import { BWImage } from "./BWImage.ts"

import * as _ from "./imports/lodash.ts"

export const to_png = (raster: BWImage) => {
    const rgba_data = new Uint8ClampedArray(raster.bit_data.length * 8 * 4)
    Array.from(raster.bit_data).forEach((byte, i) => {
        for (let j = 0; j < 8; j++) {
            // bytes are reversed
            const bit_index = 7 - j
            const is_black = ((byte >> bit_index) & 1) > 0

            rgba_data.set(is_black ? [0, 0, 0, 255] : [255, 255, 255, 255], i * 8 * 4 + j * 4)
        }
    })

    const canvas = createCanvas(raster.size[0], raster.size[1])
    const ctx = canvas.getContext("2d")

    const imageData: ImageData = {
        data: rgba_data,
        width: raster.size[0],
        height: raster.size[1],
    }
    ctx.putImageData(imageData, 0, 0)
    return canvas.toBuffer()
}

export const to_h58 = (raster: BWImage): Uint8Array => {
    const [width, height] = raster.size
    const dotsPerLine = width
    const bytesPerLine = Math.floor(dotsPerLine / 8)
    if (dotsPerLine != bytesPerLine * 8) {
        throw new Error("raster width should be a multiple of 8")
    }

    const num_slices = Math.ceil(height / 24)
    const output = new Uint8Array(4 + bytesPerLine * height + num_slices * (4 + 4 + 3))
    let output_offset = 0
    const add_to_output = (bytes: ArrayLike<number>) => {
        output.set(bytes, output_offset)
        output_offset += bytes.length
    }
    add_to_output([0x1b, 0x40])
    for (let y = 0; y < height; y += 24) {
        const sliceHeight = Math.min(24, height - y)

        add_to_output([0x1d, 0x76, 0x30, 0x00])
        add_to_output([bytesPerLine & 255, bytesPerLine >> 8])
        add_to_output([sliceHeight & 255, sliceHeight >> 8])

        add_to_output(raster.bit_data.subarray(y * bytesPerLine, (y + sliceHeight) * bytesPerLine))

        add_to_output([0x1b, 0x4a, 0x15])
    }
    add_to_output([0x1b, 0x40])
    return output
}
