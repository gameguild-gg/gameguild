import { useResolvedAssetUrl } from '@game-guild/assets/react'

export function useAssetPreview(assetId: string, mimeType: string) {
  const { url } = useResolvedAssetUrl(mimeType.startsWith('image/') ? assetId : null)
  return url || null
}
