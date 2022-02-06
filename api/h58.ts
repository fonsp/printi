import {
    createCanvas,
    ImageData,
    EmulatedImageData,
    ImageDataConstructor,
    loadImage,
    EmulatedCanvas2D,
    EmulatedCanvas2DContext,
} from "https://deno.land/x/canvas@v1.4.1/mod.ts"
import { BWImage } from "./BWImage.ts"

import * as _ from "./imports/lodash.ts"

/*
public byte[] RasterToPrintCommands(BWImage raster)
    {

        int width = raster.size.Width;
        int height = raster.size.Height;
        int dotsPerLine = Math.Min(width, pageWidth);
        int bytesPerLine = dotsPerLine / 8;
        if(dotsPerLine != bytesPerLine * 8)
        {
            throw new InvalidDataException("raster width should be a multiple of 8");
        }

        List<byte> output = new List<byte>(bytesPerLine * height + 1);

        IEnumerable<bool> rasterData;
        int byteCount = raster.data.Length / 8;

        if(raster.data.Length % 8 != 0)
        {
            int needed = 8 - (raster.data.Length % 8);
            var rasterDataList = raster.data.ToList();
            for(int i = 0; i < needed; i++)
            {
                rasterDataList.Add(false);
            }
            rasterData = rasterDataList.AsEnumerable().Reverse();
            byteCount++;
        }
        else
        {
            rasterData = raster.data.Reverse();
        }

        BitArray compactedBits = new BitArray(rasterData.ToArray());
        byte[] compactedByteBufferArray = new byte[byteCount];
        compactedBits.CopyTo(compactedByteBufferArray, 0);

        var compactedByteBuffer = compactedByteBufferArray.Reverse();

        output.AddRange(new byte[] { 0x1b, 0x40 });
        for(int y = 0; y < height; y += 24)
        {
            int sliceHeight = Math.Min(24, height - y);

            output.AddRange(new byte[] { 0x1d, 0x76, 0x30, 0x00 });
            output.Add((byte)(bytesPerLine % 256));
            output.Add((byte)(bytesPerLine / 256));
            output.Add((byte)(sliceHeight % 256));
            output.Add((byte)(sliceHeight / 256));

            output.AddRange(compactedByteBuffer.Skip(y * bytesPerLine).Take(bytesPerLine * sliceHeight));

            output.AddRange(new byte[] { 0x1b, 0x4a, 0x15 });
        }

        output.AddRange(new byte[] { 0x1b, 0x40 });

        return output.ToArray();
    }
 */

export const to_png = (raster: BWImage) => {
    const rgba_data = new Uint8ClampedArray(raster.bit_data.length * 8 * 4)
    Array.from(raster.bit_data).forEach((byte, i) => {
        for (let j = 0; j < 8; j++) {
            const black = ((byte >> j) & 1) > 0

            rgba_data.set(black ? [0, 0, 0, 255] : [255, 255, 255, 255], i * 8 * 4 + j * 4)
        }
    })
    // const rgba_data = Array.from(raster.bit_data).flatMap((byte) => {
    //     const bits = new Array(8).fill(0).map((_, i) => (byte & (1 << i)) > 0)
    //     return bits.flatMap((black) => (black ? [0, 0, 0, 255] : [255, 255, 255, 255]))
    // })

    const canvas = createCanvas(raster.size[0], raster.size[1])
    const ctx = canvas.getContext("2d")

    const imageData: ImageData = {
        data: rgba_data,
        width: raster.size[0],
        height: raster.size[1],
    }
    ctx.putImageData(imageData, 0, 0)
    return canvas.toBuffer()
}

