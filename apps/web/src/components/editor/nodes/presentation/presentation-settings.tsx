"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Plus, Trash2, ChevronUp, ChevronDown, FileText, ImageIcon, Edit, GripVertical, Settings } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Progress } from "@/components/ui/progress"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { MediaUploadDialog, type MediaUploadResult } from "@/components/editor/extras/media-upload-dialog"
import { SlidePlayer } from "@/components/editor/extras/presentation/slide-player"
import type { Slide, TransitionEffect } from "./types"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"
import { SlideEditDialog } from "@/components/editor/extras/presentation/slide-edit-dialog"

interface PresentationSettingsProps {
  title: string
  setTitle: (title: string) => void
  transitionEffect: TransitionEffect
  setTransitionEffect: (effect: TransitionEffect) => void
  autoAdvance: boolean
  setAutoAdvance: (autoAdvance: boolean) => void
  autoAdvanceDelay: number
  setAutoAdvanceDelay: (delay: number) => void
  autoAdvanceLoop: boolean
  setAutoAdvanceLoop: (loop: boolean) => void
  showControls: boolean
  setShowControls: (showControls: boolean) => void
  isImporting: boolean
  importProgress: number
  importStatus: string
  fileError: string | null
  onSave: () => void
  onCancel: () => void
  slidesCount: number
  hasPresentation: boolean
  isImageSlideshow: boolean
  slides: Slide[]
  setSlides: (slides: Slide[]) => void
  onImportPresentation: (result: MediaUploadResult) => void
  onImportImages: (results: MediaUploadResult | MediaUploadResult[]) => void
}

