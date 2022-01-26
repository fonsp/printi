import { assertEquals, assert, assertRejects } from "https://deno.land/std@0.122.0/testing/asserts.ts"

import { dither_url_to_png_data } from "./dither.ts"
import { add_to_queue, wait_for_item } from "./queue.ts"

Deno.test("Queue 1", async () => {
    let start = Date.now()

    add_to_queue("test_printer1", { a: 1 })
    add_to_queue("test_printer2", { a: 2 })
    add_to_queue("test_printer2", { a: 22 })

    const item1 = await wait_for_item("test_printer1", 3000)
    const item2 = await wait_for_item("test_printer2", 3000)
    const item22 = await wait_for_item("test_printer2", 3000)

    assert(Date.now() - start < 100, "Too slow!")

    assertEquals(item1, { a: 1 })
    assertEquals(item2, { a: 2 })
    assertEquals(item22, { a: 22 })

    await assertRejects(() => wait_for_item("test_printer1", 100))
    await assertRejects(() => wait_for_item("test_printer2", 100))
})

Deno.test("Queue 2", async () => {
    let start = Date.now()

    let promise1 = wait_for_item("test_printer1", 500)
    let promise2 = wait_for_item("test_printer1", 500)
    let promise3 = wait_for_item("test_printer1", 500)

    add_to_queue("test_printer1", { a: 1 })
    add_to_queue("test_printer1", { a: 2 })
    add_to_queue("test_printer1", { a: 3 })

    assert(Date.now() - start < 100, "Too slow!")

    assertEquals(await promise1, { a: 1 })
    assertEquals(await promise2, { a: 2 })
    assertEquals(await promise3, { a: 3 })

    // assertRejects(() => wait_for_item("test_printer1", 50))
})

Deno.test("Dither 1", async () => {
    const dino_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/Xixia_Dinosaur_Park_12.jpg/160px-Xixia_Dinosaur_Park_12.gif"

    const result = await dither_url_to_png_data(dino_url)
    assert(result.length > 100)
})
