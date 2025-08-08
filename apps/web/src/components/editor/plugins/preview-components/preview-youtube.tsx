"use client"

import type { SerializedYouTubeNode } from "../../nodes/youtube-node"

export function PreviewYouTube({ node }: { node: SerializedYouTubeNode }) {
  if (!node?.data) {
    console.error("Invalid YouTube node structure:", node)
    return null
  }

  const { videoId, title, caption, size = 100, startAt, showControls, showInfo, showRelated } = node.data

  // Build YouTube embed URL with parameters
  const getYouTubeEmbedUrl = () => {
    let url = `https://www.youtube.com/embed/${videoId}?enablejsapi=1`

    // Add start time if specified
    if (startAt && startAt > 0) {
      url += `&start=${startAt}`
    }

    // Add controls parameter
    url += `&controls=${showControls ? "1" : "0"}`

    // Add showinfo parameter
    url += `&showinfo=${showInfo ? "1" : "0"}`

    // Add related videos parameter
    url += `&rel=${showRelated ? "1" : "0"}`

    // Add modestbranding parameter (always enabled to reduce YouTube branding)
    url += "&modestbranding=1"

    return url
  }

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        <div style={{ width: `${size}%` }} className="relative">
          <div className="relative pt-[56.25%]">
            {/* 16:9 Aspect Ratio */}
            <iframe
              src={getYouTubeEmbedUrl()}
              title={title || "YouTube video player"}
              className="absolute top-0 left-0 w-full h-full rounded-lg"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
              allowFullScreen
            ></iframe>
          </div>
        </div>
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}
