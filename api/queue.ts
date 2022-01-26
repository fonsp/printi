import * as _ from "./imports/lodash.ts"

type item_type = Record<string, unknown>

const queues = new Map<string, Array<item_type>>()

const waiting = new Map<string, Array<Function>>()

export const add_to_queue = (printername: string, data: item_type) => {
    console.log("Adding to queue: ", printername, data)
    if (waiting.has(printername)) {
        console.log("Waiters for this printer")
        const waiters = waiting.get(printername)!
        if (waiters.length > 0) {
            const item = _.first(waiters)
            console.log("Found", item)
            waiters.splice(waiters.indexOf(item), 1)
            if (waiters.length === 0) waiting.delete(printername)
            item(data)
            return
        }
    }

    if (!queues.has(printername)) {
        queues.set(printername, [])
    }
    queues.get(printername)!.push(data)
}

export const wait_for_item = async (printername: string, timeout: number) => {
    if (queues.has(printername)) {
        const queue = queues.get(printername)!
        if (queue.length > 0) {
            const item = queue.shift()!
            if (queue.length === 0) queues.delete(printername)
            return item
        }
    }

    if (!waiting.has(printername)) {
        waiting.set(printername, new Array<Function>())
    }
    const waiters = waiting.get(printername)!
    const waiter = new Promise<item_type>((resolve, reject) => {
        const handle_ref = { current: -1 }
        const f = (data: item_type) => {
            resolve(data)
            if (handle_ref.current !== -1) {
                clearTimeout(handle_ref.current)
            }
        }
        handle_ref.current = setTimeout(() => {
            const index = waiters.indexOf(f)
            if (index !== -1) {
                waiters.splice(index, 1)
                reject(new Error("timeout"))
            }
        }, timeout)
        waiters.push(f)
    })
    return await waiter
}
