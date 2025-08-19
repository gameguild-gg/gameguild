"use client"

import type { SerializedSpotifyNode } from "../../nodes/spotify-node"

export function PreviewSpotify({ node }: { node: SerializedSpotifyNode }) {
  if (!node?.data) {
    console.error("Invalid Spotify node structure:", node)
    return null
  }

  const { spotifyId, type, title, caption, size = 100, showTheme } = node.data

  // Get the appropriate Spotify embed URL based on type
  const getSpotifyEmbedUrl = () => {
    const themeParam = showTheme ? "theme=1" : "theme=0"

    switch (type) {
      case "track":
        return `https://open.spotify.com/embed/track/${spotifyId}?${themeParam}`
      case "album":
      case "playlist":
        return `https://open.spotify.com/embed/album/${spotifyId}?${themeParam}`
      case "artist":
        return `https://open.spotify.com/embed/artist/${spotifyId}?${themeParam}`
      default:
        return `https://open.spotify.com/embed/track/${spotifyId}?${themeParam}`
    }
  }

  // Get the appropriate height based on type
  const getEmbedHeight = () => {
    switch (type) {
      case "track":
        return "152" // Height for track embeds
      case "album":
      case "playlist":
        return "380" // Height for album/playlist embeds
      case "artist":
        return "380" // Height for artist embeds
      default:
        return "152"
    }
  }

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        <div style={{ width: `${size}%` }} className="relative">
          <iframe
            src={getSpotifyEmbedUrl()}
            width="100%"
            height={getEmbedHeight()}
            frameBorder="0"
            allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
            loading="lazy"
            title={title || `Spotify ${type}`}
            className="rounded-lg"
          ></iframe>
        </div>
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}
