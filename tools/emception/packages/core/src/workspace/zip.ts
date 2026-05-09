// Minimal stored-only ZIP encoder/decoder.
//
// Implements just enough of the ZIP file format (PKZIP APPNOTE 4.5+) to
// round-trip workspace contents. We use compression method 0 (STORED) only:
// every entry is stored verbatim. This keeps the implementation dependency-
// free and tiny while still producing files openable by any standard ZIP
// tool. Workspaces are typically small text trees, so the lack of DEFLATE
// compression is acceptable; instructors can re-zip externally if size
// matters for a particular distribution.
//
// What we DO support:
//   - UTF-8 filenames (general-purpose bit 11 set).
//   - Empty entries (size 0).
//   - Reading any STORED entry (method 0); we ignore but don't reject other
//     methods at directory level — they just fail at extract time.
//   - End-of-central-directory record search across the trailing 64 KB so
//     comment fields don't break us.
//
// What we do NOT support:
//   - DEFLATE / ZIP64 / encryption / streaming append.
//   - Directory entries (entries with paths ending in '/'). Empty
//     directories aren't represented in workspaces.
//
// All public functions are pure and DOM-free.

import { EmceptionError } from '../errors.js';

/* eslint-disable no-bitwise */

// ---------- CRC32 (IEEE 802.3 polynomial 0xEDB88320) ----------

let CRC_TABLE: Uint32Array | null = null;
function crcTable(): Uint32Array {
    if (CRC_TABLE) return CRC_TABLE;
    const t = new Uint32Array(256);
    for (let i = 0; i < 256; i++) {
        let c = i;
        for (let k = 0; k < 8; k++) {
            c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
        }
        t[i] = c >>> 0;
    }
    CRC_TABLE = t;
    return t;
}

export function crc32(data: Uint8Array): number {
    const t = crcTable();
    let c = 0xffffffff;
    for (let i = 0; i < data.length; i++) {
        c = (t[(c ^ data[i]) & 0xff] ^ (c >>> 8)) >>> 0;
    }
    return (c ^ 0xffffffff) >>> 0;
}

// ---------- Public types ----------

export interface ZipEntry {
    /** UTF-8 path inside the archive; forward slashes only. */
    path: string;
    data: Uint8Array;
}

export interface CreateZipOptions {
    /** Optional fixed timestamp for reproducible builds (default: epoch). */
    date?: Date;
}

// ---------- Encoder ----------

const TEXT_ENC = new TextEncoder();
const TEXT_DEC = new TextDecoder('utf-8', { fatal: false });

function dosTimeDate(d: Date): { time: number; date: number } {
    const time =
        ((d.getHours() & 0x1f) << 11) |
        ((d.getMinutes() & 0x3f) << 5) |
        ((Math.floor(d.getSeconds() / 2)) & 0x1f);
    const year = d.getFullYear();
    const date =
        (((year < 1980 ? 0 : year - 1980) & 0x7f) << 9) |
        (((d.getMonth() + 1) & 0x0f) << 5) |
        (d.getDate() & 0x1f);
    return { time, date };
}

function writeUInt32LE(view: DataView, offset: number, value: number): void {
    view.setUint32(offset, value >>> 0, true);
}

function writeUInt16LE(view: DataView, offset: number, value: number): void {
    view.setUint16(offset, value & 0xffff, true);
}

/**
 * Build a stored-only ZIP archive from a flat list of entries.
 *
 * Order of entries in the produced central directory matches the input
 * order, so callers that care about deterministic output should sort
 * upstream. Throws {@link EmceptionError} on invalid input.
 */
