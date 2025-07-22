import { FormDataReader, Router } from "./imports/oak.ts"
import { BWImage } from "./BWImage.ts"
import * as _ from "./imports/lodash.ts"

import { dither_bytes_to_bwimage } from "./dither.ts"
// const dino_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/Xixia_Dinosaur_Park_12.jpg/160px-Xixia_Dinosaur_Park_12.png"

import { Queue } from "./queue.ts"
import { to_png, to_h58 } from "./h58.ts"
import { Handlebars } from "./imports/handlebars.ts"

const printer_size = (name: string) => (name === "printi" ? 576 : 384)

// Shared Handlebars instance
const handlebars = new Handlebars({
    baseDir: "./templates",
    extname: ".hbs",
    partialsDir: "", // Disable partials
    defaultLayout: "" // Disable layouts
})

export const api_queue = new Queue<BWImage>()

export const fetch_uint8 = async (url: string) => new Uint8Array(await (await fetch(url)).arrayBuffer())

// const dino_png = fetch_uint8(dino_url)
//     .then((r) => dither_bytes_to_bwimage(r, printer_size("printi")))
//     .then((r) => to_png(r))
//     .catch(() => new Uint8Array([]))

// Helper to determine the server base URL
function get_server_base_url(ctx: any): string {
    // Try env var first
    const env_url = Deno.env.get("PRINTI_SERVER_BASE_URL")
    if (env_url) {
        return env_url
    }

    // Try first from reverse proxy headers, then from request URL
    const forwarded_host = ctx.request.headers.get("x-forwarded-host")
    if (forwarded_host) {
        // If it's gone through a reverse proxy, assume HTTPS
        // instead of relying on X-Forwarded-Proto, which only some seem to set.
        return `https://${forwarded_host}`
    }

    // Fallback to request URL
    return ctx.request.url.protocol.replace(":", "") + "//" + ctx.request.url.host
}

export const api_router = ({ timeout_ms = 30 * 1000, max_size = 10 * 1024 * 1024, inspection_password = crypto.randomUUID() } = {}) =>
    new Router({
        // prefix: "/api",
    })
        .get("/makecoffee", (ctx) => {
            ctx.response.body = "sorry :( 🙅☕ "
            ctx.response.status = 418
        })
        .get("/hello/:name?", (ctx) => {
            ctx.response.body = `Hello ${ctx.params.name}!`
        })
        .get("/queuesize", (ctx) => {
            const auth_val = ctx.request.headers.get("authorization") ?? ""

            const [_username, password] = atob(auth_val.replace(/^Basic /, "")).split(":")

            if (password === inspection_password) {
                const total_length = _.sum([...api_queue.queues.values()].map((x) => x.length))
                ctx.response.body = `${api_queue.queues.size} queues and ${total_length} items.`
            } else {
                ctx.response.status = 401
                ctx.response.body = `Unauthorized`
                ctx.response.headers.set("WWW-Authenticate", `Basic charset="UTF-8"`)
            }
        })
        .get("/nextinqueue/:printername?", async (ctx) => {
            const printer_name = ctx.params.printername ?? "printi"

            const accept = ctx.request.headers.get("accept") ?? "*/*"
            const output_png = accept.includes("image/png") || accept.includes("text/html") || printer_name === "printi" // TODO: this last check should not be there, but I forgot my raspberry pi password so I can't update the client to include the Accept: image/png header :(

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
                const filename = `printi-${crypto.randomUUID()}.${output_png ? `png` : `h58`}`
                ctx.response.headers.set("Content-Disposition", `inline; filename=\"${filename}\"`)
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
                console.debug("/submitimages/: Unknown body type: ", body.type, value)
            }

            ctx.response.type = "text/html"
            ctx.response.body = "<html><body><h1>Printi is printing!</h1><script>setTimeout(() => { window.close() }, 2000)</script></body></html>"
        })
        .get(["/:printername", "/"], async (ctx) => {
            const printer_name = ctx.params.printername ?? "printi"
            const server_base_url = get_server_base_url(ctx)
            ctx.response.body = await handlebars.renderView("printer.html", { PRINTER_NAME: printer_name, SERVER_BASE_URL: server_base_url })
            ctx.response.type = "text/html"
        })
        .get("/:printername/manifest.json", async (ctx) => {
            const printer_name = ctx.params.printername ?? "printi"
            ctx.response.body = await handlebars.renderView("manifest.json", { PRINTER_NAME: printer_name })
            ctx.response.type = "application/manifest+json"
        })
