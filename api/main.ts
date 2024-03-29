import { parse } from "https://deno.land/std@0.156.0/flags/mod.ts"
import { Application } from "./imports/oak.ts"
import { api_router } from "./api_router.ts"

// Config
const { port = 8000, password } = parse(Deno.args)

const app = new Application()

// Allow CORS
app.use(async (ctx, next) => {
    await next()
    ctx.response.headers.set("Access-Control-Allow-Origin", "*")
    if (ctx.request.headers.has("Access-Control-Request-Method")) {
        ctx.response.headers.set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
    }
    if (ctx.request.headers.has("Access-Control-Request-Headers")) {
        ctx.response.headers.set(
            "Access-Control-Allow-Headers",
            ctx.request.headers.get("Access-Control-Request-Headers") ?? "Origin, X-Requested-With, Content-Type, Accept, Authorization"
        )
    }
})

// Use our API router
const router = api_router({ inspection_password: password })
app.use(router.routes())
app.use(router.allowedMethods())

// Send static content
// app.use(async (context) => {
//     await context.send({
//         root: `${Deno.cwd()}/api/static`,
//         index: "index.html",
//     })
// })

// 404 message (only runs if the router did not match)
app.use((ctx) => {
    // console.log("404 for path: ", ctx.request.url.href)
    ctx.response.body = "Not found!"
    ctx.response.status = 404
})

// Run the app
console.log(`Listening on port ${port}`)
await app.listen({ port })
