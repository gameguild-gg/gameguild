export type AssetErrorCode =
  | "aborted"
  | "corrupt"
  | "invalid"
  | "missing"
  | "quota-exceeded"
  | "storage-unavailable"
  | "unsupported";

export class AssetError extends Error {
  constructor(
    public readonly code: AssetErrorCode,
    message: string,
    options?: ErrorOptions,
  ) {
    super(message, options);
    this.name = "AssetError";
  }
}

export function toAssetError(error: unknown, fallback: AssetErrorCode): AssetError {
  if (error instanceof AssetError) return error;
  if (error instanceof DOMException && error.name === "AbortError") {
    return new AssetError("aborted", "Asset operation was aborted", {
      cause: error,
    });
  }
  if (
    error instanceof DOMException &&
    (error.name === "QuotaExceededError" || error.name === "NS_ERROR_DOM_QUOTA_REACHED")
  ) {
    return new AssetError("quota-exceeded", "Browser storage quota was exceeded", {
      cause: error,
    });
  }
  return new AssetError(
    fallback,
    error instanceof Error ? error.message : "Unknown asset error",
    { cause: error },
  );
}
