import { Router } from "https://deno.land/x/oak@v10.1.0/mod.ts"

import { dither_url_to_png_data } from "./dither.ts"
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
    .get("/add_to_queue/:printername?", async (ctx) => {
        add_to_queue(ctx.params.printername ?? "printi", { data: await dither_url_to_png_data(dino_url) })
        ctx.response.body = `Added ${ctx.params.printername} to queue!`
    })
    .get("/next_in_queue/:printername?", async (ctx) => {
        try {
            const item = await wait_for_item(ctx.params.printername ?? "printi", 5000)

            ctx.response.body = item.data as Uint8Array
            ctx.response.type = "image/png"
        } catch (e) {
            ctx.response.body = `Nothing received! ${e.message}`
            ctx.response.status = 404
        }
    })
    .get("/dino", async (ctx) => {
        ctx.response.body = await dither_url_to_png_data(dino_url)
        ctx.response.type = "image/png"
    })

// .post("/submitimages/:printername")
