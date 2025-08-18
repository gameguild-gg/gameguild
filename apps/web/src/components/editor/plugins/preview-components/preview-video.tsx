"use client"

import { useState } from "react"
import type { SerializedVideoNode } from "../../nodes/video-node"
import { AlertCircle } from 'lucide-react'

// Função para detectar e extrair IDs de vídeos de diferentes plataformas
function getVideoEmbedInfo(url: string): { type: string; id: string } | null {
  // YouTube
  const youtubeRegex = /(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i
  const youtubeMatch = url.match(youtubeRegex)
  if (youtubeMatch && youtubeMatch[1]) {
    return { type: "youtube", id: youtubeMatch[1] }
  }

  // Vimeo
  const vimeoRegex = /(?:vimeo\.com\/(?:video\/)?|player\.vimeo\.com\/video\/)([0-9]+)/i
  const vimeoMatch = url.match(vimeoRegex)
  if (vimeoMatch && vimeoMatch[1]) {
    return { type: "vimeo", id: vimeoMatch[1] }
  }

  // Dailymotion
  const dailymotionRegex = /(?:dailymotion\.com\/(?:video\/|embed\/video\/)|dai\.ly\/)([a-zA-Z0-9]+)/i
  const dailymotionMatch = url.match(dailymotionRegex)
  if (dailymotionMatch && dailymotionMatch[1]) {
    return { type: "dailymotion", id: dailymotionMatch[1] }
  }

  // URL não reconhecida como plataforma de vídeo
  return null
}

// Componente para renderizar vídeos incorporados no preview
function EmbeddedVideoPreview({
  embedInfo,
  size,
}: {
  embedInfo: { type: string; id: string }
  size: number
}) {
  let embedUrl = ""
  let title = ""

  switch (embedInfo.type) {
    case "youtube":
      embedUrl = `https://www.youtube.com/embed/${embedInfo.id}`
      title = "YouTube video player"
      break
    case "vimeo":
      embedUrl = `https://player.vimeo.com/video/${embedInfo.id}`
      title = "Vimeo video player"
      break
    case "dailymotion":
      embedUrl = `https://www.dailymotion.com/embed/video/${embedInfo.id}`
      title = "Dailymotion video player"
      break
    default:
      return null
  }

  return (
    <div style={{ width: `${size}%` }} className="mx-auto">
      <div className="relative pt-[56.25%]">
        {" "}
        {/* 16:9 Aspect Ratio */}
        <iframe
          src={embedUrl}
          title={title}
          className="absolute top-0 left-0 w-full h-full rounded-lg"
          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
          allowFullScreen
        ></iframe>
      </div>
    </div>
  )
}

export function PreviewVideo({ node }: { node: SerializedVideoNode }) {
  const [hasError, setHasError] = useState(false)
  const embedInfo = getVideoEmbedInfo(node.data?.src)

  if (!node?.data) {
    console.error("Invalid video node structure:", node)
    return null
  }

  const { src, alt, caption, size = 100 } = node.data

  const renderErrorMessage = () => (
    <div
      className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[200px]"
      style={{ width: `${size}%` }}
    >
      <AlertCircle className="h-6 w-6 mb-2" />
      <p className="text-center">Could not load the video</p>
    </div>
  )

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        {hasError ? (
          renderErrorMessage()
        ) : embedInfo ? (
          <EmbeddedVideoPreview embedInfo={embedInfo} size={size} />
        ) : (
          <video
            src={src}
            controls
            style={{ width: `${size}%` }}
            className="h-auto rounded-lg"
            poster={alt ? `/placeholder.svg?text=${encodeURIComponent(alt)}` : undefined}
            onError={() => setHasError(true)}
          />
        )}
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}
