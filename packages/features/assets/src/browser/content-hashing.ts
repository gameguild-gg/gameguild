import { AssetError } from "../core/asset-errors";

function throwIfAborted(signal?: AbortSignal): void {
  if (signal?.aborted) {
    throw new DOMException("Asset operation was aborted", "AbortError");
  }
}

export async function hashBlob(blob: Blob, signal?: AbortSignal): Promise<string> {
  throwIfAborted(signal);
  if (!globalThis.crypto?.subtle) {
    throw new AssetError("unsupported", "Web Crypto is unavailable");
  }
  const bytes = await blob.arrayBuffer();
  throwIfAborted(signal);
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  const hex = Array.from(new Uint8Array(digest), (byte) =>
    byte.toString(16).padStart(2, "0"),
  ).join("");
  return `sha256:${hex}`;
}
