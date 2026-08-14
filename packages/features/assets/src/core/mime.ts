import type { AssetKind } from "./asset-contracts";

const CODE_MIME_TYPES = new Set([
  "application/javascript",
  "application/typescript",
  "application/x-sh",
  "text/css",
  "text/html",
  "text/javascript",
  "text/typescript",
]);

export function classifyAssetKind(mimeType: string, name = ""): AssetKind {
  if (mimeType.startsWith("image/")) return "image";
  if (mimeType.startsWith("video/")) return "video";
  if (mimeType.startsWith("audio/")) return "audio";
  if (
    mimeType === "text/csv" ||
    mimeType === "application/json" ||
    /\.(csv|json|tsv)$/i.test(name)
  ) {
    return "dataset";
  }
  if (CODE_MIME_TYPES.has(mimeType) || /\.(ts|tsx|js|jsx|css|html|py|rs|go|java|c|cpp|h)$/i.test(name)) {
    return "code";
  }
  if (/zip|gzip|tar|compressed/.test(mimeType)) return "archive";
  if (mimeType.startsWith("text/") || /pdf|document|sheet|presentation/.test(mimeType)) {
    return "document";
  }
  return "other";
}

export function inferMimeType(name: string, declared = ""): string {
  if (declared) return declared;
  if (/\.csv$/i.test(name)) return "text/csv";
  if (/\.json$/i.test(name)) return "application/json";
  if (/\.svg$/i.test(name)) return "image/svg+xml";
  if (/\.png$/i.test(name)) return "image/png";
  if (/\.jpe?g$/i.test(name)) return "image/jpeg";
  if (/\.webp$/i.test(name)) return "image/webp";
  if (/\.pdf$/i.test(name)) return "application/pdf";
  return "application/octet-stream";
}
