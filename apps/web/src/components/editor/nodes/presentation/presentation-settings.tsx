"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Progress } from "@/components/ui/progress"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { AlertCircle, FileText, Images, Save, X } from "lucide-react"
import { MediaUploadDialog, type MediaUploadResult } from "@/components/editor/ui/media-upload-dialog"
import type { TransitionEffect, Slide } from "./types"

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
  setSlides: (slides: Slide[]) => void
  onImportPresentation: (result: MediaUploadResult) => void
  onImportImages: (results: MediaUploadResult[]) => void
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
  setSlides,
  onImportPresentation,
  onImportImages,
}: PresentationSettingsProps) {
  const [showPresentationDialog, setShowPresentationDialog] = useState(false)
  const [showImagesDialog, setShowImagesDialog] = useState(false)

  const handlePresentationSelected = (result: MediaUploadResult) => {
    setShowPresentationDialog(false)
    onImportPresentation(result)
  }

  const handleImagesSelected = (results: MediaUploadResult | MediaUploadResult[]) => {
    setShowImagesDialog(false)
    const resultsArray = Array.isArray(results) ? results : [results]
    onImportImages(resultsArray)
  }

  const createEmptySlide = () => {
    const newSlide: Slide = {
      id: `slide-${Date.now()}`,
      title: "New Slide",
      content: "Click to edit content",
      layout: "title-content",
      theme: "light",
      notes: "",
    }
    setSlides([newSlide])
  }

  return (
    <div className="my-8">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            Presentation Settings
            <div className="flex gap-2">
              <Button onClick={onSave} size="sm">
                <Save className="h-4 w-4 mr-2" />
                Save
              </Button>
              <Button onClick={onCancel} variant="outline" size="sm">
                <X className="h-4 w-4 mr-2" />
                Cancel
              </Button>
            </div>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isImporting && (
            <div className="mb-6 space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Importing...</span>
                <span className="text-sm text-muted-foreground">{importProgress}%</span>
              </div>
              <Progress value={importProgress} className="w-full" />
              <p className="text-xs text-muted-foreground">{importStatus}</p>
            </div>
          )}

          {fileError && (
            <Alert variant="destructive" className="mb-6">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{fileError}</AlertDescription>
            </Alert>
          )}

          <Tabs defaultValue={slidesCount === 0 ? "content" : "settings"} className="w-full">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="content">Content</TabsTrigger>
              <TabsTrigger value="settings" disabled={slidesCount === 0}>
                Settings
              </TabsTrigger>
            </TabsList>

            <TabsContent value="content" className="space-y-6">
              <div className="space-y-4">
                <h3 className="text-lg font-semibold">Add Content</h3>
                <p className="text-sm text-muted-foreground">Choose how you want to create your presentation</p>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Card
                    className="cursor-pointer hover:bg-muted/50 transition-colors"
                    onClick={() => setShowPresentationDialog(true)}
                  >
                    <CardContent className="p-6 text-center">
                      <FileText className="h-8 w-8 mx-auto mb-2 text-primary" />
                      <h4 className="font-medium mb-1">Import Presentation</h4>
                      <p className="text-xs text-muted-foreground">Upload .pptx, .odp, or .json files</p>
                    </CardContent>
                  </Card>

                  <Card
                    className="cursor-pointer hover:bg-muted/50 transition-colors"
                    onClick={() => setShowImagesDialog(true)}
                  >
                    <CardContent className="p-6 text-center">
                      <Images className="h-8 w-8 mx-auto mb-2 text-primary" />
                      <h4 className="font-medium mb-1">Create from Images</h4>
                      <p className="text-xs text-muted-foreground">Upload multiple images as slides</p>
                    </CardContent>
                  </Card>
                </div>

                {slidesCount > 0 && (
                  <div className="mt-6 p-4 bg-muted/20 rounded-lg">
                    <p className="text-sm">
                      <strong>{slidesCount}</strong> slide{slidesCount !== 1 ? "s" : ""} loaded
                      {hasPresentation && " (Presentation)"}
                      {isImageSlideshow && " (Image Slideshow)"}
                    </p>
                  </div>
                )}
              </div>
            </TabsContent>

            <TabsContent value="settings" className="space-y-6">
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

                {isImageSlideshow && (
                  <div className="space-y-2">
                    <Label htmlFor="transition">Transition Effect</Label>
                    <Select
                      value={transitionEffect}
                      onValueChange={(value: TransitionEffect) => setTransitionEffect(value)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="none">None</SelectItem>
                        <SelectItem value="fade">Fade</SelectItem>
                        <SelectItem value="slide">Slide</SelectItem>
                        <SelectItem value="zoom">Zoom</SelectItem>
                        <SelectItem value="flip">Flip</SelectItem>
                        <SelectItem value="cube">Cube</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                )}

                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label htmlFor="auto-advance">Auto Advance</Label>
                    <p className="text-xs text-muted-foreground">Automatically advance to next slide</p>
                  </div>
                  <Switch id="auto-advance" checked={autoAdvance} onCheckedChange={setAutoAdvance} />
                </div>

                {autoAdvance && (
                  <div className="space-y-2">
                    <Label htmlFor="delay">Auto Advance Delay (seconds)</Label>
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

                {/* Este bloco foi movido para fora do condicional autoAdvance */}
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label htmlFor="auto-loop">Loop Presentation</Label>
                    <p className="text-xs text-muted-foreground">Return to first slide after last slide</p>
                  </div>
                  <Switch id="auto-loop" checked={autoAdvanceLoop} onCheckedChange={setAutoAdvanceLoop} />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label htmlFor="show-controls">Show Controls</Label>
                    <p className="text-xs text-muted-foreground">Display navigation controls</p>
                  </div>
                  <Switch id="show-controls" checked={showControls} onCheckedChange={setShowControls} />
                </div>
              </div>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      {/* Media Upload Dialogs */}
      <MediaUploadDialog
        open={showPresentationDialog}
        onOpenChange={setShowPresentationDialog}
        onMediaSelected={handlePresentationSelected}
        title="Import Presentation"
        acceptTypes=".json,.pptx,.odp"
        urlPlaceholder="https://example.com/presentation.pptx"
        uploadLabel="Select a presentation file"
        urlLabel="Enter the URL of the presentation file"
        maxSizeKB={70000} // 50MB limit for presentations
      />

      <MediaUploadDialog
        open={showImagesDialog}
        onOpenChange={setShowImagesDialog}
        onMediaSelected={handleImagesSelected}
        title="Import Images for Slideshow"
        acceptTypes="image/*"
        urlPlaceholder="https://example.com/image.jpg"
        uploadLabel="Select images for your slideshow"
        urlLabel="Enter the URL of an image"
        multiple={true}
        maxSizeKB={10000} // 10MB limit per image
      />
    </div>
  )
}
