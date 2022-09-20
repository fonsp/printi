export function fitting_size([width, height]: [number, number], max_width: number): { size: [number, number]; rotated: boolean } {
    const minor = Math.min(width, height)
    const major = Math.max(width, height)
    if (minor <= max_width) {
        return {
            size: width > max_width ? [height, width] : [width, height],
            rotated: width > max_width,
        }
    } else {
        const new_minor = max_width
        const new_major = Math.floor(major * (new_minor / minor))
        return {
            size: [new_minor, new_major],
            rotated: width > height,
        }
    }
}
