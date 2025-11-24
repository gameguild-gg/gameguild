/**
 * Helper functions to detect URL type for video and audio embeds
 */

export type VideoEmbedType = "youtube" | "vimeo" | "dailymotion" | "direct"
export type AudioEmbedType = "youtube" | "spotify" | "soundcloud" | "direct"

/**
 * Detects the video embed type from a URL
 */
export function detectVideoEmbedType(url: string): VideoEmbedType {
  if (!url) return "direct"
  
  // YouTube
  if (/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i.test(url)) {
    return "youtube"
  }
  
  // Vimeo
  if (/(?:vimeo\.com\/(?:video\/)?|player\.vimeo\.com\/video\/)([0-9]+)/i.test(url)) {
    return "vimeo"
  }
  
  // Dailymotion
  if (/(?:dailymotion\.com\/(?:video\/|embed\/video\/)|dai\.ly\/)([a-zA-Z0-9]+)/i.test(url)) {
    return "dailymotion"
  }
  
  return "direct"
}

/**
 * Detects the audio embed type from a URL
 */
export function detectAudioEmbedType(url: string): AudioEmbedType {
  if (!url) return "direct"
  
  // YouTube
  if (/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i.test(url)) {
    return "youtube"
  }
  
  // Spotify
  if (/(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i.test(url)) {
    return "spotify"
  }
  
  // SoundCloud
  if (/soundcloud\.com\/([^/]+\/[^/]+)/i.test(url)) {
    return "soundcloud"
  }
  
  return "direct"
}

/**
 * Detects the video file format from URL extension
 */
export function detectVideoFileType(url: string): string {
  if (!url) return "video/mp4"
  
  const extension = url.split('.').pop()?.toLowerCase()
  switch (extension) {
    case "webm":
      return "video/webm"
    case "ogg":
    case "ogv":
      return "video/ogg"
    case "mp4":
    default:
      return "video/mp4"
  }
}

/**
 * Detects the audio file format from URL extension
 */
export function detectAudioFileType(url: string): string {
  if (!url) return "audio/mpeg"
  
  const extension = url.split('.').pop()?.toLowerCase()
  switch (extension) {
    case "wav":
      return "audio/wav"
    case "ogg":
    case "oga":
      return "audio/ogg"
    case "mp3":
    default:
      return "audio/mpeg"
  }
}