export const to_h58 = (raster: BWImage): Uint8Array => {
    const [width, height] = raster.size
    const dotsPerLine = width
    const bytesPerLine = Math.floor(dotsPerLine / 8)
    if (dotsPerLine != bytesPerLine * 8) {
        throw new Error("raster width should be a multiple of 8")
    }

    // const rasterData = raster.data
    // let byteCount = rasterData.length / 8

    // if (rasterData.length % 8 != 0) {
    //     const needed = 8 - (rasterData.length % 8)
    //     const rasterDataList = rasterData.slice()
    //     for (let i = 0; i < needed; i++) {
    //         rasterDataList.push(false)
    //     }
    //     const rasterDataReverse = rasterDataList.reverse()
    //     byteCount++
    // } else {
    // }
    // const compactedBits = compated_bits(rasterDataReverse).reverse()

    const num_slices = Math.ceil(height / 24)
    const output = new Uint8Array(4 + bytesPerLine * height + num_slices * (4 + 4 + 3))
    let output_offset = 0
    const add_to_output = (bytes: ArrayLike<number>) => {
        output.set(bytes, (output_offset += bytes.length))
    }
    add_to_output([0x1b, 0x40])
    for (let y = 0; y < height; y += 24) {
        const sliceHeight = Math.min(24, height - y)

        add_to_output([0x1d, 0x76, 0x30, 0x00])
        add_to_output([bytesPerLine & 255, bytesPerLine >> 8])
        add_to_output([sliceHeight & 255, sliceHeight >> 8])

        add_to_output(raster.bit_data.subarray(y * bytesPerLine, (y + sliceHeight) * bytesPerLine))

        add_to_output([0x1b, 0x4a, 0x15])
    }
    add_to_output([0x1b, 0x40])
    return output
}

const compacted_bits = (data: boolean[]): Uint8Array => {
    const bits = new Uint8Array(Math.ceil(data.length / 8))
    for (let i = 0; i < data.length; i += 8) {
        bits[Math.floor(i / 8)] =
            (data[i + 0] ? 128 : 0) |
            (data[i + 1] ? 64 : 0) |
            (data[i + 2] ? 32 : 0) |
            (data[i + 3] ? 16 : 0) |
            (data[i + 4] ? 8 : 0) |
            (data[i + 5] ? 4 : 0) |
            (data[i + 6] ? 2 : 0) |
            (data[i + 7] ? 1 : 0)
    }
    return bits
}

// // Convert the above C# code into a TypeScript function:
// export const raster_to_print_commands = (raster: ImageData, pageWidth: number): Uint8Array => {
//     const width = raster.width
//     const height = raster.height
//     const dotsPerLine = Math.min(width, pageWidth)
//     const bytesPerLine = dotsPerLine / 8
//     if (dotsPerLine != bytesPerLine * 8) {
//         throw new Error("raster width should be a multiple of 8")
//     }

//     const rasterData = raster.data
//     let byteCount = rasterData.length / 8

//     if (rasterData.length % 8 != 0) {
//         const needed = 8 - (rasterData.length % 8)
//         const rasterDataList = rasterData.slice()
//         for (let i = 0; i < needed; i++) {
//             rasterDataList.push(false)
//         }
//         const rasterDataReverse = rasterDataList.reverse()
//         byteCount++
//     } else {
//     }
//     const compactedBits = compated_bits(rasterDataReverse).reverse()

//     const num_slices = Math.ceil(height / 24)
//     const output = new Uint8Array(4 + bytesPerLine * height + num_slices * (4 + 4 + 3))
//     let output_offset = 0
//     const add_to_output = (bytes: ArrayLike<number>) => {
//         output.set(bytes, (output_offset += bytes.length))
//     }
//     add_to_output([0x1b, 0x40])
//     for (let y = 0; y < height; y += 24) {
//         const sliceHeight = Math.min(24, height - y)

//         add_to_output([0x1d, 0x76, 0x30, 0x00])
//         add_to_output([bytesPerLine & 255, bytesPerLine >> 8])
//         add_to_output([sliceHeight & 255, sliceHeight >> 8])

//         add_to_output(compactedByteBuffer.subarray(y * bytesPerLine, (y + sliceHeight) * bytesPerLine))

//         add_to_output([0x1b, 0x4a, 0x15])
//     }
//     add_to_output([0x1b, 0x40])
//     return output
// }

// const compated_bits = (data: boolean[]): Uint8Array => {
//     const bits = new Uint8Array(Math.ceil(data.length / 8))
//     for (let i = 0; i < data.length; i += 8) {
//         bits[Math.floor(i / 8)] =
//             (data[i + 0] ? 128 : 0) |
//             (data[i + 1] ? 64 : 0) |
//             (data[i + 2] ? 32 : 0) |
//             (data[i + 3] ? 16 : 0) |
//             (data[i + 4] ? 8 : 0) |
//             (data[i + 5] ? 4 : 0) |
//             (data[i + 6] ? 2 : 0) |
//             (data[i + 7] ? 1 : 0)
//     }
//     return bits
// }
