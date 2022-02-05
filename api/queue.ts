import * as _ from "./imports/lodash.ts"

type item_type = Record<string, unknown>

/** Queues with printis for each printer. These have not yet been printed, and are available on request. */
export const queues = new Map<string, Array<item_type>>()

/** A short-circuit for `queues`: if a printer is *currently* waiting for the latest and greatest printi, then its callback function is stored here.
 *
 * When a new printi arrives, we first check this map to see if anyone wants it *right now*, and if not, it is added to the `queues`. */
export const waiting = new Map<string, Array<(found: item_type) => void>>()

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

/** Return a promise that resolves:
 * - if printis were already waiting, then the next printi is returned
 * - if not, then the promise waits for the next printi to arrive, and returns it
 *
 * If no printis were queued, and nothing arrives within `timeout` ms, then the promise rejects.
 *
 * @param printername The name of the printer to get printis for
 * @param timeout The timeout in ms
 * @returns A promise that resolves with the next printi
 */
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
        waiting.set(printername, new Array<(found: item_type) => void>())
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
