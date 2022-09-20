import { FormDataReader, Router } from "./imports/oak.ts"
import { BWImage } from "./BWImage.ts"

import { dither_bytes_to_bwimage } from "./dither.ts"
const dino_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/Xixia_Dinosaur_Park_12.jpg/160px-Xixia_Dinosaur_Park_12.png"

import { Queue } from "./queue.ts"
import { to_png, to_h58 } from "./h58.ts"

const printer_size = (name: string) => (name === "printi" ? 576 : 384)

export const api_queue = new Queue<BWImage>()

export const fetch_uint8 = async (url: string) => new Uint8Array(await (await fetch(url)).arrayBuffer())

// const dino_png = fetch_uint8(dino_url)
//     .then((r) => dither_bytes_to_bwimage(r, printer_size("printi")))
//     .then((r) => to_png(r))
//     .catch(() => new Uint8Array([]))

export const api_router = (timeout_ms: number = 30 * 1000, max_size: number = 10 * 1024 * 1024) =>
    new Router({
        prefix: "/api",
    })
        .get("/", (ctx) => {
            ctx.response.body = "it's api time!!!!!"
        })
        .get("/makecoffee", (ctx) => {
            ctx.response.body = "sorry :( 🙅☕ "
            ctx.response.status = 418
        })
        .get("/hello/:name?", (ctx) => {
            ctx.response.body = `Hello ${ctx.params.name}!`
        })
        .get("/nextinqueue/:printername?", async (ctx) => {
            const printer_name = ctx.params.printername ?? "printi"

            const accept = ctx.request.headers.get("accept") ?? "*/*"
            const output_png = accept.includes("image/png") || accept.includes("text/html")

            let item: null | BWImage = null
            try {
                item = await api_queue.wait_for_item(printer_name, timeout_ms)

                if (output_png) {
                    ctx.response.body = to_png(item)
                    ctx.response.type = "image/png"
                } else {
                    ctx.response.body = to_h58(item)
                    ctx.response.type = "application/octet-stream"
                }
            } catch (e) {
                ctx.response.body = `Nothing received! ${e.message}`
                ctx.response.status = 404
            }
            ctx.request.originalRequest.donePromise.catch(() => {
                if (item) {
                    console.debug("Failed to write nextinqueue response, adding item back to queue.")
                    // add it back to the queue, because the client did not receive it!
                    api_queue.add_to_queue(printer_name, item)
                }
            })
        })
        .post("/submitimages/:printername?", async (ctx) => {
            const printer_name = ctx.params.printername ?? "printi"

            const body = await ctx.request.body()
            const value = await body.value

            let num_added = 0

            if (body.type === "json") {
                await Promise.all(
                    value?.images?.map(async (img_str: string) => {
                        if (img_str.length < max_size) {
                            const data_url = "data:application/octet-stream;base64," + img_str
                            const data_bytes = await fetch_uint8(data_url)
                            const bwimage = await dither_bytes_to_bwimage(data_bytes, printer_size(printer_name))
                            api_queue.add_to_queue(printer_name, bwimage)
                            num_added++
                        }
                    }) ?? []
                )
            } else if (body.type === "form-data") {
                if (value instanceof FormDataReader) {
                    for await (const [_filename, data_ref] of value.stream({
                        maxFileSize: max_size,
                        maxSize: max_size,
                    })) {
                        const data = typeof data_ref === "string" ? data_ref : data_ref.content
                        if (data instanceof Uint8Array) {
                            api_queue.add_to_queue(printer_name, await dither_bytes_to_bwimage(data, printer_size(printer_name)))
                            num_added++
                        }
                    }
                }
            } else {
                console.log("/submitimages/: Unknown body type: ", body.type, value)
            }

            ctx.response.body = `${num_added} image(s) submitted to the queue`
        })
// .get("/dino", (ctx) => {
//     ctx.response.body = dino_png
//     ctx.response.type = "image/png"
// })
