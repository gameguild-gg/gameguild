/**
 * Brotli decompression for lazy-loaded .br files.
 * When the CDN serves .br with Content-Encoding: br, the browser decompresses
 * automatically. This helper is for when we fetch .br without that header
 * (e.g. from a static file server or custom CDN).
 *
 * The bundled brotli WASM fallback is loaded at runtime by worker-entry.ts
 * and injected into LazyFS.customBrotliDecompressor. This module only
 * provides the native DecompressionStream path for library consumers.
 */

/**
 * Decompress Brotli-encoded data. Uses the native DecompressionStream when
 * available (Chrome 80+, modern browsers). Otherwise falls back to the
 * injected WASM decompressor (set by worker-entry.ts via LazyFS.customBrotliDecompressor).
 */
export async function decompressBrotli(data: Uint8Array): Promise<Uint8Array> {
  try {
    // Try native DecompressionStream('br') first
    const ds = new DecompressionStream('br' as unknown as CompressionFormat);
    const writer = ds.writable.getWriter();
    writer.write(new Uint8Array(data));
    writer.close();
    const reader = ds.readable.getReader();
    const chunks: Uint8Array[] = [];
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      chunks.push(value);
    }
    const totalLength = chunks.reduce((sum, c) => sum + c.length, 0);
    const result = new Uint8Array(totalLength);
    let off = 0;
    for (const chunk of chunks) {
      result.set(chunk, off);
      off += chunk.length;
    }
    return result;
  } catch (err) {
    throw new Error(
      `Brotli decompression failed: native DecompressionStream('br') is not available in this browser. ` +
      `Data size: ${data.length} bytes. Original error: ${err instanceof Error ? err.message : String(err)}`
    );
  }
}

/** Check if Brotli decompression is supported (DecompressionStream with 'br'). */
export function isBrotliSupported(): boolean {
  try {
    new DecompressionStream('br' as unknown as CompressionFormat);
    return true;
  } catch {
    return false;
  }
}