export function PresentationSettings({
  title,
  setTitle,
  transitionEffect,
  setTransitionEffect,
  autoAdvance,
  setAutoAdvance,
  autoAdvanceDelay,
  setAutoAdvanceDelay,
  autoAdvanceLoop,
  setAutoAdvanceLoop,
  showControls,
  setShowControls,
  isImporting,
  importProgress,
  importStatus,
  fileError,
  onSave,
  onCancel,
  slidesCount,
  hasPresentation,
  isImageSlideshow,
  slides,
  setSlides,
  onImportPresentation,
  onImportImages,
}: PresentationSettingsProps) {
  const [activeTab, setActiveTab] = useState("assembly")
  const [selectedMode, setSelectedMode] = useState<"import" | "create" | null>(null)
  const [showImportDialog, setShowImportDialog] = useState(false)
  const [showImageDialog, setShowImageDialog] = useState(false)
  const [localSlides, setLocalSlides] = useState<Slide[]>([])
  const [mediaItems, setMediaItems] = useState<MediaUploadResult[]>([])
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
  const [deleteSlideId, setDeleteSlideId] = useState<string | null>(null)
  const [slideToDelete, setSlideToDelete] = useState<Slide | null>(null)
  const [editSlideId, setEditSlideId] = useState<string | null>(null)
  const [slideToEdit, setSlideToEdit] = useState<Slide | null>(null)
  const [draggedItem, setDraggedItem] = useState<{ type: "media" | "slide"; id: string } | null>(null)
  const [dragOverSlideIndex, setDragOverSlideIndex] = useState<number | null>(null)

  useEffect(() => {
    setLocalSlides(slides)

    if (slides.length > 0) {
      if (hasPresentation) {
        setSelectedMode("import")
      } else if (isImageSlideshow) {
        setSelectedMode("create")
      }
    }
  }, [slides, hasPresentation, isImageSlideshow])

  const handleModeSelect = (mode: "import" | "create") => {
    setSelectedMode(mode)
    setActiveTab("content")
  }

  const handleImportPresentation = () => {
    setShowImportDialog(true)
  }

  const handleAddMediaItems = () => {
    setShowImageDialog(true)
  }

  const handleImportPresentationResult = (result: MediaUploadResult) => {
    onImportPresentation(result)
    setShowImportDialog(false)
  }

  const handleImportImagesResult = (results: MediaUploadResult | MediaUploadResult[]) => {
    const resultsArray = Array.isArray(results) ? results : [results]
    const updatedMedia = [...mediaItems, ...resultsArray]
    setMediaItems(updatedMedia)
    setShowImageDialog(false)
  }

  const handleDeleteSlide = (slideId: string) => {
    const slide = localSlides.find((s) => s.id === slideId)
    if (slide) {
      setSlideToDelete(slide)
      setDeleteSlideId(slideId)
    }
  }

  const confirmDeleteSlide = () => {
    if (deleteSlideId) {
      const updatedSlides = localSlides.filter((slide) => slide.id !== deleteSlideId)
      setLocalSlides(updatedSlides)
      setSlides(updatedSlides)
      setDeleteSlideId(null)
      setSlideToDelete(null)
    }
  }

  const handleMoveSlide = (slideId: string, direction: "up" | "down") => {
    const currentIndex = localSlides.findIndex((slide) => slide.id === slideId)
    if (currentIndex === -1) return

    const newIndex = direction === "up" ? currentIndex - 1 : currentIndex + 1
    if (newIndex < 0 || newIndex >= localSlides.length) return

    const updatedSlides = [...localSlides]
    const [movedSlide] = updatedSlides.splice(currentIndex, 1)
    updatedSlides.splice(newIndex, 0, movedSlide)

    setLocalSlides(updatedSlides)
    setSlides(updatedSlides)
  }

  const handleEditSlide = (slideId: string) => {
    const slide = localSlides.find((s) => s.id === slideId)
    if (slide) {
      setSlideToEdit(slide)
      setEditSlideId(slideId)
    }
  }

  const handleSaveSlideEdit = (editedSlide: Slide) => {
    const updatedSlides = localSlides.map((slide) => (slide.id === editedSlide.id ? editedSlide : slide))
    setLocalSlides(updatedSlides)
    setSlides(updatedSlides)
    setEditSlideId(null)
    setSlideToEdit(null)
  }

  const getThumbnailStyle = (slide: Slide) => {
    const hasCustomImageSettings = slide.filters || slide.imageSize

    if (slide.theme === "image" && slide.backgroundImage) {
      if (hasCustomImageSettings) {
        const filters = slide.filters || {
          brightness: 100,
          contrast: 100,
          saturation: 100,
          blur: 0,
          hueRotate: 0,
          opacity: 100,
        }

        const imageSize = slide.imageSize || {
          width: 100,
          height: 100,
          objectFit: "cover" as const,
        }

        return {
          backgroundImage: `url(${slide.backgroundImage})`,
          backgroundSize: imageSize.objectFit === "cover" ? "cover" : "contain",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
          filter: `brightness(${filters.brightness}%) contrast(${filters.contrast}%) saturate(${filters.saturation}%) blur(${Math.max(0, filters.blur * 0.5)}px) hue-rotate(${filters.hueRotate}deg) opacity(${filters.opacity}%)`,
        }
      } else {
        return {
          backgroundImage: `url(${slide.backgroundImage})`,
          backgroundSize: "cover",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
        }
      }
    }

    switch (slide.theme) {
      case "light":
        return { backgroundColor: "white" }
      case "dark":
        return { backgroundColor: "#1a1a1a" }
      case "standard":
        return { backgroundColor: "#f8f9fa" }
      case "gradient":
        return { background: "linear-gradient(135deg, #6366f1, #8b5cf6)" }
      case "custom":
        return { backgroundColor: "#3b82f6" }
      default:
        return { backgroundColor: "white" }
    }
  }

  const isContentTabDisabled = !selectedMode
  const isSettingsTabDisabled = !selectedMode || localSlides.length === 0

  const handleDragStart = (e: React.DragEvent, type: "media" | "slide", id: string) => {
    setDraggedItem({ type, id })
    e.dataTransfer.effectAllowed = "move"
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = "move"
  }

  const handleDropToSlides = (e: React.DragEvent, targetIndex?: number) => {
    e.preventDefault()
    if (!draggedItem) return

    if (draggedItem.type === "media") {
      const mediaItem = mediaItems.find((item) => item.name === draggedItem.id)
      if (mediaItem) {
        const newSlide: Slide = {
          id: `slide-${Date.now()}-${Math.random()}`,
          title: mediaItem.name || `Slide ${localSlides.length + 1}`,
          content: "",
          layout: "full-image" as const,
          theme: "image" as const,
          backgroundImage: mediaItem.data,
          notes: "",
        }

        const updatedSlides = [...localSlides]
        if (targetIndex !== undefined) {
          updatedSlides.splice(targetIndex, 0, newSlide)
        } else {
          updatedSlides.push(newSlide)
        }

        const updatedMedia = mediaItems.filter((item) => item.name !== draggedItem.id)

        setLocalSlides(updatedSlides)
        setSlides(updatedSlides)
        setMediaItems(updatedMedia)
      }
    } else if (draggedItem.type === "slide") {
      // Handle slide reordering
      const draggedSlideIndex = localSlides.findIndex((s) => s.id === draggedItem.id)
      if (draggedSlideIndex !== -1 && targetIndex !== undefined && targetIndex !== draggedSlideIndex) {
        const updatedSlides = [...localSlides]
        const [draggedSlide] = updatedSlides.splice(draggedSlideIndex, 1)
        const insertIndex = targetIndex > draggedSlideIndex ? targetIndex - 1 : targetIndex
        updatedSlides.splice(insertIndex, 0, draggedSlide)

        setLocalSlides(updatedSlides)
        setSlides(updatedSlides)
      }
    }
    setDraggedItem(null)
    setDragOverSlideIndex(null)
  }

  const handleDropToMedia = (e: React.DragEvent) => {
    e.preventDefault()
    if (!draggedItem) return

    if (draggedItem.type === "slide") {
      const slide = localSlides.find((s) => s.id === draggedItem.id)
      if (slide && slide.theme === "image" && slide.backgroundImage) {
        const mediaItem: MediaUploadResult = {
          type: "file",
          data: slide.backgroundImage,
          name: slide.title || "Untitled",
          size: 0,
        }

        const updatedMedia = [...mediaItems, mediaItem]
        const updatedSlides = localSlides.filter((s) => s.id !== draggedItem.id)

        setMediaItems(updatedMedia)
        setLocalSlides(updatedSlides)
        setSlides(updatedSlides)
      }
    }
    setDraggedItem(null)
  }

  const handleMoveAllToSlides = () => {
    const newSlides: Slide[] = mediaItems.map((item, index) => ({
      id: `slide-${Date.now()}-${Math.random()}-${index}`,
      title: item.name || `Slide ${localSlides.length + index + 1}`,
      content: "",
      layout: "full-image" as const,
      theme: "image" as const,
      backgroundImage: item.data,
      notes: "",
    }))

    const updatedSlides = [...localSlides, ...newSlides]
    setLocalSlides(updatedSlides)
    setSlides(updatedSlides)
    setMediaItems([]) // Clear media items after moving all
  }

  return (
    <div className="h-[80vh] flex flex-col space-y-6 p-6 rounded-lg border">
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="assembly" className="flex items-center gap-2">
            <ImageIcon className="h-4 w-4" />
            Media Assembly
          </TabsTrigger>
          <TabsTrigger value="slide-editing" disabled={localSlides.length === 0} className="flex items-center gap-2">
            <Edit className="h-4 w-4" />
            Slide Editing {localSlides.length > 0 && `(${localSlides.length})`}
          </TabsTrigger>
          <TabsTrigger
            value="presentation-editing"
            disabled={localSlides.length === 0}
            className="flex items-center gap-2"
          >
            <Settings className="h-4 w-4" />
            Presentation Settings
          </TabsTrigger>
        </TabsList>

        <TabsContent value="assembly" className="flex-1 flex flex-col space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">Assemble Your Presentation</h3>
            <Button onClick={handleAddMediaItems} className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Add Media
            </Button>
          </div>

          <div className="flex-1 grid grid-cols-2 gap-4 min-h-0">
            <div
              className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-4 flex flex-col"
              onDragOver={handleDragOver}
              onDrop={handleDropToMedia}
            >
              <div className="flex items-center gap-2 mb-4">
                <ImageIcon className="h-5 w-5" />
                <h4 className="font-medium">Media Library</h4>
                <span className="text-sm text-muted-foreground">({mediaItems.length} items)</span>
                {mediaItems.length > 0 && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleMoveAllToSlides}
                    className="ml-auto flex items-center gap-1 bg-transparent"
                  >
                    <ChevronDown className="h-3 w-3" />
                    Move All
                  </Button>
                )}
              </div>

              <div className="flex-1 space-y-2 overflow-y-auto min-h-0">
                {mediaItems.length === 0 ? (
                  <div className="flex-1 flex items-center justify-center text-center text-muted-foreground">
                    <div>
                      <ImageIcon className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p className="text-sm">No media items</p>
                      <p className="text-xs">Add media or drag slides here</p>
                    </div>
                  </div>
                ) : (
                  mediaItems.map((item, index) => (
                    <div
                      key={`${item.name}-${index}`}
                      draggable
                      onDragStart={(e) => handleDragStart(e, "media", item.name || `item-${index}`)}
                      className="flex items-center gap-3 p-3 border rounded-lg cursor-move hover:bg-muted/50 transition-colors"
                    >
                      <GripVertical className="h-4 w-4 text-muted-foreground" />
                      <div
                        className="w-12 h-8 rounded overflow-hidden bg-muted flex-shrink-0"
                        style={{
                          backgroundImage: `url(${item.data})`,
                          backgroundSize: "cover",
                          backgroundPosition: "center",
                        }}
                      />
                      <div className="flex-1 min-w-0">
                        <div className="font-medium text-sm truncate">{item.name || "Untitled"}</div>
                        <div className="text-xs text-muted-foreground">
                          {item.size ? `${Math.round(item.size / 1024)}KB` : "Unknown size"}
                        </div>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setMediaItems(mediaItems.filter((_, i) => i !== index))}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  ))
                )}
              </div>
            </div>

            <div
              className="border-2 border-dashed border-primary/25 rounded-lg p-4 flex flex-col"
              onDragOver={handleDragOver}
              onDrop={(e) => handleDropToSlides(e)}
            >
              <div className="flex items-center gap-2 mb-4">
                <FileText className="h-5 w-5" />
                <h4 className="font-medium">Slide Sequence</h4>
                <span className="text-sm text-muted-foreground">({localSlides.length} slides)</span>
              </div>

              <div className="flex-1 space-y-2 overflow-y-auto min-h-0">
                {localSlides.length === 0 ? (
                  <div className="flex-1 flex items-center justify-center text-center text-muted-foreground">
                    <div>
                      <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p className="text-sm">No slides yet</p>
                      <p className="text-xs">Drag media items here</p>
                    </div>
                  </div>
                ) : (
                  localSlides.map((slide, index) => (
                    <div
                      key={slide.id}
                      draggable
                      onDragStart={(e) => handleDragStart(e, "slide", slide.id)}
                      onDragOver={(e) => {
                        e.preventDefault()
                        setDragOverSlideIndex(index)
                      }}
                      onDragLeave={() => setDragOverSlideIndex(null)}
                      onDrop={(e) => handleDropToSlides(e, index)}
                      className={`flex items-center gap-3 p-3 border rounded-lg cursor-move hover:bg-muted/50 transition-colors ${
                        dragOverSlideIndex === index ? "border-primary bg-primary/5" : ""
                      }`}
                    >
                      <GripVertical className="h-4 w-4 text-muted-foreground" />
                      <div className="text-sm font-medium text-muted-foreground w-6">{index + 1}</div>
                      <div
                        className="w-12 h-8 rounded overflow-hidden relative flex-shrink-0"
                        style={getThumbnailStyle(slide)}
                      >
                        {slide.theme === "image" && slide.backgroundImage && (
                          <div className="absolute inset-0 bg-black/20"></div>
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="font-medium text-sm truncate">{slide.title || `Slide ${index + 1}`}</div>
                        <div className="text-xs text-muted-foreground">
                          {slide.layout} layout
                          {(slide.filters || slide.imageSize) && " • edited"}
                        </div>
                      </div>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleMoveSlide(slide.id, "up")}
                          disabled={index === 0}
                        >
                          <ChevronUp className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleMoveSlide(slide.id, "down")}
                          disabled={index === localSlides.length - 1}
                        >
                          <ChevronDown className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => handleDeleteSlide(slide.id)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>

          {isImporting && (
            <div className="space-y-2">
              <Progress value={importProgress} className="w-full" />
              <p className="text-sm text-muted-foreground text-center">{importStatus}</p>
            </div>
          )}

          {fileError && (
            <Alert variant="destructive">
              <AlertDescription>{fileError}</AlertDescription>
            </Alert>
          )}
        </TabsContent>

        <TabsContent value="slide-editing" className="flex-1 flex flex-col space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">Edit Individual Slides</h3>
            <div className="text-sm text-muted-foreground">
              {localSlides.length} slide{localSlides.length !== 1 ? "s" : ""}
            </div>
          </div>

          <div className="flex-1 grid grid-cols-2 gap-6 min-h-0">
            {/* Left column: Slide Player */}
            <div className="space-y-4">
              <h4 className="font-medium">Preview</h4>
              {localSlides.length > 0 && (
                <div className="border rounded-lg overflow-hidden">
                  <SlidePlayer
                    slides={localSlides}
                    title={title}
                    currentSlideIndex={currentSlideIndex}
                    onSlideChange={setCurrentSlideIndex}
                    autoAdvance={false}
                    autoAdvanceDelay={autoAdvanceDelay}
                    autoAdvanceLoop={autoAdvanceLoop}
                    transitionEffect={transitionEffect}
                    showControls={true}
                    showThumbnails={true}
                    showHeader={true}
                    showFullscreenButton={false}
                    theme="light"
                    customThemeColor={null}
                    size="md"
                    isPresenting={false}
                    isEditing={true}
                  />
                </div>
              )}
            </div>

            {/* Right column: Slide List and Edit Controls */}
            <div className="space-y-4">
              <h4 className="font-medium">Slide List</h4>
              <div className="flex-1 space-y-2 overflow-y-auto min-h-0 border rounded-lg p-4">
                {localSlides.map((slide, index) => (
                  <div
                    key={slide.id}
                    className={`flex items-center gap-3 p-3 border rounded-lg transition-colors ${
                      currentSlideIndex === index ? "bg-primary/10 border-primary" : "hover:bg-muted/50"
                    }`}
                  >
                    <div className="text-sm font-medium text-muted-foreground w-6">{index + 1}</div>
                    <div
                      className="w-12 h-8 rounded overflow-hidden relative flex-shrink-0 cursor-pointer"
                      style={getThumbnailStyle(slide)}
                      onClick={() => setCurrentSlideIndex(index)}
                    >
                      {slide.theme === "image" && slide.backgroundImage && (
                        <div className="absolute inset-0 bg-black/20"></div>
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-sm truncate">{slide.title || `Slide ${index + 1}`}</div>
                      <div className="text-xs text-muted-foreground">
                        {slide.layout} layout
                        {(slide.filters || slide.imageSize) && " • edited"}
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      <Button variant="ghost" size="sm" onClick={() => handleEditSlide(slide.id)}>
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleMoveSlide(slide.id, "up")}
                        disabled={index === 0}
                      >
                        <ChevronUp className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleMoveSlide(slide.id, "down")}
                        disabled={index === localSlides.length - 1}
                      >
                        <ChevronDown className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => handleDeleteSlide(slide.id)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="presentation-editing" className="flex-1 flex flex-col space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">Presentation Settings</h3>
            <div className="text-sm text-muted-foreground">Configure global presentation options</div>
          </div>

          <div className="flex-1 grid grid-cols-2 gap-6 min-h-0">
            {/* Left column: Slide Player */}
            <div className="space-y-4">
              <h4 className="font-medium">Live Preview</h4>
              {localSlides.length > 0 && (
                <div className="border rounded-lg overflow-hidden">
                  <SlidePlayer
                    slides={localSlides}
                    title={title}
                    currentSlideIndex={currentSlideIndex}
                    onSlideChange={setCurrentSlideIndex}
                    autoAdvance={autoAdvance}
                    autoAdvanceDelay={autoAdvanceDelay}
                    autoAdvanceLoop={autoAdvanceLoop}
                    transitionEffect={transitionEffect}
                    showControls={showControls}
                    showThumbnails={true}
                    showHeader={true}
                    showFullscreenButton={true}
                    theme="light"
                    customThemeColor={null}
                    size="md"
                    isPresenting={false}
                    isEditing={false}
                  />
                </div>
              )}
            </div>

            {/* Right column: Presentation Settings */}
            <div className="space-y-4 overflow-y-auto">
              <h4 className="font-medium">Global Settings</h4>

              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="title">Presentation Title</Label>
                  <Input
                    id="title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    placeholder="Enter presentation title"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="transition">Transition Effect</Label>
                  <Select value={transitionEffect} onValueChange={setTransitionEffect}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="fade">Fade</SelectItem>
                      <SelectItem value="slide">Slide</SelectItem>
                      <SelectItem value="zoom">Zoom</SelectItem>
                      <SelectItem value="flip">Flip</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-3">
                  <div className="flex items-center space-x-2">
                    <Checkbox
                      id="auto-advance"
                      checked={autoAdvance}
                      onCheckedChange={(checked) => {
                        setAutoAdvance(checked)
                        setAutoAdvanceLoop(checked)
                      }}
                    />
                    <Label htmlFor="auto-advance">Auto-advance slides</Label>
                  </div>

                  {autoAdvance && (
                    <div className="space-y-2 ml-6">
                      <Label htmlFor="delay">Delay (seconds)</Label>
                      <Input
                        id="delay"
                        type="number"
                        min="1"
                        max="60"
                        value={autoAdvanceDelay}
                        onChange={(e) => setAutoAdvanceDelay(Number(e.target.value))}
                      />
                    </div>
                  )}

                  <div className="flex items-center space-x-2">
                    <Checkbox id="loop" checked={autoAdvanceLoop} onCheckedChange={setAutoAdvanceLoop} />
                    <Label htmlFor="loop">Loop presentation</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Checkbox id="show-controls" checked={showControls} onCheckedChange={setShowControls} />
                    <Label htmlFor="show-controls">Show player controls</Label>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Presentation Summary</Label>
                  <div className="p-3 border rounded-lg bg-muted/50">
                    <div className="text-sm space-y-1">
                      <div>
                        <strong>Slides:</strong> {localSlides.length}
                      </div>
                      <div>
                        <strong>Transition:</strong> {transitionEffect}
                      </div>
                      <div>
                        <strong>Auto-advance:</strong> {autoAdvance ? `Yes (${autoAdvanceDelay}s)` : "No"}
                      </div>
                      <div>
                        <strong>Loop:</strong> {autoAdvanceLoop ? "Yes" : "No"}
                      </div>
                      <div>
                        <strong>Controls:</strong> {showControls ? "Visible" : "Hidden"}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </TabsContent>
      </Tabs>

      <div className="flex justify-end gap-2 pt-4 border-t">
        <Button variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button onClick={onSave} disabled={localSlides.length === 0}>
          Save Presentation
        </Button>
      </div>

      <DeleteConfirmDialog
        open={deleteSlideId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteSlideId(null)
            setSlideToDelete(null)
          }
        }}
        title="Excluir Slide"
        itemName={slideToDelete?.title || `Slide ${localSlides.findIndex((s) => s.id === deleteSlideId) + 1}`}
        itemType="slide"
        onConfirm={confirmDeleteSlide}
        confirmText="Excluir Slide"
        cancelText="Cancelar"
      />

      <div className="z-[30]">
        <MediaUploadDialog
          open={showImportDialog}
          onOpenChange={setShowImportDialog}
          onMediaSelected={handleImportPresentationResult}
          title="Import Presentation"
          mode={0}
          acceptTypes=".pptx,.odp,.json,application/vnd.openxmlformats-officedocument.presentationml.presentation,application/vnd.oasis.opendocument.presentation,application/json"
          urlPlaceholder="https://example.com/presentation.pptx"
          uploadLabel="Select a presentation file"
          urlLabel="Enter presentation URL"
          maxSizeKB={51200}
        />
      </div>

      <div className="z-[30]">
        <MediaUploadDialog
          open={showImageDialog}
          onOpenChange={setShowImageDialog}
          onMediaSelected={handleImportImagesResult}
          title="Add Media Items"
          mode={0}
          acceptTypes="image/*"
          urlPlaceholder="https://example.com/image.png"
          uploadLabel="Select media files"
          urlLabel="Enter media URL"
          maxSizeKB={5120}
          multiple={true}
        />
      </div>

      <div className="z-[10]">
        <SlideEditDialog
          open={editSlideId !== null}
          onOpenChange={(open) => {
            if (!open) {
              setEditSlideId(null)
              setSlideToEdit(null)
            }
          }}
          slide={slideToEdit}
          onSave={handleSaveSlideEdit}
        />
      </div>
    </div>
  )
}
