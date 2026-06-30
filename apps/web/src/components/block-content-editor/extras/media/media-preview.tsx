"use client"

import { useState, useRef, useEffect } from "react"
import { Play, Pause, Volume2, VolumeX, Maximize, AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Slider } from "@/components/ui/slider"
import type { BaseMediaData } from "@/components/block-content-editor/nodes/base/media-node-base"
import { resolveAssetUrl, isAssetUrl } from "@/components/block-content-editor/lib/storage/assets"
import { AssetImage } from "./asset-image"

interface MediaPreviewProps {
  data: BaseMediaData
}

export function MediaPreview({ data }: MediaPreviewProps) {
  const [isLoading, setIsLoading] = useState(true)
  const [hasError, setHasError] = useState(false)
  const [errorMessage, setErrorMessage] = useState("")
  const [resolvedSrc, setResolvedSrc] = useState<string | null>(null)
  const [isLoadingAsset, setIsLoadingAsset] = useState(false)

  // Resolve asset URL
  useEffect(() => {
    async function loadAsset() {
      if (!data.src) {
        setResolvedSrc(null)
        return
      }

      if (isAssetUrl(data.src)) {
        setIsLoadingAsset(true)
        try {
          const url = await resolveAssetUrl(data.src)
          setResolvedSrc(url)
        } catch (error) {
          console.error("Failed to resolve asset URL:", error)
          setResolvedSrc(null)
        } finally {
          setIsLoadingAsset(false)
        }
      } else {
        setResolvedSrc(data.src)
      }
    }
    loadAsset()
  }, [data.src])

  // Video/Audio player state
  const [isPlaying, setIsPlaying] = useState(false)
  const [currentTime, setCurrentTime] = useState(0)
  const [duration, setDuration] = useState(0)
  const [volume, setVolume] = useState(0.7)
  const [muted, setMuted] = useState(false)
  const [showControls, setShowControls] = useState(false)

  const mediaRef = useRef<HTMLVideoElement | HTMLAudioElement>(null)

  const formatTime = (time: number) => {
    const minutes = Math.floor(time / 60)
    const seconds = Math.floor(time % 60)
    return `${minutes}:${seconds < 10 ? "0" : ""}${seconds}`
  }

  const togglePlay = () => {
    if (!mediaRef.current) return

    try {
      if (isPlaying) {
        mediaRef.current.pause()
        setIsPlaying(false)
      } else {
        const playPromise = mediaRef.current.play()
        if (playPromise !== undefined) {
          playPromise
            .then(() => {
              setIsPlaying(true)
            })
            .catch((error) => {
              console.error("Error playing media:", error)
              setIsPlaying(false)
            })
        }
      }
    } catch (error) {
      console.error("Error controlling media:", error)
      setIsPlaying(false)
    }
  }

  const handleTimeUpdate = () => {
    if (!mediaRef.current) return
    setCurrentTime(mediaRef.current.currentTime)
    if (mediaRef.current.duration && !duration) {
      setDuration(mediaRef.current.duration)
    }
  }

  const handleSliderChange = (values: number[]) => {
    if (!mediaRef.current || !values[0]) return
    const newTime = values[0]
    mediaRef.current.currentTime = newTime
    setCurrentTime(newTime)
  }

  const handleVolumeChange = (values: number[]) => {
    if (!mediaRef.current || values[0] === undefined) return
    const newVolume = values[0]
    mediaRef.current.volume = newVolume
    setVolume(newVolume)
    setMuted(newVolume === 0)
  }

  const toggleMute = () => {
    if (!mediaRef.current) return
    mediaRef.current.muted = !mediaRef.current.muted
    setMuted(!muted)
  }

  const handleFullscreen = () => {
    if (!mediaRef.current) return
    if (document.fullscreenElement) {
      document.exitFullscreen()
    } else {
      mediaRef.current.requestFullscreen()
    }
  }

  const renderImage = () => (
    <div className="flex items-center justify-center h-full w-full p-8">
      <div style={{ width: `${data.size}%` }} className="relative">
        {(isLoading || isLoadingAsset) && (
          <div className="absolute inset-0 flex items-center justify-center bg-muted/50 rounded-lg">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
        )}
        {hasError ? (
          <div className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[200px]">
            <AlertCircle className="h-6 w-6 mb-2" />
            <p className="text-center">{errorMessage || "Erro ao carregar imagem"}</p>
          </div>
        ) : (
          <AssetImage
            src={data.src || "/placeholder.svg"}
            alt={data.alt || ""}
            className="w-full h-auto rounded-lg"
            onLoad={() => setIsLoading(false)}
            onError={() => {
              setIsLoading(false)
              setHasError(true)
              setErrorMessage("Não foi possível carregar a imagem")
            }}
          />
        )}
        {data.caption && (
          <div className="mt-2 text-sm text-muted-foreground text-center">{data.caption}</div>
        )}
      </div>
    </div>
  )

  const renderVideo = () => {
    // Check if it's an embedded video
    const embedType = data.embedType
    
    if (embedType && embedType !== "direct") {
      return renderEmbeddedVideo()
    }

    return (
      <div className="flex items-center justify-center h-full w-full p-8">
        <div
          style={{ width: `${data.size}%` }}
          className="relative"
          onMouseEnter={() => setShowControls(true)}
          onMouseLeave={() => setShowControls(false)}
        >
          {hasError ? (
            <div className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[200px]">
              <AlertCircle className="h-6 w-6 mb-2" />
              <p className="text-center">{errorMessage || "Erro ao carregar vídeo"}</p>
            </div>
          ) : (
            <>
              <video
                ref={mediaRef as React.RefObject<HTMLVideoElement>}
                src={resolvedSrc || data.src}
                className="w-full h-auto rounded-lg"
                onLoadedData={() => setIsLoading(false)}
                onTimeUpdate={handleTimeUpdate}
                onEnded={() => setIsPlaying(false)}
                onError={() => {
                  setIsLoading(false)
                  setHasError(true)
                  setErrorMessage("Não foi possível carregar o vídeo")
                }}
              >
                <source src={resolvedSrc || data.src} type={data.videoType || "video/mp4"} />
              </video>

              {/* Custom controls */}
              {showControls && (
                <div className="absolute bottom-0 left-0 right-0 bg-black/70 text-white p-2 rounded-b-lg">
                  <div className="space-y-2">
                    <Slider
                      value={[currentTime]}
                      max={duration || 100}
                      step={0.1}
                      onValueChange={handleSliderChange}
                      className="cursor-pointer"
                    />
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-white hover:bg-white/20"
                          onClick={togglePlay}
                        >
                          {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                        </Button>
                        <span className="text-xs">
                          {formatTime(currentTime)} / {formatTime(duration)}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-white hover:bg-white/20"
                          onClick={toggleMute}
                        >
                          {muted ? <VolumeX className="h-4 w-4" /> : <Volume2 className="h-4 w-4" />}
                        </Button>
                        <Slider
                          value={[muted ? 0 : volume]}
                          max={1}
                          step={0.1}
                          onValueChange={handleVolumeChange}
                          className="w-20"
                        />
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-white hover:bg-white/20"
                          onClick={handleFullscreen}
                        >
                          <Maximize className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
          {data.caption && (
            <div className="mt-2 text-sm text-muted-foreground text-center">{data.caption}</div>
          )}
        </div>
      </div>
    )
  }

  const renderEmbeddedVideo = () => {
    let embedUrl = ""
    let videoId = ""

    // Extract video ID from URL
    if (data.embedType === "youtube") {
      const match = data.src.match(/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i)
      if (match && match[1]) {
        videoId = match[1]
        embedUrl = `https://www.youtube-nocookie.com/embed/${videoId}?enablejsapi=1`
      }
    } else if (data.embedType === "vimeo") {
      const match = data.src.match(/(?:vimeo\.com\/(?:video\/)?|player\.vimeo\.com\/video\/)([0-9]+)/i)
      if (match && match[1]) {
        videoId = match[1]
        embedUrl = `https://player.vimeo.com/video/${videoId}`
      }
    } else if (data.embedType === "dailymotion") {
      const match = data.src.match(/(?:dailymotion\.com\/(?:video\/|embed\/video\/)|dai\.ly\/)([a-zA-Z0-9]+)/i)
      if (match && match[1]) {
        videoId = match[1]
        embedUrl = `https://www.dailymotion.com/embed/video/${videoId}`
      }
    }

    if (!embedUrl) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4">
            <AlertCircle className="h-6 w-6 mb-2 mx-auto" />
            <p className="text-center">URL de vídeo inválida</p>
          </div>
        </div>
      )
    }

    return (
      <div className="flex items-center justify-center h-full w-full p-8">
        <div style={{ width: `${data.size}%` }} className="relative">
          <div className="relative pt-[56.25%]">
            <iframe
              src={embedUrl}
              className="absolute inset-0 w-full h-full rounded-lg"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
              onLoad={() => setIsLoading(false)}
              // @ts-expect-error credentialless is not yet in React's iframe types
              credentialless="true"
            />
          </div>
          {data.caption && (
            <div className="mt-2 text-sm text-muted-foreground text-center">{data.caption}</div>
          )}
        </div>
      </div>
    )
  }

  const renderAudio = () => {
    // Check if it's an embedded audio
    const embedType = data.embedAudioType
    
    if (embedType && embedType !== "direct") {
      return renderEmbeddedAudio()
    }

    return (
      <div className="flex items-center justify-center h-full w-full p-8">
        <div style={{ width: `${data.size}%` }} className="relative">
          {hasError ? (
            <div className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[80px]">
              <AlertCircle className="h-6 w-6 mb-2" />
              <p className="text-center">{errorMessage || "Erro ao carregar áudio"}</p>
            </div>
          ) : (
            <div className="bg-card border rounded-lg p-4">
              <audio
                ref={mediaRef as React.RefObject<HTMLAudioElement>}
                src={resolvedSrc || data.src}
                onLoadedData={() => setIsLoading(false)}
                onTimeUpdate={handleTimeUpdate}
                onEnded={() => setIsPlaying(false)}
                onError={() => {
                  setIsLoading(false)
                  setHasError(true)
                  setErrorMessage("Não foi possível carregar o áudio")
                }}
              >
                <source src={resolvedSrc || data.src} type={data.audioType || "audio/mpeg"} />
              </audio>

              {/* Custom controls */}
              <div className="space-y-2">
                <Slider
                  value={[currentTime]}
                  max={duration || 100}
                  step={0.1}
                  onValueChange={handleSliderChange}
                  className="cursor-pointer"
                />
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={togglePlay}
                    >
                      {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                    </Button>
                    <span className="text-xs text-muted-foreground">
                      {formatTime(currentTime)} / {formatTime(duration)}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={toggleMute}
                    >
                      {muted ? <VolumeX className="h-4 w-4" /> : <Volume2 className="h-4 w-4" />}
                    </Button>
                    <Slider
                      value={[muted ? 0 : volume]}
                      max={1}
                      step={0.1}
                      onValueChange={handleVolumeChange}
                      className="w-20"
                    />
                  </div>
                </div>
              </div>
            </div>
          )}
          {data.caption && (
            <div className="mt-2 text-sm text-muted-foreground text-center">{data.caption}</div>
          )}
        </div>
      </div>
    )
  }

  const renderEmbeddedAudio = () => {
    let embedUrl = ""
    let audioId = ""
    let height = "80"

    if (data.embedAudioType === "youtube") {
      const match = data.src.match(/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i)
      if (match && match[1]) {
        audioId = match[1]
        embedUrl = `https://www.youtube-nocookie.com/embed/${audioId}?feature=oembed&enablejsapi=1&showinfo=0&controls=1&disablekb=1&rel=0&modestbranding=1&vq=small&iv_load_policy=3&fs=0`
        height = "60"
      }
    } else if (data.embedAudioType === "spotify") {
      const match = data.src.match(/(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i)
      if (match && match[1]) {
        audioId = match[1]
        embedUrl = `https://open.spotify.com/embed/track/${audioId}`
      }
    } else if (data.embedAudioType === "soundcloud") {
      const match = data.src.match(/soundcloud\.com\/([^/]+\/[^/]+)/i)
      if (match && match[1]) {
        audioId = match[1]
        embedUrl = `https://w.soundcloud.com/player/?url=https%3A//soundcloud.com/${audioId}&color=%23ff5500&auto_play=false&hide_related=false&show_comments=true&show_user=true&show_reposts=false&show_teaser=true`
        height = "166"
      }
    }

    if (!embedUrl) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4">
            <AlertCircle className="h-6 w-6 mb-2 mx-auto" />
            <p className="text-center">URL de áudio inválida</p>
          </div>
        </div>
      )
    }

    return (
      <div className="flex items-center justify-center h-full w-full p-8">
        <div style={{ width: `${data.size}%` }} className="relative">
          <iframe
            src={embedUrl}
            height={height}
            className="w-full rounded-lg border"
            allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
            loading="lazy"
            onLoad={() => setIsLoading(false)}
            // @ts-expect-error credentialless is not yet in React's iframe types
            credentialless="true"
          />
          {data.caption && (
            <div className="mt-2 text-sm text-muted-foreground text-center">{data.caption}</div>
          )}
        </div>
      </div>
    )
  }

  const renderContent = () => {
    if (!data.src) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center text-muted-foreground">
            <p>Nenhuma mídia selecionada</p>
            <p className="text-sm mt-1">Configure a URL da mídia no painel à esquerda</p>
          </div>
        </div>
      )
    }

    switch (data.type) {
      case "image":
        return renderImage()
      case "video":
        return renderVideo()
      case "audio":
        return renderAudio()
      default:
        return null
    }
  }

  return (
    <div className="w-full h-full bg-muted/30 flex items-center justify-center overflow-auto">
      {renderContent()}
    </div>
  )
}
