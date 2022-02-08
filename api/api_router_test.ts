import { Application } from "https://deno.land/x/oak@v10.1.0/mod.ts"
import { superoak } from "https://deno.land/x/superoak@4.6.0/mod.ts"

import { api_router } from "./api_router.ts"
import { assertEquals, assert, assertStringIncludes } from "https://deno.land/std@0.122.0/testing/asserts.ts"

const app = new Application({
    logErrors: false,
})
const timeout = 1000
const router = api_router(timeout)
app.use(router.routes())
app.use(router.allowedMethods())

// Send simple GET request
Deno.test("it should support the Oak framework", async () => {
    const request = await superoak(app)
    await request.get("/api/").then((r) => {
        assertEquals(r.status, 200)
        assertStringIncludes(r.text, "api time")
    })
})

Deno.test("Submit files with FormData", async () => {
    const lijntje2_bytes = await Deno.readFile("api/static/lijntje2.png")

    let request = await superoak(app)

    await request
        .post("/api/submitimages/testprinter1")
        .attach("img1", new File([lijntje2_bytes], "img1.png"))
        .attach("img2", new File([lijntje2_bytes], "img1.png"))
        .then((r) => {
            assertEquals(r.status, 200)
            assertEquals(r.text, "2 image(s) submitted to the queue")
        })

    for (let i = 0; i < 2; i++) {
        request = await superoak(app)
        await request
            .get("/api/nextinqueue/testprinter1")
            .set("Accept", "*/*")
            .timeout({
                deadline: 3000,
            })
            .then((r) => {
                assertEquals(r.status, 200)
            })
    }

    // request the next image, but cancel the request very quickly
    request = await superoak(app)
    await request
        .get("/api/nextinqueue/testprinter1")
        .set("Accept", "*/*")
        .timeout({
            deadline: 200,
        })
        .catch((error) => {
            assert(error.timeout)
        })

    request = await superoak(app)
    await request
        .get("/api/nextinqueue/testprinter1")
        .set("Accept", "*/*")
        .then((r) => {
            assertEquals(r.status, 404)
        })
})

Deno.test("Cancelled requests", async () => {
    const lijntje2_bytes = await Deno.readFile("api/static/lijntje2.png")

    let request = await superoak(app)

    // request the next image, but cancel the request very quickly
    const fast_deadline = 200
    await request
        .get("/api/nextinqueue/testprinter2")
        .set("Accept", "*/*")
        .timeout({
            deadline: fast_deadline,
        })
        .catch((error) => {
            assert(error.timeout)
        })

    // the original request should still be processing on the server! But when resolved, it will add itself back into the queue.
    // submit two images
    request = await superoak(app)
    await request
        .post("/api/submitimages/testprinter2")
        .attach("img1", new File([lijntje2_bytes], "img1.png"))
        .attach("img2", new File([lijntje2_bytes], "img1.png"))
        .then((r) => {
            assertEquals(r.status, 200)
            assertEquals(r.text, "2 image(s) submitted to the queue")
        })

    // request the next two images, this should work!
    for (let i = 0; i < 2; i++) {
        request = await superoak(app)
        await request
            .get("/api/nextinqueue/testprinter2")
            .set("Accept", "*/*")
            .timeout({
                deadline: 3000,
            })
            .then((r) => {
                assertEquals(r.status, 200)
            })
    }

    request = await superoak(app)
    await request
        .get("/api/nextinqueue/testprinter2")
        .set("Accept", "*/*")
        .then((r) => {
            assertEquals(r.status, 404)
        })
})

const base64_arraybuffer = async (data: Uint8Array) => {
    // Use a FileReader to generate a base64 data URI
    const base64url = (await new Promise((r) => {
        const reader = new FileReader()
        reader.onload = () => r(reader.result as string)
        reader.readAsDataURL(new Blob([data]))
    })) as string

    /*
    The result looks like 
    "data:application/octet-stream;base64,<your base64 data>", 
    so we split off the beginning:
    */
    return base64url.split(",", 2)[1]
}
assertEquals(await base64_arraybuffer(new Uint8Array([1, 2, 3])), "AQID")

Deno.test("Submit files with JSON", async () => {
    const lijntje2_bytes = await Deno.readFile("api/static/lijntje2.png")
    const base64data = await base64_arraybuffer(lijntje2_bytes)

    const request = await superoak(app)
    await request
        .post("/api/submitimages/testprinter3")
        .send(
            JSON.stringify({
                images: [base64data, base64data],
            })
        )
        .set("Content-Type", "application/json")
        .then((r) => {
            assertEquals(r.status, 200)
            assertEquals(r.text, "2 image(s) submitted to the queue")
        })
})
