// Seed hashing.
//
// Canonical-encode a WorkspaceSeed so 'once' policy can detect tampering and
// 'merge' can avoid clobbering edits. We use a stable JSON encoding (sorted
// keys, raw bytes hex-encoded) plus FNV-1a 64-bit. FNV is not cryptographic
// but is plenty for "did this seed change" tamper-detection and works
// identically in browser and Node without WebCrypto async overhead.

import type { FileEntry, WorkspaceSeed } from '../types.js';

/**
 * Normalize a `WorkspaceSeed` value to the canonical FileEntry shape so a
 * shorthand string and an explicit `{ content }` produce the same hash.
 */
export function normalizeSeedEntry(value: FileEntry | string): FileEntry {
    if (typeof value === 'string') return { content: value };
    return value;
}

/**
 * Compute a deterministic FNV-1a 64-bit hash (lowercase hex) over a seed.
 *
 * Determinism rules:
 * - Paths are sorted lexicographically before iteration.
 * - String content is encoded as UTF-8.
 * - Uint8Array content is hashed by raw bytes.
 * - Per-entry metadata (visibility/readonly/executable) is folded in so a
 *   visibility flip is treated as a meaningful change.
 */
export function hashSeed(seed: WorkspaceSeed): string {
    let hi = 0xcbf29ce4 >>> 0;
    let lo = 0x84222325 >>> 0;
    const FNV_PRIME_HI = 0x00000100;
    const FNV_PRIME_LO = 0x000001b3;

    const update = (byte: number) => {
        lo ^= byte & 0xff;
        // 64-bit multiply (hi:lo) * (FNV_PRIME_HI:FNV_PRIME_LO).
        const ll = lo * FNV_PRIME_LO;
        const lh = lo * FNV_PRIME_HI;
        const hl = hi * FNV_PRIME_LO;
        const newLo = ll >>> 0;
        const carry = Math.floor(ll / 0x100000000);
        hi = (lh + hl + carry) >>> 0;
        lo = newLo;
    };

    const updateString = (s: string) => {
        const bytes = TEXT_ENCODER.encode(s);
        for (let i = 0; i < bytes.length; i++) update(bytes[i]);
    };

    for (const path of Object.keys(seed).sort()) {
        const entry = normalizeSeedEntry(seed[path]);
        updateString(path);
        update(0);
        if (typeof entry.content === 'string') {
            update(0x73); // 's'
            updateString(entry.content);
        } else {
            update(0x62); // 'b'
            for (let i = 0; i < entry.content.length; i++) update(entry.content[i]);
        }
        update(0);
        update(entry.visibility ? VIS_CODE[entry.visibility] : 0);
        update(entry.readonly ? 1 : 0);
        update(entry.executable ? 1 : 0);
        update(0);
    }

    return (
        hi.toString(16).padStart(8, '0') +
        lo.toString(16).padStart(8, '0')
    );
}

const TEXT_ENCODER = /* @__PURE__ */ new TextEncoder();

const VIS_CODE: Record<NonNullable<FileEntry['visibility']>, number> = {
    public: 1,
    hidden: 2,
    solution: 3,
};
