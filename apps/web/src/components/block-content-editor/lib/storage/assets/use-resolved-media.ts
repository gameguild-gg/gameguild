import { useEffect, useState } from "react"
import { resolveAssetUrl, isAssetUrl } from "@/components/block-content-editor/lib/storage/assets"
import type { BaseMediaData } from "@/components/block-content-editor/nodes/base/media-node-base"

/**
 * Hook to resolve asset URLs for a list of media items
 */
export function useResolvedMediaItems(items: BaseMediaData[]) {
  const [resolvedItems, setResolvedItems] = useState<BaseMediaData[]>(items)
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    async function resolveItems() {
      setIsLoading(true)
      try {
        const resolved = await Promise.all(
          items.map(async (item) => {
            if (item.src && isAssetUrl(item.src)) {
              const resolvedSrc = await resolveAssetUrl(item.src)
              return { ...item, src: resolvedSrc || item.src }
            }
            return item
          })
        )
        setResolvedItems(resolved)
      } catch (error) {
        console.error("Failed to resolve media asset URLs:", error)
        setResolvedItems(items)
      } finally {
        setIsLoading(false)
      }
    }
    resolveItems()
  }, [items])

  return { resolvedItems, isLoading }
}

/**
 * Hook to resolve a single asset URL
 */
export function useResolvedAssetUrl(url: string | undefined) {
  const [resolvedUrl, setResolvedUrl] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    async function loadAsset() {
      if (!url) {
        setResolvedUrl(null)
        return
      }

      if (isAssetUrl(url)) {
        setIsLoading(true)
        try {
          const resolved = await resolveAssetUrl(url)
          setResolvedUrl(resolved)
        } catch (error) {
          console.error("Failed to resolve asset URL:", error)
          setResolvedUrl(null)
        } finally {
          setIsLoading(false)
        }
      } else {
        setResolvedUrl(url)
      }
    }
    loadAsset()
  }, [url])

  return { resolvedUrl, isLoading }
}
