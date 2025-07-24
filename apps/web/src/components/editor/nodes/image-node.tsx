"use client"

import { useEffect, useRef, useState } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Move, Type } from "lucide-react"

import { ImageSizeControl } from "@/components/editor/ui/image-size-control"
import { CaptionInput } from "@/components/editor/ui/caption-input"
import { Button } from "@/components/ui/button"
import { X } from "lucide-react"
import { ContentEditMenu, type EditMenuOption } from "@/components/editor/ui/content-edit-menu"

export interface ImageData {
  src: string
  alt: string
  caption?: string
  size?: number // Size as a percentage (1-100)
}

export interface SerializedImageNode extends SerializedLexicalNode {
  type: "image"
  data: ImageData
  version: 1
}

export class ImageNode extends DecoratorNode<JSX.Element> {
  __data: ImageData

  static getType(): string {
    return "image"
  }

  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.__data, node.__key)
  }

  constructor(data: ImageData, key?: string) {
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

  setData(data: ImageData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedImageNode {
    return {
      type: "image",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedImageNode): ImageNode {
    return new ImageNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <ImageComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface ImageComponentProps {
  data: ImageData
  nodeKey: string
}

function ImageComponent({ data, nodeKey }: ImageComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isEditing, setIsEditing] = useState(false)
  const [caption, setCaption] = useState(data.caption || "")
  const [size, setSize] = useState(data.size || 100)
  const [showSizeControls, setShowSizeControls] = useState(false)
  const [showMenu, setShowMenu] = useState(false)
  const imageRef = useRef<HTMLImageElement>(null)
  const controlsRef = useRef<HTMLDivElement>(null)
  const sizeControlsRef = useRef<HTMLDivElement>(null)
  const captionControlsRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const isClickInsideImage = imageRef.current && imageRef.current.contains(e.target as Node)
      const isClickInsideControls = controlsRef.current && controlsRef.current.contains(e.target as Node)
      const isClickInsideSizeControls = sizeControlsRef.current && sizeControlsRef.current.contains(e.target as Node)
      const isClickInsideCaptionControls =
        captionControlsRef.current && captionControlsRef.current.contains(e.target as Node)
      const isClickInsideWrapper =
        e.target instanceof Node && (e.target as HTMLElement).closest(".image-wrapper") !== null

      // Não fechamos automaticamente nenhum controle ao clicar fora
      // Apenas os botões X podem fechar os controles
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => {
      document.removeEventListener("mousedown", handleClickOutside)
    }
  }, [])

  const updateImage = (newData: Partial<ImageData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof ImageNode) {
        node.setData({ ...data, ...newData })
      }
    })
  }

  const handleCaptionChange = (newCaption: string) => {
    setCaption(newCaption)
    updateImage({ caption: newCaption })
  }

  const handleSizeChange = (newSize: number) => {
    setSize(newSize)
    updateImage({ size: newSize })
  }

  // Opções do menu de edição
  const editMenuOptions: EditMenuOption[] = [
    {
      id: "size",
      icon: <Move className="h-4 w-4" />,
      label: "Ajustar tamanho",
      action: () => setShowSizeControls(true),
    },
    {
      id: "caption",
      icon: <Type className="h-4 w-4" />,
      label: caption ? "Editar legenda" : "Adicionar legenda",
      action: () => setIsEditing(true),
    },
  ]

  return (
    <div
      className="my-8 relative group image-wrapper"
      onMouseEnter={() => setShowMenu(true)}
      onMouseLeave={() => setShowMenu(false)}
    >
      <div className="relative flex justify-center">
        <img
          ref={imageRef}
          src={data.src || "/placeholder.svg"}
          alt={data.alt}
          style={{ width: `${size}%` }}
          className="h-auto rounded-lg cursor-pointer transition-all duration-200"
        />

        {/* Menu de edição */}
        {showMenu && <ContentEditMenu options={editMenuOptions} />}
      </div>

      {/* Controle de tamanho */}
      {showSizeControls && (
        <div
          ref={sizeControlsRef}
          className="absolute -top-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Ajustar tamanho</span>
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

      {/* Editor de legenda */}
      {isEditing && (
        <div
          ref={captionControlsRef}
          className="absolute -bottom-16 left-2 right-2 rounded-lg bg-background/80 p-2 backdrop-blur z-20"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium">Adicionar legenda</span>
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

      {/* Exibir legenda quando não estiver editando */}
      {!isEditing && caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}

export function $createImageNode(data: ImageData): ImageNode {
  return new ImageNode(data)
}
