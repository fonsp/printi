import { parse } from "https://deno.land/std@0.122.0/flags/mod.ts"
import { Application } from "./imports/oak.ts"
import { api_router } from "./api_router.ts"

const app = new Application()

// Allow CORS
app.use(async (ctx, next) => {
    await next()
    ctx.response.headers.set("Access-Control-Allow-Origin", "*")
})

// Use our API router
app.use(api_router.routes())
app.use(api_router.allowedMethods())

// Send static content
app.use(async (context) => {
    await context.send({
        root: `${Deno.cwd()}/api/static`,
        index: "index.html",
    })
})

// 404 message (only runs if the router did not match)
app.use((ctx) => {
    console.log("404 for path: ", ctx.request.url.href)
    ctx.response.body = "Not found!"
    ctx.response.status = 404
})

// Config
const port = parse(Deno.args).port ?? 8000

// Run the app
console.log(`Listening on port ${port}`)
await app.listen({ port })