export function createZip(entries: readonly ZipEntry[], opts: CreateZipOptions = {}): Uint8Array {
    if (!Array.isArray(entries)) {
        throw new EmceptionError('createZip: entries must be an array');
    }
    const date = opts.date ?? new Date(0);
    const { time, date: dosDate } = dosTimeDate(date);

    interface Prepared {
        nameBytes: Uint8Array;
        data: Uint8Array;
        crc: number;
        offset: number;
    }
    const prepared: Prepared[] = [];

    // Pass 1: encode names + compute total byte sizes for local headers.
    let totalLocal = 0;
    for (const e of entries) {
        if (typeof e.path !== 'string' || e.path === '') {
            throw new EmceptionError('createZip: entry.path must be a non-empty string');
        }
        if (e.path.endsWith('/')) {
            throw new EmceptionError(
                `createZip: directory entries are not supported (got '${e.path}')`,
            );
        }
        if (!(e.data instanceof Uint8Array)) {
            throw new EmceptionError(`createZip: entry.data must be Uint8Array (path='${e.path}')`);
        }
        const nameBytes = TEXT_ENC.encode(e.path);
        if (nameBytes.length > 0xffff) {
            throw new EmceptionError(`createZip: filename too long (path='${e.path}')`);
        }
        if (e.data.length > 0xfffffffe) {
            throw new EmceptionError(`createZip: entry too large for ZIP32 (path='${e.path}')`);
        }
        prepared.push({ nameBytes, data: e.data, crc: crc32(e.data), offset: 0 });
        totalLocal += 30 + nameBytes.length + e.data.length;
    }

    // Pass 2: compute central directory size.
    let centralSize = 0;
    for (const p of prepared) centralSize += 46 + p.nameBytes.length;
    const eocdSize = 22;
    const out = new Uint8Array(totalLocal + centralSize + eocdSize);
    const view = new DataView(out.buffer);

    // Pass 3: write local file headers + entry data.
    let cursor = 0;
    for (const p of prepared) {
        p.offset = cursor;
        writeUInt32LE(view, cursor, 0x04034b50);    // local file header signature
        writeUInt16LE(view, cursor + 4, 20);        // version needed (2.0)
        writeUInt16LE(view, cursor + 6, 0x0800);    // GP bit 11: UTF-8 filenames
        writeUInt16LE(view, cursor + 8, 0);         // method = stored
        writeUInt16LE(view, cursor + 10, time);
        writeUInt16LE(view, cursor + 12, dosDate);
        writeUInt32LE(view, cursor + 14, p.crc);
        writeUInt32LE(view, cursor + 18, p.data.length); // compressed size
        writeUInt32LE(view, cursor + 22, p.data.length); // uncompressed size
        writeUInt16LE(view, cursor + 26, p.nameBytes.length);
        writeUInt16LE(view, cursor + 28, 0);        // extra field length
        out.set(p.nameBytes, cursor + 30);
        out.set(p.data, cursor + 30 + p.nameBytes.length);
        cursor += 30 + p.nameBytes.length + p.data.length;
    }

    // Pass 4: write central directory.
    const centralStart = cursor;
    for (const p of prepared) {
        writeUInt32LE(view, cursor, 0x02014b50);    // central dir header signature
        writeUInt16LE(view, cursor + 4, 0x0314);    // version made by (UNIX, 2.0)
        writeUInt16LE(view, cursor + 6, 20);        // version needed
        writeUInt16LE(view, cursor + 8, 0x0800);    // GP bit 11
        writeUInt16LE(view, cursor + 10, 0);        // method
        writeUInt16LE(view, cursor + 12, time);
        writeUInt16LE(view, cursor + 14, dosDate);
        writeUInt32LE(view, cursor + 16, p.crc);
        writeUInt32LE(view, cursor + 20, p.data.length);
        writeUInt32LE(view, cursor + 24, p.data.length);
        writeUInt16LE(view, cursor + 28, p.nameBytes.length);
        writeUInt16LE(view, cursor + 30, 0);        // extra
        writeUInt16LE(view, cursor + 32, 0);        // comment length
        writeUInt16LE(view, cursor + 34, 0);        // disk number
        writeUInt16LE(view, cursor + 36, 0);        // internal attrs
        writeUInt32LE(view, cursor + 38, 0);        // external attrs
        writeUInt32LE(view, cursor + 42, p.offset);
        out.set(p.nameBytes, cursor + 46);
        cursor += 46 + p.nameBytes.length;
    }

    // Pass 5: end-of-central-directory.
    writeUInt32LE(view, cursor, 0x06054b50);
    writeUInt16LE(view, cursor + 4, 0);             // disk number
    writeUInt16LE(view, cursor + 6, 0);             // disk where CD starts
    writeUInt16LE(view, cursor + 8, prepared.length);
    writeUInt16LE(view, cursor + 10, prepared.length);
    writeUInt32LE(view, cursor + 12, centralSize);
    writeUInt32LE(view, cursor + 16, centralStart);
    writeUInt16LE(view, cursor + 20, 0);            // comment length
    cursor += 22;

    return out.subarray(0, cursor);
}

