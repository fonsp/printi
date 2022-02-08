import canvasdither from "https://esm.sh/canvas-dither@1.0.1"
import { createCanvas, ImageData, loadImage } from "https://deno.land/x/canvas@v1.4.1/mod.ts"

import { fitting_size } from "./resize.ts"
import { imagedata_to_bwimage } from "./BWImage.ts"

export const dither_bytes_to_imagedata = async (img_contents: Uint8Array, max_width: number) => {
    const img = await loadImage(img_contents)
    const input_w = img.width(),
        input_h = img.height()

    const { size, rotated } = fitting_size([input_w, input_h], max_width)

    const canvas = createCanvas(...size)
    const ctx = canvas.getContext("2d")

    if (rotated) {
        ctx.rotate(Math.PI / 2)
        ctx.drawImage(img, 0, -size[0], size[1], size[0])
        ctx.rotate(-Math.PI / 2)
    } else {
        ctx.drawImage(img, 0, 0, ...size)
    }

    const input_img_data = ctx.getImageData(0, 0, ...size)
    const output_img_data: ImageData = canvasdither.floydsteinberg(input_img_data)
    return {
        imagedata: output_img_data,
        canvas,
        ctx,
    }
}

export const dither_bytes_to_bwimage = async (img_contents: Uint8Array, max_width: number) =>
    imagedata_to_bwimage((await dither_bytes_to_imagedata(img_contents, max_width)).imagedata)

// export const dither_url_to_png_data = async (url: string | Request | URL, max_width: number) => {
//     const dino_data = await (await fetch(url)).arrayBuffer()
//     return await dither_bytes_to_png_data(new Uint8Array(dino_data), max_width)
// }

// export const dither_bytes_to_png_data = async (bytes: Uint8Array, max_width: number) => {
//     const { imagedata, canvas, ctx } = await dither_bytes_to_imagedata(bytes, max_width)

//     // reuse the canvas to draw the dithered image
//     ctx.putImageData(imagedata, 0, 0)
//     return canvas.toBuffer()
// }
