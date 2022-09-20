import { assertEquals, assert, assertRejects } from "https://deno.land/std@0.156.0/testing/asserts.ts"

import { fitting_size } from "./resize.ts"

Deno.test("Resize", () => {
    const cool = ([width, height]: [number, number], max_width: number) => {
        const { size, rotated } = fitting_size([width, height], max_width)
        return [...size, rotated]
    }

    // smaller, should never rotate
    assertEquals(cool([6, 6], 8), [6, 6, false])
    assertEquals(cool([6, 7], 8), [6, 7, false])
    assertEquals(cool([6, 5], 8), [6, 5, false])
    assertEquals(cool([6, 9], 9), [6, 9, false])
    assertEquals(cool([9, 6], 9), [9, 6, false])

    // bigger
    assertEquals(cool([10, 20], 4), [4, 8, false])
    assertEquals(cool([20, 10], 4), [4, 8, true])
    assertEquals(cool([20, 20], 4), [4, 4, false])

    // in the middle
    assertEquals(cool([8, 18], 12), [8, 18, false])
    assertEquals(cool([18, 8], 12), [8, 18, true])
    assertEquals(cool([12, 12], 12), [12, 12, false])
})