// ---------- Decoder ----------

function findEocd(buf: Uint8Array): number {
    // Minimum size and search window per APPNOTE: comment max 0xFFFF.
    const max = Math.min(buf.length, 0xffff + 22);
    const minStart = buf.length - max;
    for (let i = buf.length - 22; i >= minStart; i--) {
        if (
            buf[i] === 0x50 &&
            buf[i + 1] === 0x4b &&
            buf[i + 2] === 0x05 &&
            buf[i + 3] === 0x06
        ) {
            return i;
        }
    }
    return -1;
}

/**
 * Parse a ZIP archive and return its STORED entries.
 *
 * Throws {@link EmceptionError} if the archive is malformed or contains
 * any non-stored entry (we don't ship a DEFLATE decoder).
 */
export function readZip(buf: Uint8Array): ZipEntry[] {
    if (!(buf instanceof Uint8Array)) {
        throw new EmceptionError('readZip: input must be Uint8Array');
    }
    if (buf.length < 22) {
        throw new EmceptionError('readZip: buffer too small to be a ZIP archive');
    }
    const eocd = findEocd(buf);
    if (eocd < 0) throw new EmceptionError('readZip: end-of-central-directory record not found');
    const view = new DataView(buf.buffer, buf.byteOffset, buf.byteLength);
    const entryCount = view.getUint16(eocd + 10, true);
    const cdSize = view.getUint32(eocd + 12, true);
    const cdStart = view.getUint32(eocd + 16, true);
    if (cdStart + cdSize > buf.length) {
        throw new EmceptionError('readZip: central directory extends past end of buffer');
    }

    const entries: ZipEntry[] = [];
    let cursor = cdStart;
    for (let i = 0; i < entryCount; i++) {
        if (view.getUint32(cursor, true) !== 0x02014b50) {
            throw new EmceptionError(`readZip: bad central directory header at offset ${cursor}`);
        }
        const method = view.getUint16(cursor + 10, true);
        const compSize = view.getUint32(cursor + 20, true);
        const uncompSize = view.getUint32(cursor + 24, true);
        const nameLen = view.getUint16(cursor + 28, true);
        const extraLen = view.getUint16(cursor + 30, true);
        const commentLen = view.getUint16(cursor + 32, true);
        const localOffset = view.getUint32(cursor + 42, true);
        const nameBytes = buf.subarray(cursor + 46, cursor + 46 + nameLen);
        const path = TEXT_DEC.decode(nameBytes);
        cursor += 46 + nameLen + extraLen + commentLen;

        if (path.endsWith('/')) continue; // skip directory entries
        if (method !== 0) {
            throw new EmceptionError(
                `readZip: entry '${path}' uses unsupported compression method ${method} (only STORED is supported)`,
            );
        }

        // Read the local header to find the data offset (extra field length
        // can differ from the central directory entry).
        if (view.getUint32(localOffset, true) !== 0x04034b50) {
            throw new EmceptionError(`readZip: bad local file header for '${path}'`);
        }
        const lhNameLen = view.getUint16(localOffset + 26, true);
        const lhExtraLen = view.getUint16(localOffset + 28, true);
        const dataStart = localOffset + 30 + lhNameLen + lhExtraLen;
        const data = buf.subarray(dataStart, dataStart + compSize);
        if (data.length !== uncompSize) {
            throw new EmceptionError(
                `readZip: declared size ${uncompSize} != stored size ${data.length} for '${path}'`,
            );
        }
        entries.push({ path, data: new Uint8Array(data) });
    }
    return entries;
}
