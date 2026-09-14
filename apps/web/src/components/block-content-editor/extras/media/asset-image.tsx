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
      <span
        aria-label={props.alt ? `Loading ${props.alt}` : "Loading image"}
        className={props.className}
        role="status"
        style={props.style}
      >
        <span className="flex h-full w-full items-center justify-center bg-gray-100 dark:bg-gray-800">
          <span className="text-xs text-gray-400">Loading...</span>
        </span>
      </span>
    )
  }

  if (!resolvedSrc) return null

  return <img src={resolvedSrc} {...props} />
}
