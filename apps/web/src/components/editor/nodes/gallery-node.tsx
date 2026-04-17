"use client"

import { ContentEditMenu, type EditMenuOption } from "@/components/editor/extras/content-edit-menu"
import { UnifiedMediaEditor } from "@/components/editor/extras/media/unified-media-editor"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from "lexical"
import { ImageIcon } from "lucide-react"
import { useContext, useEffect, useState } from "react"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "@/components/editor/engines/lexical/lexical-editor"
import type { BaseMediaData } from "./base/media-node-base"
import { AssetImage } from "../extras/media/asset-image"

export type GalleryLayout = "1" | "2" | "3" | "4"

export type ImageDisplayMode = "crop" | "adaptive"

export interface GalleryImage {
  id: string
  src: string
  alt: string
  caption?: string
  displayMode?: ImageDisplayMode
  span?: "1x1" | "1x2" | "2x1" | "2x2"
  aspectRatio?: number
  gridPosition?: { rowStart: number; colStart: number; rowSpan: number; colSpan: number }
}

export interface GalleryData {
  images: GalleryImage[]
  layout: GalleryLayout
  caption?: string
  captionStyle?: {
    fontSize?: "xs" | "sm" | "base" | "lg"
    fontFamily?: "sans" | "serif" | "mono"
    fontWeight?: "normal" | "medium"
    fontStyle?: "normal" | "italic"
  }
  isNew?: boolean
  defaultDisplayMode?: ImageDisplayMode
}

export interface SerializedGalleryNode extends SerializedLexicalNode {
  type: "gallery"
  data: GalleryData
  version: 1
}

export class GalleryNode extends DecoratorNode<JSX.Element> {
  __data: GalleryData

  static getType(): string {
    return "gallery"
  }

  static clone(node: GalleryNode): GalleryNode {
    return new GalleryNode(node.__data, node.__key)
  }

  constructor(data: GalleryData, key?: string) {
    super(key)
    this.__data = {
      images: data.images || [],
      layout: data.layout || "2",
      caption: data.caption || "",
      isNew: data.isNew,
      defaultDisplayMode: data.defaultDisplayMode || "crop",
    }
  }

  createDOM(): HTMLElement {
    const div = document.createElement("div")
    div.style.display = "contents"
    return div
  }

  updateDOM(): false {
    return false
  }

