"use client"

import { useEffect, useRef, useState } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { X, Move, Type, Play } from "lucide-react"
import type { JSX } from "react/jsx-runtime" // Import JSX to fix the undeclared variable error

import { ImageSizeControl } from "@/components/editor/ui/image-size-control"
import { CaptionInput } from "@/components/editor/ui/caption-input"
import { Button } from "@/components/ui/button"
import { ContentEditMenu, type EditMenuOption } from "@/components/editor/ui/content-edit-menu"

export interface YouTubeData {
  videoId: string
  title?: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  isNew?: boolean // Flag to indicate if the video was newly inserted
  startAt?: number // Start time in seconds
  showControls?: boolean
  showInfo?: boolean
  showRelated?: boolean
}

export interface SerializedYouTubeNode extends SerializedLexicalNode {
  type: "youtube"
  data: YouTubeData
  version: 1
}

export class YouTubeNode extends DecoratorNode<JSX.Element> {
  __data: YouTubeData

  static getType(): string {
    return "youtube"
  }

  static clone(node: YouTubeNode): YouTubeNode {
    return new YouTubeNode(node.__data, node.__key)
  }

  constructor(data: YouTubeData, key?: string) {
    super(key)
    this.__data = {
      ...data,
      size: data.size ?? 100, // Default to 100% if not specified
      showControls: data.showControls ?? true,
      showInfo: data.showInfo ?? true,
      showRelated: data.showRelated ?? false,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: YouTubeData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedYouTubeNode {
    return {
      type: "youtube",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedYouTubeNode): YouTubeNode {
    return new YouTubeNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <YouTubeComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface YouTubeComponentProps {
  data: YouTubeData
  nodeKey: string
}

function YouTubeComponent({ data, nodeKey }: YouTubeComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isEditing, setIsEditing] = useState(false)
  const [caption, setCaption] = useState(data.caption || "")
  const [size, setSize] = useState(data.size || 100)
  // Show size control automatically for new videos
  const [showSizeControls, setShowSizeControls] = useState(data.isNew || false)
  const [showMenu, setShowMenu] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [hasError, setHasError] = useState(false)

  const containerRef = useRef<HTMLDivElement>(null)
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const sizeControlsRef = useRef<HTMLDivElement>(null)
  const captionControlsRef = useRef<HTMLDivElement>(null)

  // Remove the isNew flag after first render
  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof YouTubeNode) {
          const { isNew, ...rest } = data
          node.setData(rest)
        }
      })
    }
  }, [data, editor, nodeKey])

  const updateYouTube = (newData: Partial<YouTubeData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof YouTubeNode) {
        node.setData({ ...data, ...newData })
      }
    })
  }

  const handleCaptionChange = (newCaption: string) => {
    setCaption(newCaption)
    updateYouTube({ caption: newCaption })
  }

  const handleSizeChange = (newSize: number) => {
    setSize(newSize)
    updateYouTube({ size: newSize })
  }

  // Build YouTube embed URL with parameters
  const getYouTubeEmbedUrl = () => {
    let url = `https://www.youtube.com/embed/${data.videoId}?enablejsapi=1`

    // Add start time if specified
    if (data.startAt && data.startAt > 0) {
      url += `&start=${data.startAt}`
    }

    // Add controls parameter
    url += `&controls=${data.showControls ? "1" : "0"}`

    // Add showinfo parameter
    url += `&showinfo=${data.showInfo ? "1" : "0"}`

    // Add related videos parameter
    url += `&rel=${data.showRelated ? "1" : "0"}`

    // Add modestbranding parameter (always enabled to reduce YouTube branding)
    url += "&modestbranding=1"

    return url
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
      <Play className="h-6 w-6 mb-2" />
      <p className="text-center">Could not load the YouTube video</p>
    </div>
  )

  return (
    <div ref={containerRef} className="my-8 relative group">
      <div className="relative flex justify-center">
        {hasError ? (
          renderErrorMessage()
        ) : (
          <div style={{ width: `${size}%` }} className="relative">
            <div className="relative pt-[56.25%]">
              {/* 16:9 Aspect Ratio */}
              <iframe
                ref={iframeRef}
                src={getYouTubeEmbedUrl()}
                title={data.title || "YouTube video player"}
                className="absolute top-0 left-0 w-full h-full rounded-lg"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                allowFullScreen
                onLoad={() => setIsLoading(false)}
                onError={() => {
                  setIsLoading(false)
                  setHasError(true)
                }}
              ></iframe>

              {isLoading && (
                <div className="absolute inset-0 flex items-center justify-center bg-black/10 rounded-lg">
                  <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full"></div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Edit menu */}
      {!hasError && <ContentEditMenu options={editMenuOptions} />}

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
      {!isEditing && caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}

export function $createYouTubeNode(data: YouTubeData): YouTubeNode {
  return new YouTubeNode(data)
}

// Helper function to extract YouTube video ID from various URL formats
export function extractYouTubeVideoId(url: string): string | null {
  // Regular expression to match YouTube video IDs from various URL formats
  const regExp = /^.*(youtu.be\/|v\/|e\/|u\/\w+\/|embed\/|v=)([^#&?]*).*/
  const match = url.match(regExp)

  if (match && match[2].length === 11) {
    return match[2]
  }

  return null
}
