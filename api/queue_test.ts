import { assertEquals, assert, assertRejects } from "https://deno.land/std@0.122.0/testing/asserts.ts"

import { Queue } from "./queue.ts"

Deno.test("Queue 1", async () => {
    const q = new Queue<Record<string, number>>()

    let start = Date.now()

    q.add_to_queue("test_printer1", { a: 1 })
    q.add_to_queue("test_printer2", { a: 2 })
    q.add_to_queue("test_printer2", { a: 22 })

    const item1 = await q.wait_for_item("test_printer1", 3000)
    const item2 = await q.wait_for_item("test_printer2", 3000)
    const item22 = await q.wait_for_item("test_printer2", 3000)

    assert(Date.now() - start < 100, "Too slow!")

    assertEquals(item1, { a: 1 })
    assertEquals(item2, { a: 2 })
    assertEquals(item22, { a: 22 })

    await assertRejects(() => q.wait_for_item("test_printer1", 100))
    await assertRejects(() => q.wait_for_item("test_printer2", 100))
})

Deno.test("Queue 2", async () => {
    const q = new Queue<Record<string, number>>()

    let start = Date.now()

    let promise1 = q.wait_for_item("test_printer1", 500)
    let promise2 = q.wait_for_item("test_printer1", 500)
    let promise3 = q.wait_for_item("test_printer1", 500)

    q.add_to_queue("test_printer1", { a: 1 })
    q.add_to_queue("test_printer1", { a: 2 })
    q.add_to_queue("test_printer1", { a: 3 })

    assert(Date.now() - start < 100, "Too slow!")

    assertEquals(await promise1, { a: 1 })
    assertEquals(await promise2, { a: 2 })
    assertEquals(await promise3, { a: 3 })

    // assertRejects(() => q.wait_for_item("test_printer1", 50))
})
