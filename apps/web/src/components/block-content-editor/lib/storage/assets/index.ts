/**
 * Asset storage module
 * Provides centralized asset management for media files
 */

export * from "./types"
export * from "./asset-manager"
export * from "./use-resolved-media"

import { assetManager } from "./asset-manager"

/**
 * Resolve an asset URL to a data URL
 * If the URL is an asset:// URL, fetch it from the asset store
 * Otherwise, return the URL as-is
 */
export async function resolveAssetUrl(url: string): Promise<string | null> {
  if (url.startsWith("asset://")) {
    const assetId = url.replace("asset://", "")
    return await assetManager.getAssetUrl(assetId)
  }
  return url
}

/**
 * Check if a URL is an asset URL
 */
export function isAssetUrl(url: string): boolean {
  return url.startsWith("asset://")
}

/**
 * Extract asset ID from an asset URL
 */
export function getAssetIdFromUrl(url: string): string | null {
  if (isAssetUrl(url)) {
    return url.replace("asset://", "")
  }
  return null
}
