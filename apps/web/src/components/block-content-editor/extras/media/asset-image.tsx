"use client"

import { useState, useEffect } from "react"
import { isAssetUrl, resolveAssetUrl } from "@/components/block-content-editor/lib/storage/assets"

interface AssetImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  src?: string
}

/**
 * Image component that automatically resolves asset:// URLs
 * For non-asset URLs, renders normally
 */
export function AssetImage({ src, ...props }: AssetImageProps) {
  const [resolvedSrc, setResolvedSrc] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!src) {
      setResolvedSrc(null)
      return
    }

    if (isAssetUrl(src)) {
      setIsLoading(true)
      resolveAssetUrl(src)
        .then(resolved => {
          setResolvedSrc(resolved)
          setIsLoading(false)
        })
        .catch(error => {
          console.error("Failed to resolve asset URL:", error)
          setResolvedSrc(null)
          setIsLoading(false)
        })
    } else {
      setResolvedSrc(src)
    }
  }, [src])

  if (!src) return null
  
  if (isLoading) {
    return (
      <div className={props.className} style={props.style}>
        <div className="w-full h-full flex items-center justify-center bg-gray-100 dark:bg-gray-800">
          <div className="text-xs text-gray-400">Loading...</div>
        </div>
      </div>
    )
  }

  if (!resolvedSrc) return null

  return <img src={resolvedSrc} {...props} />
}
