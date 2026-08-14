import { AssetError } from "../core/asset-errors";
import { throwIfAborted } from "./object-store";

export async function hashBlob(blob: Blob, signal?: AbortSignal): Promise<string> {
  throwIfAborted(signal);
  if (!globalThis.crypto?.subtle) {
    throw new AssetError("unsupported", "Web Crypto is unavailable");
  }
  const bytes = await blob.arrayBuffer();
  throwIfAborted(signal);
  const digest =
    blob.size >= 4 * 1024 * 1024 && typeof Worker === "function"
      ? await digestInWorker(bytes, signal)
      : await crypto.subtle.digest("SHA-256", bytes);
  const hex = Array.from(new Uint8Array(digest), (byte) =>
    byte.toString(16).padStart(2, "0"),
  ).join("");
  return `sha256:${hex}`;
}

async function digestInWorker(
  bytes: ArrayBuffer,
  signal?: AbortSignal,
): Promise<ArrayBuffer> {
  const source = `self.onmessage=async(e)=>{try{const d=await crypto.subtle.digest("SHA-256",e.data);self.postMessage({d},[d])}catch(error){self.postMessage({error:String(error)})}}`;
  const workerUrl = URL.createObjectURL(new Blob([source], { type: "text/javascript" }));
  const worker = new Worker(workerUrl);
  try {
    return await new Promise<ArrayBuffer>((resolve, reject) => {
      const abort = () => {
        worker.terminate();
        reject(new DOMException("Asset operation was aborted", "AbortError"));
      };
      signal?.addEventListener("abort", abort, { once: true });
      worker.onmessage = (event: MessageEvent<{ d?: ArrayBuffer; error?: string }>) => {
        signal?.removeEventListener("abort", abort);
        if (event.data.error || !event.data.d) reject(new Error(event.data.error ?? "Hash worker failed"));
        else resolve(event.data.d);
      };
      worker.onerror = (event) => {
        signal?.removeEventListener("abort", abort);
        reject(new Error(event.message || "Hash worker failed"));
      };
      worker.postMessage(bytes, [bytes]);
    });
  } finally {
    worker.terminate();
    URL.revokeObjectURL(workerUrl);
  }
}
