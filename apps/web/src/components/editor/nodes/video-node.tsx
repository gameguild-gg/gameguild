"use client"

import { useEffect, useRef, useState } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Play, Pause, Volume2, VolumeX, Maximize, AlertCircle, X, Move, Type } from "lucide-react"

import { ImageSizeControl } from "@/components/ui/image-size-control"
import { CaptionInput } from "@/components/ui/caption-input"
import { Button } from "@/components/ui/button"
import { Slider } from "@/components/ui/slider"
import { cn } from "@/lib/utils"
import { ContentEditMenu, type EditMenuOption } from "@/components/ui/content-edit-menu"

export interface VideoData {
  src: string
  type?: string
  alt: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  isNew?: boolean // Flag to indicate if the video was newly inserted
}

export interface SerializedVideoNode extends SerializedLexicalNode {
  type: "video"
  data: VideoData
  version: 1
}

export class VideoNode extends DecoratorNode<JSX.Element> {
  __data: VideoData

  static getType(): string {
    return "video"
  }

  static clone(node: VideoNode): VideoNode {
    return new VideoNode(node.__data, node.__key)
  }

  constructor(data: VideoData, key?: string) {
    super(key)
    this.__data = {
      ...data,
      size: data.size ?? 100, // Default to 100% if not specified
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: VideoData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedVideoNode {
    return {
      type: "video",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedVideoNode): VideoNode {
    return new VideoNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <VideoComponent data={this.__data} nodeKey={this.__key} />
  }
}

// Function to detect and extract video IDs from different platforms
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

  // URL not recognized as a video platform
  return null
}

// Component to render embedded videos
function EmbeddedVideo({
  embedInfo,
  size,
  onError,
  isEditing,
  setIsEditing,
  showSizeControls,
  setShowSizeControls,
  onSizeChange,
  caption,
  onCaptionChange,
}: {
  embedInfo: { type: string; id: string }
  size: number
  onError: () => void
  isEditing?: boolean
  setIsEditing?: (editing: boolean) => void
  showSizeControls?: boolean
  setShowSizeControls?: (show: boolean) => void
  onSizeChange?: (size: number) => void
  caption?: string
  onCaptionChange?: (caption: string) => void
}) {
  const [isLoading, setIsLoading] = useState(true)
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const controlsRef = useRef<HTMLDivElement>(null)
  const captionControlsRef = useRef<HTMLDivElement>(null)
  const [showMenu, setShowMenu] = useState(false)

  let embedUrl = ""
  let title = ""

  switch (embedInfo.type) {
    case "youtube":
      embedUrl = `https://www.youtube.com/embed/${embedInfo.id}?enablejsapi=1`
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
      onError()
      return null
  }

  const editMenuOptions: EditMenuOption[] = [
    {
      id: "size",
      icon: <Move className="h-4 w-4" />,
      label: "Adjust size",
      action: () => {
        if (setShowSizeControls) {
          setShowSizeControls(true)
        }
      },
    },
    {
      id: "caption",
      icon: <Type className="h-4 w-4" />,
      label: caption ? "Edit caption" : "Add caption",
      action: () => {
        if (setIsEditing) {
          setIsEditing(true)
        }
      },
    },
  ]

  return (
    <div
      style={{ width: `${size}%` }}
      className="relative"
      onMouseEnter={() => setShowMenu(true)}
      onMouseLeave={() => setShowMenu(false)}
    >
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-black/10 rounded-lg">
          <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      )}
      <div className="relative pt-[56.25%]">
        {/* 16:9 Aspect Ratio */}
        <iframe
          ref={iframeRef}
          src={embedUrl}
          title={title}
          className="absolute top-0 left-0 w-full h-full rounded-lg"
          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
          allowFullScreen
          onLoad={() => setIsLoading(false)}
          onError={onError}
        ></iframe>
      </div>

      {/* Edit menu */}
      {showMenu && <ContentEditMenu options={editMenuOptions} />}

      {showSizeControls && onSizeChange && setShowSizeControls && (
        <div
          className="absolute -top-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Adjust size</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  setShowSizeControls(false)
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <ImageSizeControl size={size} onChange={onSizeChange} />
          </div>
        </div>
      )}

      {isEditing && onCaptionChange && setIsEditing && (
        <div
          ref={captionControlsRef}
          className="absolute -bottom-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Add caption</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  setIsEditing(false)
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <CaptionInput caption={caption || ""} onChange={onCaptionChange} autoFocus={true} />
          </div>
        </div>
      )}

      {/* Display caption when not editing */}
      {!isEditing && caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}

interface VideoComponentProps {
  data: VideoData
  nodeKey: string
}

function VideoComponent({ data, nodeKey }: VideoComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isEditing, setIsEditing] = useState(false)
  const [caption, setCaption] = useState(data.caption || "")
  const [size, setSize] = useState(data.size || 100)
  // Show size control automatically for new videos or without caption
  const [showSizeControls, setShowSizeControls] = useState(data.isNew || false)
  const [showMenu, setShowMenu] = useState(false)

  // Video player state
  const [isPlaying, setIsPlaying] = useState(false)
  const [currentTime, setCurrentTime] = useState(0)
  const [duration, setDuration] = useState(0)
  const [volume, setVolume] = useState(0.7)
  const [muted, setMuted] = useState(false)
  const [showControls, setShowControls] = useState(false)

  // State for loading and error control
  const [isLoading, setIsLoading] = useState(true)
  const [hasError, setHasError] = useState(false)
  const [errorMessage, setErrorMessage] = useState("")

  // State for embedded video control
  const [embedInfo, setEmbedInfo] = useState<{ type: string; id: string } | null>(null)

  const videoRef = useRef<HTMLVideoElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const controlsRef = useRef<HTMLDivElement>(null)
  const sizeControlsRef = useRef<HTMLDivElement>(null)
  const captionControlsRef = useRef<HTMLDivElement>(null)

  // Detect if it's an embedded video when loading the component
  useEffect(() => {
    if (data.src) {
      const info = getVideoEmbedInfo(data.src)
      setEmbedInfo(info)

      // If it's an embedded video, we don't need to show the loading error
      if (info) {
        setIsLoading(false)
      }
    }
  }, [data.src])

  // Remove the isNew flag after first render
  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof VideoNode) {
          const { isNew, ...rest } = data
          node.setData(rest)
        }
      })
    }
  }, [data, editor, nodeKey])

  const updateVideo = (newData: Partial<VideoData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof VideoNode) {
        node.setData({ ...data, ...newData })
      }
    })
  }

  const handleCaptionChange = (newCaption: string) => {
    setCaption(newCaption)
    updateVideo({ caption: newCaption })
  }

  const handleSizeChange = (newSize: number) => {
    setSize(newSize)
    updateVideo({ size: newSize })
  }

  // Video player functions
  const togglePlay = () => {
    const video = videoRef.current
    if (!video || !data.src) return

    try {
      if (isPlaying) {
        video.pause()
      } else {
        // Check if the video is ready for playback
        if (video.readyState >= 2) {
          // HAVE_CURRENT_DATA or higher
          const playPromise = video.play()

          if (playPromise !== undefined) {
            playPromise
              .then(() => {
                setIsPlaying(true)
              })
              .catch((error) => {
                console.error("Error playing video:", error)
                setIsPlaying(false)
              })
          }
        } else {
          console.warn("Video is not ready for playback")
        }
      }
    } catch (error) {
      console.error("Error controlling video:", error)
      setIsPlaying(false)
    }
  }

  const handleTimeUpdate = () => {
    const video = videoRef.current
    if (!video) return

    setCurrentTime(video.currentTime)
    if (video.duration && !duration) {
      setDuration(video.duration)
    }
  }

  const handleSliderChange = (values: number[]) => {
    const video = videoRef.current
    if (!video) return

    const newTime = values[0]
    video.currentTime = newTime
    setCurrentTime(newTime)
  }

  const handleVolumeChange = (values: number[]) => {
    const video = videoRef.current
    if (!video) return

    const newVolume = values[0]
    video.volume = newVolume
    setVolume(newVolume)
    setMuted(newVolume === 0)
  }

  const toggleMute = () => {
    const video = videoRef.current
    if (!video) return

    video.muted = !video.muted
    setMuted(!muted)
  }

  const handleFullscreen = () => {
    const video = videoRef.current
    if (!video) return

    if (document.fullscreenElement) {
      document.exitFullscreen()
    } else {
      video.requestFullscreen()
    }
  }

  const formatTime = (time: number) => {
    const minutes = Math.floor(time / 60)
    const seconds = Math.floor(time % 60)
    return `${minutes}:${seconds < 10 ? "0" : ""}${seconds}`
  }

  const handleEmbedError = () => {
    setHasError(true)
    setErrorMessage("Could not load the embedded video. Please check the URL.")
  }

  // Edit menu options
  const editMenuOptions: EditMenuOption[] = [
    {
      id: "size",
      icon: <Move className="h-4 w-4" />,
      label: "Adjust size",
      action: () => setShowSizeControls(true),
    },
    {
      id: "caption",
      icon: <Type className="h-4 w-4" />,
      label: caption ? "Edit caption" : "Add caption",
      action: () => setIsEditing(true),
    },
  ]

  // Render error message
  const renderErrorMessage = () => (
    <div
      className="bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 rounded-lg p-4 flex flex-col items-center justify-center min-h-[200px]"
      style={{ width: `${size}%` }}
    >
      <AlertCircle className="h-6 w-6 mb-2" />
      <p className="text-center">{errorMessage}</p>
    </div>
  )

  return (
    <div
      ref={containerRef}
      className="my-8 relative group video-wrapper"
      onMouseEnter={() => {
        setShowControls(true)
        setShowMenu(true)
      }}
      onMouseLeave={() => {
        setShowControls(false)
        setShowMenu(false)
      }}
    >
      <div className="relative flex justify-center">
        {hasError ? (
          renderErrorMessage()
        ) : embedInfo ? (
          // Render embedded video (YouTube, Vimeo, etc.)
          <EmbeddedVideo
            embedInfo={embedInfo}
            size={size}
            onError={handleEmbedError}
            isEditing={isEditing}
            setIsEditing={setIsEditing}
            showSizeControls={showSizeControls}
            setShowSizeControls={setShowSizeControls}
            onSizeChange={handleSizeChange}
            caption={caption}
            onCaptionChange={handleCaptionChange}
          />
        ) : (
          // Render native video
          <>
            {isLoading && (
              <div className="absolute inset-0 flex items-center justify-center bg-black/10 rounded-lg z-10">
                <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full"></div>
              </div>
            )}
            <video
              ref={videoRef}
              src={data.src || ""}
              poster={data.alt ? `/placeholder.svg?text=${encodeURIComponent(data.alt)}` : undefined}
              style={{ width: `${size}%` }}
              className="h-auto rounded-lg cursor-pointer transition-all duration-200"
              onClick={(e) => {
                e.stopPropagation()
                // We don't do anything when clicking on the video now
                // Specific buttons control the actions
              }}
              onTimeUpdate={handleTimeUpdate}
              onPlay={() => setIsPlaying(true)}
              onPause={() => setIsPlaying(false)}
              onLoadedMetadata={() => {
                if (videoRef.current) {
                  setDuration(videoRef.current.duration)
                }
              }}
              onLoadStart={() => {
                setIsLoading(true)
                setHasError(false)
              }}
              onLoadedData={() => {
                setIsLoading(false)
              }}
              onError={(e) => {
                setIsLoading(false)
                setHasError(true)
                setErrorMessage("Could not load the video. Please check the format or URL.")
                console.error("Video error:", e)
              }}
            />

            {/* Edit menu */}
            {showMenu && !hasError && <ContentEditMenu options={editMenuOptions} />}

            {/* Video controls - only show if not embedded video and no error */}
            {!hasError && (
              <div
                ref={controlsRef}
                className={cn(
                  "absolute bottom-0 left-0 right-0 p-2 bg-black/60 backdrop-blur-sm rounded-b-lg transition-opacity duration-200",
                  showControls ? "opacity-100" : "opacity-0",
                )}
              >
                <div className="flex flex-col gap-2">
                  <div className="flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-white hover:bg-white/20"
                      onClick={(e) => {
                        e.stopPropagation()
                        togglePlay()
                      }}
                    >
                      {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                    </Button>

                    <div className="flex-1 px-2">
                      <Slider
                        value={[currentTime]}
                        min={0}
                        max={duration || 100}
                        step={0.1}
                        onValueChange={handleSliderChange}
                        className="cursor-pointer"
                      />
                    </div>

                    <span className="text-xs text-white min-w-[60px] text-right">
                      {formatTime(currentTime)} / {formatTime(duration)}
                    </span>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 text-white hover:bg-white/20"
                      onClick={(e) => {
                        e.stopPropagation()
                        toggleMute()
                      }}
                    >
                      {muted ? <VolumeX className="h-3 w-3" /> : <Volume2 className="h-3 w-3" />}
                    </Button>

                    <div className="w-24">
                      <Slider
                        value={[muted ? 0 : volume]}
                        min={0}
                        max={1}
                        step={0.1}
                        onValueChange={handleVolumeChange}
                        className="cursor-pointer"
                      />
                    </div>

                    <div className="flex-1" />

                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 text-white hover:bg-white/20"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleFullscreen()
                      }}
                    >
                      <Maximize className="h-3 w-3" />
                    </Button>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Size control */}
      {showSizeControls && !hasError && (
        <div
          ref={sizeControlsRef}
          className="absolute -top-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Adjust size</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  setShowSizeControls(false)
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <ImageSizeControl size={size} onChange={handleSizeChange} />
          </div>
        </div>
      )}

      {/* Caption editor */}
      {isEditing && !hasError && (
        <div
          ref={captionControlsRef}
          className="absolute -bottom-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Add caption</span>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  setIsEditing(false)
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <CaptionInput caption={caption} onChange={handleCaptionChange} autoFocus={true} />
          </div>
        </div>
      )}

      {/* Display caption when not editing */}
      {!isEditing && caption && !embedInfo && (
        <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>
      )}
    </div>
  )
}

export function $createVideoNode(data: VideoData): VideoNode {
  return new VideoNode(data)
}
