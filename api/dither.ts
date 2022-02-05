import canvasdither from "https://esm.sh/canvas-dither@1.0.1"
import { createCanvas, ImageData, loadImage, EmulatedCanvas2D, EmulatedCanvas2DContext } from "https://deno.land/x/canvas@v1.4.1/mod.ts"

export const dither_bytes_to_imagedata = async (img_contents: Uint8Array) => {
    const img = await loadImage(img_contents)
    const w = img.width(),
        h = img.height()

    const canvas = createCanvas(w, h)
    const ctx = canvas.getContext("2d")

    ctx.drawImage(img, 0, 0, w, h)

    const input_img_data = ctx.getImageData(0, 0, w, h)
    const output_img_data: ImageData = canvasdither.floydsteinberg(input_img_data)
    return {
        imagedata: output_img_data,
        canvas,
        ctx,
    }
}

export const dither_url_to_png_data = async (url: string | Request | URL) => {
    const dino_data = await (await fetch(url)).arrayBuffer()
    return await dither_bytes_to_png_data(new Uint8Array(dino_data))
}

export const dither_bytes_to_png_data = async (bytes: Uint8Array) => {
    const { imagedata, canvas, ctx } = await dither_bytes_to_imagedata(bytes)

    ctx.putImageData(imagedata, 0, 0)
    return canvas.toBuffer()
}
