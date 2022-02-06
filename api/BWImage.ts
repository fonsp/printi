import { createCanvas, ImageData, loadImage, EmulatedCanvas2D, EmulatedCanvas2DContext } from "https://deno.land/x/canvas@v1.4.1/mod.ts"

export type BWImage = {
    size: [number, number]
    bit_data: Uint8Array
}

export function imagedata_to_bwimage(imagedata: ImageData): BWImage {
    const input_width = imagedata.width
    const input_height = imagedata.height
    const width = Math.ceil(input_width / 8) * 8
    const height = input_height

    const bit_data = new Uint8Array((width * height) / 8)
    const input_data = imagedata.data
    for (let y = 0; y < height; y++) {
        const offset = y * width

        for (let x = 0; x < input_width; x += 8) {
            let result = 0
            const num_bits = Math.min(8, input_width - x)
            for (let bit_index = 0; bit_index < num_bits; bit_index++) {
                result |= (input_data[(offset + x + bit_index) * 4] > 0 ? 0 : 1) << bit_index
            }
            bit_data[(offset + x) >> 3] = result
        }
    }

    return {
        bit_data,
        size: [width, height],
    }
}
