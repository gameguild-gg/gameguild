import { useEffect, useState } from 'react'
import { assetManager } from '@/components/block-content-editor/lib/storage/assets/asset-manager'

export function useAssetPreview(assetId: string, mimeType: string) {
  const [assetDataUrl, setAssetDataUrl] = useState<string | null>(null)

  useEffect(() => {
    const loadAssetData = async () => {
      if (mimeType.startsWith('image/')) {
        try {
          const assetData = await assetManager.getAsset(assetId)
          if (assetData && assetData.data) {
            setAssetDataUrl(assetData.data)
          }
        } catch (error) {
          console.error(`Failed to load asset ${assetId}:`, error)
        }
      }
    }

    loadAssetData()
  }, [assetId, mimeType])

  return assetDataUrl
}
