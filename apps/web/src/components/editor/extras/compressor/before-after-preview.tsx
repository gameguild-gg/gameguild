"use client"

import React from "react"

import { useState, useRef, useCallback } from "react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Slider } from "@/components/ui/slider"
import { Card, CardContent } from "@/components/ui/card"
import { WebPConverter } from "@/lib/editor/webp-converter"
import {
  Eye,
  EyeOff,
  ZoomIn,
  ZoomOut,
  RotateCcw,
  Move3D,
  SplitSquareHorizontal,
  ToggleLeft,
  ToggleRight,
} from "lucide-react"

export interface PreviewData {
  original: {
    url: string
    size: number
    name: string
  }
  compressed?: {
    url: string
    size: number
    compressionRatio: number
  }
}

interface BeforeAfterPreviewProps {
  data: PreviewData
  className?: string
}

type ViewMode = "side-by-side" | "slider" | "toggle"

export function BeforeAfterPreview({ data, className = "" }: BeforeAfterPreviewProps) {
  const [viewMode, setViewMode] = useState<ViewMode>("side-by-side")
  const [sliderPosition, setSliderPosition] = useState([50])
  const [showOriginal, setShowOriginal] = useState(true)
  const [zoom, setZoom] = useState(100)
  const [isDragging, setIsDragging] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  const handleSliderDrag = useCallback(
    (clientX: number) => {
      if (!containerRef.current || !isDragging) return

      const rect = containerRef.current.getBoundingClientRect()
      const position = ((clientX - rect.left) / rect.width) * 100
      setSliderPosition([Math.max(0, Math.min(100, position))])
    },
    [isDragging],
  )

  const handleMouseMove = useCallback(
    (e: MouseEvent) => {
      handleSliderDrag(e.clientX)
    },
    [handleSliderDrag],
  )

  const handleMouseUp = useCallback(() => {
    setIsDragging(false)
  }, [])

  const handleMouseDown = useCallback(() => {
    setIsDragging(true)
  }, [])

  // Add event listeners for dragging
  React.useEffect(() => {
    if (isDragging) {
      document.addEventListener("mousemove", handleMouseMove)
      document.addEventListener("mouseup", handleMouseUp)
      return () => {
        document.removeEventListener("mousemove", handleMouseMove)
        document.removeEventListener("mouseup", handleMouseUp)
      }
    }
  }, [isDragging, handleMouseMove, handleMouseUp])

  const resetZoom = () => setZoom(100)
  const zoomIn = () => setZoom((prev) => Math.min(prev + 25, 200))
  const zoomOut = () => setZoom((prev) => Math.max(prev - 25, 25))

  const renderSideBySide = () => (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 h-full">
      {/* Original */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h4 className="font-medium text-sm">Original</h4>
          <Badge variant="outline">{WebPConverter.formatFileSize(data.original.size)}</Badge>
        </div>
        <div className="relative aspect-video bg-muted rounded-lg overflow-hidden">
          <img
            src={data.original.url || "/placeholder.svg"}
            alt="Original"
            className="w-full h-full object-contain transition-transform duration-200"
            style={{ transform: `scale(${zoom / 100})` }}
          />
        </div>
      </div>

      {/* Compressed */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h4 className="font-medium text-sm">Compressed</h4>
          <div className="flex gap-2">
            {data.compressed && (
              <>
                <Badge variant="outline">{WebPConverter.formatFileSize(data.compressed.size)}</Badge>
                {data.compressed.compressionRatio > 0 && (
                  <Badge variant="secondary">-{Math.round(data.compressed.compressionRatio)}%</Badge>
                )}
              </>
            )}
          </div>
        </div>
        <div className="relative aspect-video bg-muted rounded-lg overflow-hidden">
          {data.compressed ? (
            <img
              src={data.compressed.url || "/placeholder.svg"}
              alt="Compressed"
              className="w-full h-full object-contain transition-transform duration-200"
              style={{ transform: `scale(${zoom / 100})` }}
            />
          ) : (
            <div className="flex items-center justify-center h-full text-muted-foreground">
              No compressed version available
            </div>
          )}
        </div>
      </div>
    </div>
  )

  const renderSlider = () => (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <h4 className="font-medium text-sm">Comparison</h4>
        <div className="flex gap-2">
          <Badge variant="outline">Original: {WebPConverter.formatFileSize(data.original.size)}</Badge>
          {data.compressed && (
            <Badge variant="secondary">
              Compressed: {WebPConverter.formatFileSize(data.compressed.size)}
              (-{Math.round(data.compressed.compressionRatio)}%)
            </Badge>
          )}
        </div>
      </div>

      <div
        ref={containerRef}
        className="relative aspect-video bg-muted rounded-lg overflow-hidden cursor-col-resize"
        onMouseDown={handleMouseDown}
      >
        {/* Original Image (background) */}
        <img
          src={data.original.url || "/placeholder.svg"}
          alt="Original"
          className="absolute inset-0 w-full h-full object-contain"
          style={{ transform: `scale(${zoom / 100})` }}
        />

        {/* Compressed Image (clipped) */}
        {data.compressed && (
          <div
            className="absolute inset-0 overflow-hidden"
            style={{ clipPath: `inset(0 ${100 - sliderPosition[0]}% 0 0)` }}
          >
            <img
              src={data.compressed.url || "/placeholder.svg"}
              alt="Compressed"
              className="w-full h-full object-contain"
              style={{ transform: `scale(${zoom / 100})` }}
            />
          </div>
        )}

        {/* Slider Line */}
        <div
          className="absolute top-0 bottom-0 w-0.5 bg-white shadow-lg z-10 cursor-col-resize"
          style={{ left: `${sliderPosition[0]}%` }}
        >
          <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-8 h-8 bg-white rounded-full shadow-lg flex items-center justify-center">
            <Move3D className="h-4 w-4 text-gray-600" />
          </div>
        </div>
      </div>

      <Slider value={sliderPosition} onValueChange={setSliderPosition} max={100} min={0} step={1} className="mt-2" />
    </div>
  )

  const renderToggle = () => (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h4 className="font-medium text-sm">{showOriginal ? "Original" : "Compressed"}</h4>
          <Button variant="outline" size="sm" onClick={() => setShowOriginal(!showOriginal)} className="h-6 px-2">
            {showOriginal ? <Eye className="h-3 w-3" /> : <EyeOff className="h-3 w-3" />}
            Toggle
          </Button>
        </div>
        <div className="flex gap-2">
          {showOriginal ? (
            <Badge variant="outline">{WebPConverter.formatFileSize(data.original.size)}</Badge>
          ) : data.compressed ? (
            <>
              <Badge variant="outline">{WebPConverter.formatFileSize(data.compressed.size)}</Badge>
              <Badge variant="secondary">-{Math.round(data.compressed.compressionRatio)}%</Badge>
            </>
          ) : null}
        </div>
      </div>

      <div className="relative aspect-video bg-muted rounded-lg overflow-hidden">
        <img
          src={showOriginal ? data.original.url : data.compressed?.url || data.original.url}
          alt={showOriginal ? "Original" : "Compressed"}
          className="w-full h-full object-contain transition-all duration-300"
          style={{ transform: `scale(${zoom / 100})` }}
        />
      </div>
    </div>
  )

  return (
    <Card className={className}>
      <CardContent className="p-4">
        {/* Controls */}
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Button
              variant={viewMode === "side-by-side" ? "default" : "outline"}
              size="sm"
              onClick={() => setViewMode("side-by-side")}
            >
              <SplitSquareHorizontal className="h-4 w-4 mr-1" />
              Side by Side
            </Button>
            <Button
              variant={viewMode === "slider" ? "default" : "outline"}
              size="sm"
              onClick={() => setViewMode("slider")}
              disabled={!data.compressed}
            >
              <Move3D className="h-4 w-4 mr-1" />
              Slider
            </Button>
            <Button
              variant={viewMode === "toggle" ? "default" : "outline"}
              size="sm"
              onClick={() => setViewMode("toggle")}
              disabled={!data.compressed}
            >
              {showOriginal ? <ToggleLeft className="h-4 w-4 mr-1" /> : <ToggleRight className="h-4 w-4 mr-1" />}
              Toggle
            </Button>
          </div>

          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={zoomOut} disabled={zoom <= 25}>
              <ZoomOut className="h-4 w-4" />
            </Button>
            <span className="text-sm font-medium min-w-[3rem] text-center">{zoom}%</span>
            <Button variant="outline" size="sm" onClick={zoomIn} disabled={zoom >= 200}>
              <ZoomIn className="h-4 w-4" />
            </Button>
            <Button variant="outline" size="sm" onClick={resetZoom}>
              <RotateCcw className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Preview Content */}
        <div className="min-h-[300px]">
          {viewMode === "side-by-side" && renderSideBySide()}
          {viewMode === "slider" && renderSlider()}
          {viewMode === "toggle" && renderToggle()}
        </div>

        {/* File Info */}
        {data.compressed && (
          <div className="mt-4 pt-4 border-t">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
              <div>
                <span className="font-medium">Original Size:</span>
                <br />
                {WebPConverter.formatFileSize(data.original.size)}
              </div>
              <div>
                <span className="font-medium">Compressed Size:</span>
                <br />
                {WebPConverter.formatFileSize(data.compressed.size)}
              </div>
              <div>
                <span className="font-medium">Space Saved:</span>
                <br />
                {WebPConverter.formatFileSize(data.original.size - data.compressed.size)}(
                {Math.round(data.compressed.compressionRatio)}%)
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
