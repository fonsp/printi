import { FormDataReader, Router } from "https://deno.land/x/oak@v10.1.0/mod.ts"
import { BWImage } from "./BWImage.ts"

import { dither_bytes_to_bwimage } from "./dither.ts"
const dino_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/Xixia_Dinosaur_Park_12.jpg/160px-Xixia_Dinosaur_Park_12.gif"

import { Queue } from "./queue.ts"
import { to_png, to_h58 } from "./h58.ts"

const printer_size = (name: string) => (name === "printi" ? 576 : 384)

const api_queue = new Queue<BWImage>()

const fetch_uint8 = async (url: string) => new Uint8Array(await (await fetch(url)).arrayBuffer())

export const api_router = new Router({
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
    // .get("/add_to_queue/:printername?", async (ctx) => {
    //     add_to_queue(ctx.params.printername ?? "printi", { data: await dither_url_to_png_data(dino_url) })
    //     ctx.response.body = `Added ${ctx.params.printername} to queue!`
    // })
    .get("/nextinqueue/:printername?", async (ctx) => {
        const accept = ctx.request.headers.get("accept") ?? "*/*"
        const output_png = accept.includes("image/png") || accept.includes("text/html")

        console.log({ output_png })
        // ctx.request.url.searchParams
        try {
            const item = await api_queue.wait_for_item(ctx.params.printername ?? "printi", 30 * 1000)

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
    })
    .post("/submitimages/:printername?", async (ctx) => {
        const printer_name = ctx.params.printername ?? "printi"

        const body = await ctx.request.body()
        const value = await body.value
        console.log(body, value)
        if (body.type === "json") {
            await Promise.all(
                value?.images?.map(async (img_str: string) => {
                    const data_url = "data:application/octet-stream;base64," + img_str
                    api_queue.add_to_queue(printer_name, await dither_bytes_to_bwimage(await fetch_uint8(data_url), printer_size(printer_name)))
                }) ?? []
            )
        } else if (body.type === "form-data") {
            if (value instanceof FormDataReader) {
                for await (const [_filename, data_ref] of value.stream({
                    maxFileSize: 10 * 1024 * 1024,
                    maxSize: 10 * 1024 * 1024,
                })) {
                    const data = typeof data_ref === "string" ? data_ref : data_ref.content
                    if (data instanceof Uint8Array) {
                        api_queue.add_to_queue(printer_name, await dither_bytes_to_bwimage(data, printer_size(printer_name)))
                    }
                }
            }
        } else {
            console.log("Unknown body type: ", body.type, value)
        }

        ctx.response.body = `Added ${ctx.params.printername} to queue!`
    })
    .get("/dino", async (ctx) => {
        ctx.response.body = to_png(await dither_bytes_to_bwimage(await fetch_uint8(dino_url), printer_size("printi")))
        ctx.response.type = "image/png"
        // let x = await dither_bytes_to_bwimage(await fetch_uint8(dino_url), printer_size("printi"))
        // ctx.response.body = JSON.stringify(x)
        // ctx.response.type = "text/plain"
    })

// .post("/submitimages/:printername")
