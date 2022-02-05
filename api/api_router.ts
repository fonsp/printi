import { FormDataReader, Router } from "https://deno.land/x/oak@v10.1.0/mod.ts"

import { dither_bytes_to_png_data, dither_url_to_png_data } from "./dither.ts"
const dino_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/Xixia_Dinosaur_Park_12.jpg/160px-Xixia_Dinosaur_Park_12.gif"

import { add_to_queue, wait_for_item } from "./queue.ts"

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
    .get("/next_in_queue/:printername?", async (ctx) => {
        try {
            const item = await wait_for_item(ctx.params.printername ?? "printi", 30 * 1000)

            ctx.response.body = item.data as Uint8Array
            ctx.response.type = "image/png"
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
                    add_to_queue(printer_name, { data: await dither_url_to_png_data(data_url) })
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
                        add_to_queue(printer_name, { data: await dither_bytes_to_png_data(data) })
                    }
                }
            }
        } else {
            console.log("Unknown body type: ", body.type, value)
        }

        ctx.response.body = `Added ${ctx.params.printername} to queue!`
    })
    .get("/dino", async (ctx) => {
        ctx.response.body = await dither_url_to_png_data(dino_url)
        ctx.response.type = "image/png"
    })

// .post("/submitimages/:printername")
