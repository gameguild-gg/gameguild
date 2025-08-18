"use client"

import { useState, useEffect, useCallback } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Slider } from "@/components/ui/slider"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { WebPConverter, type WebPConversionOptions, WEBP_PRESETS } from "@/lib/editor/webp-converter"
import { Settings, ImageIcon, Zap, Palette } from "lucide-react"

export interface CompressionSettings extends WebPConversionOptions {
  applyToAll: boolean
  preset?: string
}

interface CompressionSettingsDialogProps {
  isOpen: boolean
  onClose: () => void
  file: File
  onConfirm: (settings: CompressionSettings) => void
  onCancel: () => void
  remainingCount?: number
}

export function CompressionSettingsDialog({
  isOpen,
  onClose,
  file,
  onConfirm,
  onCancel,
  remainingCount = 0,
}: CompressionSettingsDialogProps) {
  const [originalDimensions, setOriginalDimensions] = useState<{ width: number; height: number } | null>(null)
  const [scaleMultiplier, setScaleMultiplier] = useState(100) // 100% = original size

  const [settings, setSettings] = useState<CompressionSettings>({
    quality: WebPConverter.getOptimalQuality(file.size),
    maxWidth: 1920,
    maxHeight: 1080,
    maintainAspectRatio: true,
    lossless: false,
    applyToAll: false,
  })

  const [previewData, setPreviewData] = useState<{
    original: string
    compressed?: string
    originalSize: number
    compressedSize?: number
    compressionRatio?: number
  }>({
    original: "",
    originalSize: file.size,
  })

  const [isGeneratingPreview, setIsGeneratingPreview] = useState(false)

  // Generate original preview
  useEffect(() => {
    const img = new Image()
    const objectUrl = URL.createObjectURL(file)

    img.onload = () => {
      setOriginalDimensions({ width: img.naturalWidth, height: img.naturalHeight })
      // Set initial settings based on original dimensions
      setSettings((prev) => ({
        ...prev,
        maxWidth: img.naturalWidth,
        maxHeight: img.naturalHeight,
      }))
      URL.revokeObjectURL(objectUrl)
    }

    img.src = objectUrl
    setPreviewData((prev) => ({ ...prev, original: objectUrl }))

    return () => URL.revokeObjectURL(objectUrl)
  }, [file])

  // Generate compressed preview when settings change
  const generatePreview = useCallback(async () => {
    setIsGeneratingPreview(true)
    try {
      const result = await WebPConverter.convertToWebP(file, settings)
      if (result.success && result.dataUrl) {
        setPreviewData((prev) => ({
          ...prev,
          compressed: result.dataUrl,
          compressedSize: result.compressedSize,
          compressionRatio: result.compressionRatio,
        }))
      }
    } catch (error) {
      console.error("Preview generation failed:", error)
    } finally {
      setIsGeneratingPreview(false)
    }
  }, [file, settings])

  useEffect(() => {
    const debounceTimer = setTimeout(generatePreview, 500)
    return () => clearTimeout(debounceTimer)
  }, [generatePreview])

  const handlePresetChange = (preset: string) => {
    if (preset === "custom") {
      setSettings((prev) => ({ ...prev, preset }))
      return
    }

    const presetConfig = WEBP_PRESETS[preset as keyof typeof WEBP_PRESETS]
    if (presetConfig) {
      setSettings((prev) => ({ ...prev, ...presetConfig, preset }))
    }
  }

  const handleQualityChange = (value: number[]) => {
    setSettings((prev) => ({ ...prev, quality: value[0] / 100, preset: "custom" }))
  }

  const handleScaleChange = (value: number[]) => {
    const newScale = value[0]
    setScaleMultiplier(newScale)

    if (originalDimensions) {
      const newWidth = Math.round((originalDimensions.width * newScale) / 100)
      const newHeight = Math.round((originalDimensions.height * newScale) / 100)

      setSettings((prev) => ({
        ...prev,
        maxWidth: newWidth,
        maxHeight: newHeight,
        preset: "custom",
      }))
    }
  }

  const getFinalDimensions = () => {
    if (!originalDimensions) return null

    const { maxWidth = originalDimensions.width, maxHeight = originalDimensions.height } = settings

    if (!settings.maintainAspectRatio) {
      return { width: maxWidth, height: maxHeight }
    }

    const aspectRatio = originalDimensions.width / originalDimensions.height

    let finalWidth = maxWidth
    let finalHeight = maxHeight

    if (finalWidth / aspectRatio > finalHeight) {
      finalWidth = Math.round(finalHeight * aspectRatio)
    } else {
      finalHeight = Math.round(finalWidth / aspectRatio)
    }

    return { width: finalWidth, height: finalHeight }
  }

  const finalDimensions = getFinalDimensions()

  const handleConfirm = () => {
    onConfirm(settings)
    onClose()
  }

  const handleCancel = () => {
    onCancel()
    onClose()
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="min-w-[65vw] max-h-[95vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            Compression Settings
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Preview Panel */}
          <div className="space-y-4">
            <Label className="text-lg font-semibold">Preview Comparison</Label>

            <div className="grid grid-cols-2 gap-6 min-w-0">
              {/* Original */}
              <div className="space-y-3 min-w-0">
                <div className="flex items-center justify-between">
                  <Label className="text-base font-medium">Original</Label>
                  <div className="flex gap-2">
                    <Badge variant="outline">{WebPConverter.formatFileSize(previewData.originalSize)}</Badge>
                    {originalDimensions && (
                      <Badge variant="secondary">
                        {originalDimensions.width} × {originalDimensions.height}
                      </Badge>
                    )}
                  </div>
                </div>
                <div className="relative aspect-video bg-muted rounded-lg overflow-hidden min-h-[300px]">
                  <img
                    src={previewData.original || "/placeholder.svg"}
                    alt="Original"
                    className="w-full h-full object-contain"
                  />
                </div>
              </div>

              {/* Compressed */}
              <div className="space-y-3 min-w-0">
                <div className="flex items-center justify-between">
                  <Label className="text-base font-medium">Compressed</Label>
                  <div className="flex gap-2">
                    {previewData.compressedSize && (
                      <Badge variant="outline">{WebPConverter.formatFileSize(previewData.compressedSize)}</Badge>
                    )}
                    {finalDimensions && (
                      <Badge variant="secondary">
                        {finalDimensions.width} × {finalDimensions.height}
                      </Badge>
                    )}
                    {previewData.compressionRatio && previewData.compressionRatio > 0 && (
                      <Badge variant="secondary">-{Math.round(previewData.compressionRatio)}%</Badge>
                    )}
                  </div>
                </div>
                <div className="relative aspect-video bg-muted rounded-lg overflow-hidden min-h-[300px]">
                  {isGeneratingPreview ? (
                    <div className="flex items-center justify-center h-full">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
                    </div>
                  ) : previewData.compressed ? (
                    <img
                      src={previewData.compressed || "/placeholder.svg"}
                      alt="Compressed"
                      className="w-full h-full object-contain"
                    />
                  ) : (
                    <div className="flex items-center justify-center h-full text-muted-foreground">
                      Generating preview...
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          <Separator />

          {/* Settings Panel */}
          <div className="space-y-6">
            <Label className="text-lg font-semibold">Compression Settings</Label>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <div>
                <Label className="text-base font-medium">Preset</Label>
                <Select value={settings.preset || "custom"} onValueChange={handlePresetChange}>
                  <SelectTrigger className="mt-2">
                    <SelectValue placeholder="Choose preset" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="HIGH_QUALITY">High Quality</SelectItem>
                    <SelectItem value="BALANCED">Balanced</SelectItem>
                    <SelectItem value="COMPRESSED">Compressed</SelectItem>
                    <SelectItem value="THUMBNAIL">Thumbnail</SelectItem>
                    <SelectItem value="custom">Custom</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div>
                <div className="flex items-center justify-between mb-3">
                  <Label className="text-base font-medium flex items-center gap-2">
                    <Zap className="h-4 w-4" />
                    Quality
                  </Label>
                  <Badge variant="secondary">{Math.round(settings.quality * 100)}%</Badge>
                </div>
                <Slider
                  value={[settings.quality * 100]}
                  onValueChange={handleQualityChange}
                  max={100}
                  min={10}
                  step={5}
                  className="mt-2"
                />
                <div className="flex justify-between text-xs text-muted-foreground mt-1">
                  <span>Smaller file</span>
                  <span>Better quality</span>
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-3">
                  <Label className="text-base font-medium flex items-center gap-2">
                    <ImageIcon className="h-4 w-4" />
                    Scale
                  </Label>
                  <Badge variant="secondary">{scaleMultiplier}%</Badge>
                </div>
                <Slider
                  value={[scaleMultiplier]}
                  onValueChange={handleScaleChange}
                  max={100}
                  min={10}
                  step={1}
                  className="mt-2"
                />
                <div className="flex justify-between text-xs text-muted-foreground mt-1">
                  <span>10% (smaller)</span>
                  <span>100% (original)</span>
                </div>
                {finalDimensions && (
                  <div className="text-xs text-muted-foreground mt-2 text-center">
                    Final: {finalDimensions.width} × {finalDimensions.height}px
                  </div>
                )}
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <div className="flex items-center justify-between">
                <Label className="text-base font-medium">Maintain Aspect Ratio</Label>
                <Switch
                  checked={settings.maintainAspectRatio}
                  onCheckedChange={(checked) =>
                    setSettings((prev) => ({ ...prev, maintainAspectRatio: checked, preset: "custom" }))
                  }
                />
              </div>

              <div className="flex items-center justify-between">
                <Label className="text-base font-medium flex items-center gap-2">
                  <Palette className="h-4 w-4" />
                  Lossless Compression
                </Label>
                <Switch
                  checked={settings.lossless}
                  onCheckedChange={(checked) =>
                    setSettings((prev) => ({ ...prev, lossless: checked, preset: "custom" }))
                  }
                />
              </div>

              {remainingCount > 0 && (
                <div className="flex items-center justify-between">
                  <div>
                    <Label className="text-base font-medium">Apply to All</Label>
                    <p className="text-sm text-muted-foreground">Apply to {remainingCount} remaining images</p>
                  </div>
                  <Switch
                    checked={settings.applyToAll}
                    onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, applyToAll: checked }))}
                  />
                </div>
              )}
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button onClick={handleConfirm}>
            {settings.applyToAll ? `Apply to All (${remainingCount + 1})` : "Apply"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
