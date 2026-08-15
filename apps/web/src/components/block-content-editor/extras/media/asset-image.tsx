"use client"

import { useResolvedAssetUrl } from "@game-guild/assets/react"

interface AssetImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  src?: string
}

/**
 * Image component that automatically resolves asset:// URLs
 * For non-asset URLs, renders normally
 */
export function AssetImage({ src, ...props }: AssetImageProps) {
  const { url: resolvedSrc, loading: isLoading } = useResolvedAssetUrl(src)

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
