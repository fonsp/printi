import { assertEquals, AssertionError } from "https://deno.land/std@0.122.0/testing/asserts.ts"

import { dither_bytes_to_bwimage } from "./dither.ts"
import { to_h58 } from "./h58.ts"
import { fetch_uint8 } from "./api_router.ts"
import * as _ from "./imports/lodash.ts"

export function assertStringStartsWith(actual: string, expected: string): void {
    if (expected.length > actual.length) {
        throw new AssertionError(`actual: "${actual}" expected to start with: "${expected}"`)
    } else {
        assertEquals(actual.substring(0, expected.length), expected)
    }
}

const hexstring = (data: Uint8Array) =>
    Array.from(data)
        .map((val) => ("0" + val.toString(16)).slice(-2))
        .join(" ")

// deno-lint-ignore no-unused-vars
const newline_every_n = (s: string, n: number) =>
    _.chunk(Array.from(s), n, null)
        .map((chunk: string[]) => chunk.join("") + "\n")
        .join("")

Deno.test("Dither 1", async (t) => {
    // Our test image is already monochrome (only pure black and pure white), which means that our dithering algortihm will be lossless. So we are not testing whether the dithering algorithm "looks good", we are only testing whether the *rest* of our processing behaves as expected, including the h58 printer output.
    //
    // In this test, we will check that the output of our pipeline exactly equals what we expect by looking at the black pixels in the test image. 👀
    //
    const lijntje2_bytes = await Deno.readFile("api/static/lijntje2.png")

    // const lijntje2_bytes = await fetch_uint8(lijntje2_url)

    const bwimage = await dither_bytes_to_bwimage(lijntje2_bytes, 384)

    await t.step("Dither data check", () => {
        const bwdata_should_be = `
                    80 00 00 02
                    40 00 00 02
                    20 00 00 02
                    10 00 00 02
                    08 00 00 02
                    04 00 00 02
                    02 00 00 02
                    01 00 00 00
                    00 80 00 00
                    00 40 00 00
                    00 20 00 00
                    00 10 00 00
                    00 08 00 00
                    00 00 00 00
                    00 00 00 00
                    07 80 00 00
                    00 40 00 00
                    00 30 00 00
                    00 0c 00 00
                    00 02 00 00
                    00 01 00 00
                    00 00 80 00
                    00 00 40 00
                    00 00 40 00
                
                    
                    00 00 40 00
                    00 00 80 00
                    03 ff 00 00
                    1c 00 00 00
                    30 00 00 00
                    40 00 03 fe
                    `
            .replaceAll(/\s+/g, " ")
            .trim()

        assertEquals(hexstring(bwimage.bit_data), bwdata_should_be)
    })

    await t.step("h58", () => {
        const h58data = to_h58(bwimage)

        const h58_should_be = `1b 40
                        1d 76 30 00
                            04 00   18 00
                                    
                            80 00 00 02
                            40 00 00 02
                            20 00 00 02
                            10 00 00 02
                            08 00 00 02
                            04 00 00 02
                            02 00 00 02
                            01 00 00 00
                            00 80 00 00
                            00 40 00 00
                            00 20 00 00
                            00 10 00 00
                            00 08 00 00
                            00 00 00 00
                            00 00 00 00
                            07 80 00 00
                            00 40 00 00
                            00 30 00 00
                            00 0c 00 00
                            00 02 00 00
                            00 01 00 00
                            00 00 80 00
                            00 00 40 00
                            00 00 40 00
                            
                        1b 4a 15
                        1d 76 30 00
                            04 00   06 00
            
                            00 00 40 00
                            00 00 80 00
                            03 ff 00 00
                            1c 00 00 00
                            30 00 00 00
                            40 00 03 fe
                            
                        1b 4a 15
                    1b 40`
            .replaceAll(/\s+/g, " ")
            .trim()

        assertEquals(hexstring(h58data), h58_should_be)
    })
})

Deno.test("Dither big image", async (t) => {
    const big_image_url = "https://upload.wikimedia.org/wikipedia/commons/c/c6/Xixia_Dinosaur_Park_12.jpg"
    const original_size = [3168, 4752]

    const big_image_bytes = await fetch_uint8(big_image_url)

    const bwimage = await dither_bytes_to_bwimage(big_image_bytes, 384)

    assertEquals(Math.floor((original_size[1] / original_size[0]) * 384), 576)
    const expected_size = [384, 576]

    assertEquals(bwimage.size, expected_size)
    const expected_length = (expected_size[0] * expected_size[1]) / 8
    assertEquals(bwimage.bit_data.length, expected_length)

    await t.step("h58", () => {
        const h58data = to_h58(bwimage)
        const str = hexstring(h58data)

        assertEquals(0x30, 384 / 8)

        const should_start = `
        1b 40
            1d 76 30 00
                30 00   18 00
        `
            .replaceAll(/\s+/g, " ")
            .trim()

        assertStringStartsWith(str, should_start)
    })
})