  setData(data: GalleryData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedGalleryNode {
    return {
      type: "gallery",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedGalleryNode): GalleryNode {
    return new GalleryNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <GalleryComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface GalleryComponentProps {
  data: GalleryData
  nodeKey: string
}

function GalleryComponent({ data, nodeKey }: GalleryComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading)
  const [showMenu, setShowMenu] = useState(false)

  // Remove isNew flag after first render
  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof GalleryNode) {
          const { isNew, ...rest } = data
          node.setData(rest)
        }
      })
    }
  }, [data, editor, nodeKey])

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false)
    }
  }, [isLoading])

  // Convert GalleryImage to BaseMediaData
  const galleryToMediaItems = (images: GalleryImage[]): BaseMediaData[] => {
    return images.map(img => ({
      type: 'image' as const,
      src: img.src,
      alt: img.alt,
      caption: img.caption,
      size: 100,
      // Preserve placeholder and static properties stored in alt field as JSON
      ...(() => {
        try {
          // Check if alt contains our metadata
          if (img.alt && img.alt.startsWith('__metadata__:')) {
            const metadata = JSON.parse(img.alt.substring(13))
            return {
              isPlaceholder: metadata.isPlaceholder,
              isStatic: metadata.isStatic,
              gridPosition: metadata.gridPosition,
              alt: metadata.originalAlt || '',
            }
          }
        } catch (e) {
          // Not metadata, just regular alt
        }
        return {}
      })(),
    }))
  }

  // Convert BaseMediaData back to GalleryImage
  const mediaItemsToGallery = (items: BaseMediaData[]): GalleryImage[] => {
    return items.map(item => {
      // Store metadata in alt field if item is placeholder or static
      let altText = item.alt || ''
      if (item.isPlaceholder || item.isStatic) {
        const metadata = {
          isPlaceholder: item.isPlaceholder,
          isStatic: item.isStatic,
          gridPosition: item.gridPosition,
          originalAlt: item.alt || '',
        }
        altText = `__metadata__:${JSON.stringify(metadata)}`
      }

      return {
        id: Math.random().toString(36).substring(7),
        src: item.src || '', // Ensure src is always a string
        alt: altText,
        caption: item.caption || '',
        displayMode: 'adaptive' as const,
        span: '1x1' as const,
      }
    })
  }

  const handleSaveGallery = (items?: BaseMediaData[], columns?: number, caption?: string) => {
    if (!items || items.length === 0) return

    // Filter items: keep those with src OR those that are placeholders
    const validItems = items.filter(item =>
      item.isPlaceholder || (item.src && item.src.trim() !== "")
    )

    if (validItems.length === 0) return

    // Se tem apenas 1 item e não é placeholder, converte de volta para ImageNode simples
    if (validItems.length === 1 && !validItems[0]?.isPlaceholder) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node) {
          // Import ImageNode dynamically
          const { ImageNode } = require('./image-node')

          // Create simple image node
          const imageNode = new ImageNode(validItems[0])

          // Replace gallery with simple image
          node.replace(imageNode)
        }
      })
      setIsEditing(false)
      return
    }

    // Se tem 2+ itens ou 1 placeholder, mantém como galeria
    const galleryImages = mediaItemsToGallery(validItems)

    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof GalleryNode) {
        node.setData({
          ...data,
          images: galleryImages,
          layout: (columns?.toString() || data.layout) as GalleryLayout,
          caption: caption !== undefined ? caption : data.caption,
        })
      }
    })
    setIsEditing(false)
  }

  // Edit menu options
  const editMenuOptions: EditMenuOption[] = [
    {
      id: "edit",
      icon: <ImageIcon className="h-4 w-4" />,
      label: "Edit gallery",
      action: () => setIsEditing(true),
    },
  ]

  // Get grid template columns based on layout
  const getGridTemplateColumns = () => {
    return `repeat(${data.layout}, 1fr)`
  }

  if (!isEditing) {
    return (
      <div className="my-8 relative group">
        <div className="relative">
          {data.images.length > 0 ? (
            <>
              <div
                className="grid gap-3"
                style={{
                  gridTemplateColumns: getGridTemplateColumns(),
                }}
              >
                {data.images.map((image) => {
                  // Check if this is a placeholder (empty src means it's a placeholder)
                  const isPlaceholder = !image.src || image.src.trim() === ''

                  return (
                    <div key={image.id} className="space-y-2">
                      <div
                        className="relative overflow-hidden rounded-md bg-gray-200 dark:bg-gray-700"
                        style={{ aspectRatio: image.displayMode === "crop" ? "1/1" : "16/9" }}
                      >
                        {isPlaceholder || !image.src ? (
                          // Render empty space for placeholders - maintains aspect ratio from parent
                          <div className="h-full w-full" />
                        ) : (
                          // Render actual image
                          <AssetImage
                            src={image.src}
                            alt={image.alt}
                            className={
                              image.displayMode === "crop"
                                ? "h-full w-full object-cover"
                                : "h-auto w-full object-contain"
                            }
                          />
                        )}
                      </div>
                      {image.caption && (
                        <p className="text-sm text-gray-600 dark:text-gray-400 text-center">
                          {image.caption}
                        </p>
                      )}
                    </div>
                  )
                })}
              </div>
              {data.caption && (
                <div className="mt-4 text-sm text-gray-600 dark:text-gray-400 text-center">
                  {data.caption}
                </div>
              )}
            </>
          ) : (
            <div className="flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed">
              <div className="text-center">
                <ImageIcon className="h-8 w-8 mx-auto text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">No images in gallery</p>
              </div>
            </div>
          )}

          {/* Edit menu */}
          <ContentEditMenu options={editMenuOptions} className="opacity-100" />
        </div>
      </div>
    )
  }

  // When editing, use UnifiedMediaEditor
  return (
    <>
      <div className="my-8 relative group">
        <div className="relative">
          {data.images.length > 0 ? (
            <div
              className="grid gap-3"
              style={{
                gridTemplateColumns: getGridTemplateColumns(),
              }}
            >
              {data.images.map((image) => {
                // Check if this is a placeholder (empty src means it's a placeholder)
                const isPlaceholder = !image.src || image.src.trim() === ''

                return (
                  <div key={image.id} className="space-y-2">
                    <div
                      className="relative overflow-hidden rounded-md bg-gray-200 dark:bg-gray-700"
                      style={{ aspectRatio: image.displayMode === "crop" ? "1/1" : "16/9" }}
                    >
                      {isPlaceholder || !image.src ? (
                        // Render empty space for placeholders - maintains aspect ratio from parent
                        <div className="h-full w-full" />
                      ) : (
                        // Render actual image
                        <AssetImage
                          src={image.src}
                          alt={image.alt}
                          className={
                            image.displayMode === "crop"
                              ? "h-full w-full object-cover"
                              : "h-auto w-full object-contain"
                          }
                        />
                      )}
                    </div>
                    {image.caption && (
                      <p className="text-sm text-gray-600 dark:text-gray-400 text-center">
                        {image.caption}
                      </p>
                    )}
                  </div>
                )
              })}
            </div>
          ) : (
            <div className="flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed">
              <div className="text-center">
                <ImageIcon className="h-8 w-8 mx-auto text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">No images in gallery</p>
              </div>
            </div>
          )}
        </div>
      </div>

      <UnifiedMediaEditor
        data={galleryToMediaItems(data.images)[0] || { type: 'image', src: '', alt: '', size: 100 }}
        onChange={() => { }}
        onClose={() => setIsEditing(false)}
        onSave={handleSaveGallery}
        galleryItems={galleryToMediaItems(data.images)}
        galleryColumns={Number.parseInt(data.layout)}
        galleryCaption={data.caption}
      />
    </>
  )
}

export function $createGalleryNode(): GalleryNode {
  return new GalleryNode({
    images: [],
    layout: "2",
    caption: "",
    isNew: true,
    defaultDisplayMode: "crop",
  })
}
