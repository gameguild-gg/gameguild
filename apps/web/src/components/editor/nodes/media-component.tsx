"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useLexicalNodeSelection } from "@lexical/react/useLexicalNodeSelection"
import { mergeRegister } from "@lexical/utils"
import {
  $getNodeByKey,
  $getSelection,
  $isNodeSelection,
  CLICK_COMMAND,
  COMMAND_PRIORITY_LOW,
  KEY_BACKSPACE_COMMAND,
  KEY_DELETE_COMMAND,
  type NodeKey,
  SELECTION_CHANGE_COMMAND,
} from "lexical"
import { Settings } from "lucide-react"
import { Button } from "@/components/ui/button"
import { MediaNodeBase, type BaseMediaData } from "./base/media-node-base"
import { UnifiedMediaEditor } from "@/components/editor/extras/media/unified-media-editor"

interface MediaComponentProps {
  nodeKey: NodeKey
  data: BaseMediaData
  NodeClass: typeof MediaNodeBase
}

export function MediaComponent({ nodeKey, data, NodeClass }: MediaComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isSelected, setSelected, clearSelection] = useLexicalNodeSelection(nodeKey)
  const [showEditor, setShowEditor] = useState(data.isNew || false)
  const [showMenu, setShowMenu] = useState(false)
  const [hasAutoOpened, setHasAutoOpened] = useState(false)
  const mediaRef = useRef<HTMLDivElement>(null)

  const onDelete = useCallback(
    (payload: KeyboardEvent) => {
      if (isSelected && $isNodeSelection($getSelection())) {
        const event: KeyboardEvent = payload
        event.preventDefault()
        const node = $getNodeByKey(nodeKey)
        if (node) {
          node.remove()
        }
      }
      return false
    },
    [isSelected, nodeKey],
  )

  const updateMedia = useCallback(
    (newData: Partial<BaseMediaData>) => {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof NodeClass) {
          node.setData({ ...data, ...newData })
        }
      })
    },
    [editor, nodeKey, data, NodeClass],
  )

  useEffect(() => {
    return mergeRegister(
      editor.registerCommand(
        CLICK_COMMAND,
        (payload) => {
          const event = payload
          if (mediaRef.current?.contains(event.target as Node)) {
            if (!event.shiftKey) {
              clearSelection()
            }
            setSelected(!isSelected)
            return true
          }
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
      editor.registerCommand(KEY_DELETE_COMMAND, onDelete, COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_BACKSPACE_COMMAND, onDelete, COMMAND_PRIORITY_LOW),
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        () => {
          if ($isNodeSelection($getSelection())) {
            return false
          }
          clearSelection()
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
    )
  }, [clearSelection, editor, isSelected, nodeKey, onDelete, setSelected])

  // Auto-open editor for new media and remove isNew flag
  useEffect(() => {
    if (data.isNew && !hasAutoOpened) {
      setShowEditor(true)
      setHasAutoOpened(true)
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof NodeClass) {
          const newData = { ...data, isNew: false }
          node.setData(newData)
        }
      })
    }
  }, [data, hasAutoOpened, editor, nodeKey, NodeClass])

  const renderMediaContent = () => {
    switch (data.type) {
      case "image":
        return (
          <img
            src={data.src || "/placeholder.svg"}
            alt={data.alt || ""}
            style={{ width: `${data.size}%` }}
            className="h-auto rounded-lg transition-all duration-200 mx-auto"
          />
        )

      case "video":
        return renderVideoContent()

      case "audio":
        return renderAudioContent()

      default:
        return null
    }
  }

  const renderVideoContent = () => {
    const embedType = data.embedType

    if (embedType && embedType !== "direct") {
      return renderVideoEmbed()
    }

    return (
      <video
        src={data.src}
        className="w-full h-auto rounded-lg"
        controls
        style={{ width: `${data.size}%` }}
      >
        <source src={data.src} type={data.videoType || "video/mp4"} />
        Seu navegador não suporta vídeo.
      </video>
    )
  }

  const renderVideoEmbed = () => {
    let embedUrl = ""
    const embedType = data.embedType

    if (embedType === "youtube") {
      const match = data.src.match(/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i)
      if (match && match[1]) {
        embedUrl = `https://www.youtube.com/embed/${match[1]}?enablejsapi=1`
      }
    } else if (embedType === "vimeo") {
      const match = data.src.match(/(?:vimeo\.com\/(?:video\/)?|player\.vimeo\.com\/video\/)([0-9]+)/i)
      if (match && match[1]) {
        embedUrl = `https://player.vimeo.com/video/${match[1]}`
      }
    } else if (embedType === "dailymotion") {
      const match = data.src.match(/(?:dailymotion\.com\/(?:video\/|embed\/video\/)|dai\.ly\/)([a-zA-Z0-9]+)/i)
      if (match && match[1]) {
        embedUrl = `https://www.dailymotion.com/embed/video/${match[1]}`
      }
    }

    if (!embedUrl) {
      return <div className="text-red-500 p-4">URL de vídeo inválida</div>
    }

    return (
      <div style={{ width: `${data.size}%` }} className="relative mx-auto">
        <div className="relative pt-[56.25%]">
          <iframe
            src={embedUrl}
            className="absolute inset-0 w-full h-full rounded-lg"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
          />
        </div>
      </div>
    )
  }

  const renderAudioContent = () => {
    const embedType = data.embedAudioType

    if (embedType && embedType !== "direct") {
      return renderAudioEmbed()
    }

    return (
      <div style={{ width: `${data.size}%` }} className="mx-auto">
        <div className="bg-card border rounded-lg p-4">
          <audio src={data.src} controls className="w-full">
            <source src={data.src} type={data.audioType || "audio/mpeg"} />
            Seu navegador não suporta áudio.
          </audio>
        </div>
      </div>
    )
  }

  const renderAudioEmbed = () => {
    let embedUrl = ""
    let height = "80"
    const embedType = data.embedAudioType

    if (embedType === "youtube") {
      const match = data.src.match(/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i)
      if (match && match[1]) {
        embedUrl = `https://www.youtube.com/embed/${match[1]}?feature=oembed&enablejsapi=1&showinfo=0&controls=1&disablekb=1&rel=0&modestbranding=1&vq=small&iv_load_policy=3&fs=0`
        height = "60"
      }
    } else if (embedType === "spotify") {
      const match = data.src.match(/(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i)
      if (match && match[1]) {
        embedUrl = `https://open.spotify.com/embed/track/${match[1]}`
      }
    } else if (embedType === "soundcloud") {
      const match = data.src.match(/soundcloud\.com\/([^/]+\/[^/]+)/i)
      if (match && match[1]) {
        embedUrl = `https://w.soundcloud.com/player/?url=https%3A//soundcloud.com/${match[1]}&color=%23ff5500&auto_play=false&hide_related=false&show_comments=true&show_user=true&show_reposts=false&show_teaser=true`
        height = "166"
      }
    }

    if (!embedUrl) {
      return <div className="text-red-500 p-4">URL de áudio inválida</div>
    }

    return (
      <div style={{ width: `${data.size}%` }} className="mx-auto">
        <iframe
          src={embedUrl}
          height={height}
          className="w-full rounded-lg border"
          allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
          loading="lazy"
        />
      </div>
    )
  }

  const getMarginClass = () => {
    switch (data.type) {
      case "image":
        return "my-8"
      case "video":
        return "my-8"
      case "audio":
        return "my-6"
      default:
        return "my-8"
    }
  }

  return (
    <>
      <div
        ref={mediaRef}
        className={`${getMarginClass()} relative group ${data.type}-wrapper ${isSelected ? "ring-2 ring-blue-500 rounded-lg" : ""}`}
        onMouseEnter={() => setShowMenu(true)}
        onMouseLeave={() => setShowMenu(false)}
      >
        <div className="relative flex justify-center">
          <div onClick={() => setShowEditor(true)} className="cursor-pointer w-full">
            {renderMediaContent()}
          </div>

          {/* Settings button */}
          {showMenu && (
            <div className="absolute top-2 right-2">
              <Button
                variant="secondary"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  setShowEditor(true)
                }}
              >
                <Settings className="h-4 w-4" />
              </Button>
            </div>
          )}
        </div>

        {/* Display caption */}
        {data.caption && (
          <div className="mt-2 text-sm text-muted-foreground text-center">{data.caption}</div>
        )}
      </div>

      {/* Unified Editor Modal */}
      {showEditor && (
        <UnifiedMediaEditor
          data={data}
          onChange={updateMedia}
          onClose={() => setShowEditor(false)}
        />
      )}
    </>
  )
}
