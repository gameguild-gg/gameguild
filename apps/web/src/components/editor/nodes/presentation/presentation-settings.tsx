"use client"

import { useState, useEffect } from "react"
import { Upload, Plus, Trash2, ChevronUp, ChevronDown, Settings, FileText, ImageIcon, Edit } from 'lucide-react'
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Progress } from "@/components/ui/progress"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { MediaUploadDialog, type MediaUploadResult } from "@/components/editor/ui/media-upload-dialog"
import { SlidePlayer } from "@/components/editor/ui/slide-player"
import type { Slide, SlideTheme, TransitionEffect } from "./types"
import { DeleteConfirmDialog } from "@/components/editor/ui/delete-confirm-dialog"
import { SlideEditDialog } from "@/components/editor/ui/slide-edit-dialog"

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
  const [activeTab, setActiveTab] = useState("mode")
  const [selectedMode, setSelectedMode] = useState<"import" | "create" | null>(null)
  const [showImportDialog, setShowImportDialog] = useState(false)
  const [showImageDialog, setShowImageDialog] = useState(false)
  const [localSlides, setLocalSlides] = useState<Slide[]>([])
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
  const [deleteSlideId, setDeleteSlideId] = useState<string | null>(null)
  const [slideToDelete, setSlideToDelete] = useState<Slide | null>(null)
  const [editSlideId, setEditSlideId] = useState<string | null>(null)
  const [slideToEdit, setSlideToEdit] = useState<Slide | null>(null)

  // Sincronizar slides locais com os slides da prop
  useEffect(() => {
    // Verificar se slides existe e é um array
    if (slides && Array.isArray(slides)) {
      setLocalSlides(slides)
      
      // Detectar modo baseado nos dados existentes
      if (slides.length > 0) {
        if (hasPresentation) {
          setSelectedMode("import")
        } else if (isImageSlideshow) {
          setSelectedMode("create")
        }
      }
    } else {
      // Se slides não existe ou não é um array, inicializar como array vazio
      setLocalSlides([])
    }
  }, [slides, hasPresentation, isImageSlideshow])

  const handleModeSelect = (mode: "import" | "create") => {
    setSelectedMode(mode)
    setActiveTab("content")
  }

  const handleImportPresentation = () => {
    setShowImportDialog(true)
  }

  const handleAddImages = () => {
    setShowImageDialog(true)
  }

  const handleImportPresentationResult = (result: MediaUploadResult) => {
    onImportPresentation(result)
    setShowImportDialog(false)
  }

  const handleImportImagesResult = (results: MediaUploadResult | MediaUploadResult[]) => {
    // Garantir que sempre temos um array
    const resultsArray = Array.isArray(results) ? results : [results]
    
    const newSlides: Slide[] = resultsArray.map((result, index) => ({
      id: `image-slide-${Date.now()}-${index}`,
      title: `Slide ${localSlides.length + index + 1}`,
      content: "",
      layout: "full-image" as const,
      theme: "image" as const,
      backgroundImage: result.data,
      notes: "",
    }))

    const updatedSlides = [...localSlides, ...newSlides]
    setLocalSlides(updatedSlides)
    setSlides(updatedSlides)
    setShowImageDialog(false)
  }

  const handleDeleteSlide = (slideId: string) => {
    const slide = localSlides.find(s => s.id === slideId)
    if (slide) {
      setSlideToDelete(slide)
      setDeleteSlideId(slideId)
    }
  }

  const confirmDeleteSlide = () => {
    if (deleteSlideId) {
      const updatedSlides = localSlides.filter(slide => slide.id !== deleteSlideId)
      setLocalSlides(updatedSlides)
      setSlides(updatedSlides)
      setDeleteSlideId(null)
      setSlideToDelete(null)
    }
  }

  const handleMoveSlide = (slideId: string, direction: "up" | "down") => {
    const currentIndex = localSlides.findIndex(slide => slide.id === slideId)
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
    const slide = localSlides.find(s => s.id === slideId)
    if (slide) {
      setSlideToEdit(slide)
      setEditSlideId(slideId)
    }
  }

  const handleSaveSlideEdit = (editedSlide: Slide) => {
    const updatedSlides = localSlides.map(slide => 
      slide.id === editedSlide.id ? editedSlide : slide
    )
    setLocalSlides(updatedSlides)
    setSlides(updatedSlides)
    setEditSlideId(null)
    setSlideToEdit(null)
  }

  // Get thumbnail style for slide with custom settings
  const getThumbnailStyle = (slide: Slide) => {
    const hasCustomImageSettings = slide.filters || slide.imageSize
    
    if (slide.theme === "image" && slide.backgroundImage) {
      if (hasCustomImageSettings) {
        // Apply custom filters and sizing to thumbnail
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
        // Default background image style
        return {
          backgroundImage: `url(${slide.backgroundImage})`,
          backgroundSize: "cover",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
        }
      }
    }
    
    // Default solid background based on theme
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

  return (
    <div className="space-y-6 p-6 rounded-lg border">
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="mode" className="flex items-center gap-2">
            <FileText className="h-4 w-4" />
            Mode {localSlides.length > 0 && `(${localSlides.length} slides)`}
          </TabsTrigger>
          <TabsTrigger value="content" disabled={isContentTabDisabled} className="flex items-center gap-2">
            <ImageIcon className="h-4 w-4" />
            Content
          </TabsTrigger>
          <TabsTrigger value="settings" disabled={isSettingsTabDisabled} className="flex items-center gap-2">
            <Settings className="h-4 w-4" />
            Settings
          </TabsTrigger>
        </TabsList>

        <TabsContent value="mode" className="space-y-4">
          <div className="text-center space-y-4">
            <h3 className="text-lg font-semibold">Choose Presentation Mode</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Button
                variant={selectedMode === "import" ? "default" : "outline"}
                className="h-24 flex flex-col items-center justify-center gap-2"
                onClick={() => handleModeSelect("import")}
              >
                <Upload className="h-6 w-6" />
                <div>
                  <div className="font-medium">Import Presentation</div>
                  <div className="text-xs text-muted-foreground">Upload .pptx, .odp, or .json files</div>
                </div>
              </Button>
              <Button
                variant={selectedMode === "create" ? "default" : "outline"}
                className="h-24 flex flex-col items-center justify-center gap-2"
                onClick={() => handleModeSelect("create")}
              >
                <Plus className="h-6 w-6" />
                <div>
                  <div className="font-medium">Create Presentation</div>
                  <div className="text-xs text-muted-foreground">Build slideshow from images</div>
                </div>
              </Button>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="content" className="space-y-4">
          {selectedMode === "import" && (
            <div className="space-y-4">
              <div className="text-center">
                <h3 className="text-lg font-semibold mb-4">Import Presentation</h3>
                {localSlides.length === 0 ? (
                  <Button onClick={handleImportPresentation} className="flex items-center gap-2">
                    <Upload className="h-4 w-4" />
                    Import Presentation File
                  </Button>
                ) : (
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">
                        {localSlides.length} slides imported
                      </span>
                      <Button variant="outline" size="sm" onClick={handleImportPresentation}>
                        <Upload className="h-4 w-4 mr-2" />
                        Import Another
                      </Button>
                    </div>
                    
                    <div className="space-y-2 max-h-60 overflow-y-auto">
                      {localSlides.map((slide, index) => (
                        <div key={slide.id} className="flex items-center gap-3 p-3 border rounded-lg">
                          <div 
                            className="w-16 h-12 rounded flex items-center justify-center text-xs overflow-hidden relative"
                            style={getThumbnailStyle(slide)}
                          >
                            {slide.theme === "image" && slide.backgroundImage && (
                              <div className="absolute inset-0 bg-black/20"></div>
                            )}
                            <span className="relative z-10 text-white font-medium drop-shadow-sm">
                              {index + 1}
                            </span>
                          </div>
                          <div className="flex-1">
                            <div className="font-medium">{slide.title || `Slide ${index + 1}`}</div>
                            <div className="text-xs text-muted-foreground">
                              {slide.layout} layout
                              {(slide.filters || slide.imageSize) && " • edited"}
                            </div>
                          </div>
                          <div className="flex items-center gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleEditSlide(slide.id)}
                              title="Edit slide"
                            >
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
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleDeleteSlide(slide.id)}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {selectedMode === "create" && (
            <div className="space-y-4">
              <div className="text-center">
                <h3 className="text-lg font-semibold mb-4">Create Image Slideshow</h3>
                <Button onClick={handleAddImages} className="flex items-center gap-2">
                  <Plus className="h-4 w-4" />
                  Add Images
                </Button>
              </div>

              {localSlides.length > 0 && (
                <div className="space-y-2 max-h-60 overflow-y-auto">
                  {localSlides.map((slide, index) => (
                    <div key={slide.id} className="flex items-center gap-3 p-3 border rounded-lg">
                      <div 
                        className="w-16 h-12 rounded overflow-hidden relative"
                        style={getThumbnailStyle(slide)}
                      >
                        {!slide.backgroundImage && (
                          <div className="w-full h-full flex items-center justify-center text-xs bg-muted">
                            {index + 1}
                          </div>
                        )}
                        {slide.theme === "image" && slide.backgroundImage && (
                          <div className="absolute inset-0 bg-black/20 flex items-center justify-center">
                            <span className="text-white font-medium text-xs drop-shadow-sm">
                              {index + 1}
                            </span>
                          </div>
                        )}
                      </div>
                      <div className="flex-1">
                        <div className="font-medium">{slide.title}</div>
                        <div className="text-xs text-muted-foreground">
                          Image slide
                          {(slide.filters || slide.imageSize) && " • edited"}
                        </div>
                      </div>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleEditSlide(slide.id)}
                          title="Edit slide"
                        >
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
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteSlide(slide.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

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

        <TabsContent value="settings" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Presentation Settings</h3>
              
              <div className="space-y-2">
                <Label htmlFor="title">Title</Label>
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

              <div className="flex items-center space-x-2">
                <Checkbox
                  id="auto-advance"
                  checked={autoAdvance}
                  onCheckedChange={(checked) => {
                    setAutoAdvance(checked);
                    setAutoAdvanceLoop(checked); // Automaticamente marca/desmarca o loop
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

              {/* Este é o novo local para o switch de loop, fora do condicional autoAdvance */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="loop"
                  checked={autoAdvanceLoop}
                  onCheckedChange={setAutoAdvanceLoop}
                />
                <Label htmlFor="loop">Loop presentation</Label>
              </div>

              <div className="flex items-center space-x-2">
                <Checkbox
                  id="show-controls"
                  checked={showControls}
                  onCheckedChange={setShowControls}
                />
                <Label htmlFor="show-controls">Show player controls</Label>
              </div>
            </div>

            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Preview</h3>
              {localSlides.length > 0 && (
                <div className="border rounded-lg overflow-hidden">
                  <SlidePlayer
                    slides={localSlides}
                    title={title}
                    currentSlideIndex={currentSlideIndex}
                    onSlideChange={setCurrentSlideIndex}
                    autoAdvance={false} // Disable auto-advance in preview
                    autoAdvanceDelay={autoAdvanceDelay}
                    autoAdvanceLoop={autoAdvanceLoop}
                    transitionEffect={transitionEffect}
                    showControls={showControls}
                    showThumbnails={false}
                    showHeader={false}
                    showFullscreenButton={false}
                    theme="light"
                    customThemeColor={null}
                    size="sm"
                    isPresenting={false}
                    isEditing={false}
                  />
                </div>
              )}
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
        itemName={slideToDelete?.title || `Slide ${localSlides.findIndex(s => s.id === deleteSlideId) + 1}`}
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
        title="Add Images to Slideshow"
        mode={0}
        acceptTypes="image/*"
        urlPlaceholder="https://example.com/image.png"
        uploadLabel="Select images for your slideshow"
        urlLabel="Enter image URL"
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