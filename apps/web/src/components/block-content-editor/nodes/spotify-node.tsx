import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface SpotifyData {
  spotifyId: string
  type: "track" | "album" | "playlist" | "artist"
  title?: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  showTheme?: boolean // Show Spotify theme color
  isNew?: boolean // Flag to indicate if the Spotify embed was newly inserted
}

export type SerializedSpotifyNode = SerializedBlockNode<"spotify", SpotifyData>

/**
 * Extract Spotify ID and content type from a Spotify URL or URI.
 * Supports track, album, playlist, and artist resources.
 */
export function extractSpotifyInfo(
  url: string,
): { spotifyId: string; type: "track" | "album" | "playlist" | "artist" } | null {
  const trackRegex = /(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i
  const trackMatch = url.match(trackRegex)
  if (trackMatch && trackMatch[1]) {
    return { spotifyId: trackMatch[1], type: "track" }
  }

  const albumRegex = /(?:spotify\.com\/album\/|spotify:album:)([a-zA-Z0-9]+)/i
  const albumMatch = url.match(albumRegex)
  if (albumMatch && albumMatch[1]) {
    return { spotifyId: albumMatch[1], type: "album" }
  }

  const playlistRegex = /(?:spotify\.com\/playlist\/|spotify:playlist:)([a-zA-Z0-9]+)/i
  const playlistMatch = url.match(playlistRegex)
  if (playlistMatch && playlistMatch[1]) {
    return { spotifyId: playlistMatch[1], type: "playlist" }
  }

  const artistRegex = /(?:spotify\.com\/artist\/|spotify:artist:)([a-zA-Z0-9]+)/i
  const artistMatch = url.match(artistRegex)
  if (artistMatch && artistMatch[1]) {
    return { spotifyId: artistMatch[1], type: "artist" }
  }

  return null
}
