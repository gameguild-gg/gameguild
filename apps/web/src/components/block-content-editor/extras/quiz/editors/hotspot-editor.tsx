/**
 * Hotspot Editor
 * Upload an image, click to place hotspot points, configure concentric zone radii.
 * Points become invisible to students; evaluation checks proximity.
 */

"use client"

import { useState, useRef, useEffect, useCallback } from "react"
import { useFormContext } from "react-hook-form"
import { Upload, X, Plus, Crosshair, Trash2, ImageIcon } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import type { HotspotEntry, HotspotPoint } from "../types"

const ZONE_COLORS = [
  { bg: "rgba(34, 197, 94, 0.2)", border: "rgb(34, 197, 94)" },
  { bg: "rgba(234, 179, 8, 0.15)", border: "rgb(234, 179, 8)" },
  { bg: "rgba(249, 115, 22, 0.12)", border: "rgb(249, 115, 22)" },
  { bg: "rgba(239, 68, 68, 0.1)", border: "rgb(239, 68, 68)" },
]

export function HotspotEditor() {
  const { watch, setValue } = useFormContext<HotspotEntry>()
  const imageUrl = watch("imageUrl")
  const hotspots = watch("hotspots") || []

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const [dragMoved, setDragMoved] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const hotspotsRef = useRef(hotspots)
  useEffect(() => { hotspotsRef.current = hotspots }, [hotspots])

  const handleFile = useCallback((file: File) => {
    if (!file.type.startsWith("image/")) return
    const reader = new FileReader()
    reader.onload = (e) => {
      const dataUrl = e.target?.result as string
      const img = new window.Image()
      img.onload = () => {
        setValue("imageUrl", dataUrl)
        setValue("imageWidth", img.naturalWidth)
        setValue("imageHeight", img.naturalHeight)
      }
      img.src = dataUrl
    }
    reader.readAsDataURL(file)
  }, [setValue])

  const handleOverlayClick = useCallback((e: React.MouseEvent) => {
    if (dragMoved) return
    const rect = containerRef.current?.getBoundingClientRect()
    if (!rect) return
    const x = ((e.clientX - rect.left) / rect.width) * 100
    const y = ((e.clientY - rect.top) / rect.height) * 100

    // Check if near existing point → select it
    for (const hp of hotspotsRef.current) {
      const dx = Math.abs(hp.x - x)
      const dy = Math.abs(hp.y - y)
      if (dx < 2.5 && dy < 2.5) {
        setSelectedId(hp.id)
        return
      }
    }

    // Add new point
    const newPoint: HotspotPoint = {
      id: Math.random().toString(36).substring(7),
      x: +x.toFixed(2),
      y: +y.toFixed(2),
      zones: [
        { radius: 3, label: "Exact" },
        { radius: 6, label: "Close" },
        { radius: 10, label: "Near" },
      ],
    }
    setValue("hotspots", [...hotspotsRef.current, newPoint])
    setSelectedId(newPoint.id)
  }, [dragMoved, setValue])

  // Drag handling via document listeners
  useEffect(() => {
    if (!draggingId) return
    setDragMoved(false)

    const handleMove = (e: PointerEvent) => {
      setDragMoved(true)
      const rect = containerRef.current?.getBoundingClientRect()
      if (!rect) return
      const x = Math.max(0, Math.min(100, ((e.clientX - rect.left) / rect.width) * 100))
      const y = Math.max(0, Math.min(100, ((e.clientY - rect.top) / rect.height) * 100))
      setValue("hotspots", hotspotsRef.current.map(hp =>
        hp.id === draggingId ? { ...hp, x: +x.toFixed(2), y: +y.toFixed(2) } : hp
      ))
    }

    const handleUp = () => {
      setDraggingId(null)
      // Reset dragMoved after a tick so click handler can check it
      setTimeout(() => setDragMoved(false), 0)
    }

    document.addEventListener("pointermove", handleMove)
    document.addEventListener("pointerup", handleUp)
    return () => {
      document.removeEventListener("pointermove", handleMove)
      document.removeEventListener("pointerup", handleUp)
    }
  }, [draggingId, setValue])

  const removePoint = (id: string) => {
    setValue("hotspots", hotspots.filter(hp => hp.id !== id))
    if (selectedId === id) setSelectedId(null)
  }

  const updateZone = (pointId: string, zoneIdx: number, field: "radius" | "label", value: number | string) => {
    setValue("hotspots", hotspots.map(hp => {
      if (hp.id !== pointId) return hp
      const zones = hp.zones.map((z, i) =>
        i === zoneIdx ? { ...z, [field]: value } : z
      )
      return { ...hp, zones }
    }))
  }

  const addZone = (pointId: string) => {
    setValue("hotspots", hotspots.map(hp => {
      if (hp.id !== pointId) return hp
      const maxRadius = hp.zones.length > 0 ? Math.max(...hp.zones.map(z => z.radius)) : 0
      return { ...hp, zones: [...hp.zones, { radius: maxRadius + 5, label: `Zone ${hp.zones.length + 1}` }] }
    }))
  }

  const removeZone = (pointId: string, zoneIdx: number) => {
    setValue("hotspots", hotspots.map(hp => {
      if (hp.id !== pointId) return hp
      return { ...hp, zones: hp.zones.filter((_, i) => i !== zoneIdx) }
    }))
  }

  const selectedPoint = hotspots.find(hp => hp.id === selectedId)

  return (
    <div className="space-y-5">
      {/* Image Upload / Display */}
      {!imageUrl ? (
        <div
          className="border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-xl p-8 text-center cursor-pointer hover:border-blue-400 dark:hover:border-blue-500 transition-colors"
          onClick={() => fileInputRef.current?.click()}
          onDragOver={(e) => { e.preventDefault(); e.stopPropagation() }}
          onDrop={(e) => {
            e.preventDefault(); e.stopPropagation()
            const file = e.dataTransfer.files[0]
            if (file) handleFile(file)
          }}
        >
          <Upload className="h-8 w-8 mx-auto text-gray-400 mb-3" />
          <p className="text-sm font-medium text-gray-600 dark:text-gray-300">
            Drag &amp; drop an image or click to browse
          </p>
          <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
            PNG, JPG, SVG, WebP
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) handleFile(file)
            }}
          />
        </div>
      ) : (
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300 flex items-center gap-1.5">
              <ImageIcon className="h-4 w-4" />
              Image (click to add hotspot points)
            </Label>
            <div className="flex gap-2">
              <Button type="button" variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}>
                Change
              </Button>
              <Button
                type="button" variant="outline" size="sm"
                onClick={() => { setValue("imageUrl", ""); setValue("hotspots", []); setSelectedId(null) }}
                className="hover:text-red-600"
              >
                Remove
              </Button>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0]
                  if (file) handleFile(file)
                }}
              />
            </div>
          </div>

          {/* Image with overlay */}
          <div
            ref={containerRef}
            className="relative select-none rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700"
            style={{ touchAction: draggingId ? "none" : "auto" }}
            onClick={handleOverlayClick}
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={imageUrl} alt="Hotspot image" className="w-full block" draggable={false} />

            {/* Zone circles (render outermost first so inner appears on top) */}
            {hotspots.map((hp) => {
              const isSelected = hp.id === selectedId
              const sortedZones = [...hp.zones].sort((a, b) => b.radius - a.radius)
              return sortedZones.map((zone, zi) => {
                const colorIdx = hp.zones.indexOf(zone)
                const color = ZONE_COLORS[colorIdx % ZONE_COLORS.length] ?? ZONE_COLORS[ZONE_COLORS.length - 1]!
                return (
                  <div
                    key={`${hp.id}-z${zi}`}
                    className="absolute rounded-full pointer-events-none"
                    style={{
                      left: `${hp.x}%`,
                      top: `${hp.y}%`,
                      width: `${zone.radius * 2}%`,
                      aspectRatio: "1",
                      transform: "translate(-50%, -50%)",
                      backgroundColor: isSelected ? color.bg : color.bg.replace(/[\d.]+\)$/, '0.08)'),
                      border: `1.5px ${isSelected ? "solid" : "dashed"} ${color.border}`,
                      opacity: isSelected ? 1 : 0.7,
                    }}
                  />
                )
              })
            })}

            {/* Point markers */}
            {hotspots.map((hp) => (
              <div
                key={hp.id}
                className={`absolute w-5 h-5 -translate-x-1/2 -translate-y-1/2 flex items-center justify-center cursor-grab active:cursor-grabbing z-10 ${
                  hp.id === selectedId
                    ? "text-blue-600 dark:text-blue-400"
                    : "text-gray-500 dark:text-gray-300"
                }`}
                style={{ left: `${hp.x}%`, top: `${hp.y}%` }}
                onPointerDown={(e) => {
                  e.stopPropagation()
                  e.preventDefault()
                  setDraggingId(hp.id)
                  setSelectedId(hp.id)
                }}
              >
                <Crosshair className="h-5 w-5 drop-shadow-md" />
              </div>
            ))}

            {/* Crosshair cursor overlay */}
            {!draggingId && (
              <div className="absolute inset-0 cursor-crosshair" />
            )}
          </div>
        </div>
      )}

      {/* Hotspot Points Configuration */}
      {imageUrl && (
        <div className="space-y-3">
          <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Hotspot Points ({hotspots.length})
          </Label>

          {hotspots.length === 0 && (
            <div className="text-center py-4 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
              <Crosshair className="h-5 w-5 mx-auto mb-1 opacity-50" />
              <p className="text-sm">Click on the image above to add hotspot points.</p>
            </div>
          )}

          <div className="space-y-2">
            {hotspots.map((hp, hpIdx) => {
              const isSelected = hp.id === selectedId
              return (
                <div
                  key={hp.id}
                  className={`rounded-lg border transition-colors ${
                    isSelected
                      ? "border-blue-300 dark:border-blue-700 bg-blue-50/50 dark:bg-blue-950/20"
                      : "border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50"
                  }`}
                >
                  {/* Point header */}
                  <div
                    className="flex items-center justify-between px-3 py-2 cursor-pointer"
                    onClick={() => setSelectedId(isSelected ? null : hp.id)}
                  >
                    <div className="flex items-center gap-2">
                      <Crosshair className="h-4 w-4 text-blue-500" />
                      <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        Point {hpIdx + 1}
                      </span>
                      <span className="text-xs text-gray-400 font-mono">
                        ({hp.x.toFixed(1)}%, {hp.y.toFixed(1)}%)
                      </span>
                    </div>
                    <Button
                      type="button" variant="ghost" size="sm"
                      onClick={(e) => { e.stopPropagation(); removePoint(hp.id) }}
                      className="h-7 w-7 p-0 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>

                  {/* Zones (expanded when selected) */}
                  {isSelected && (
                    <div className="px-3 pb-3 space-y-2">
                      <div className="text-xs text-gray-500 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-2 rounded border border-blue-200 dark:border-blue-800">
                        Define concentric zones from innermost (highest accuracy) to outermost.
                        Radius is in % of image width.
                      </div>

                      {hp.zones.map((zone, zi) => {
                        const color = ZONE_COLORS[zi % ZONE_COLORS.length] ?? ZONE_COLORS[ZONE_COLORS.length - 1]!
                        return (
                          <div key={zi} className="flex items-center gap-2 px-2 py-1.5 rounded border" style={{ borderColor: color.border + "60" }}>
                            <div className="w-3 h-3 rounded-full shrink-0" style={{ backgroundColor: color.border }} />
                            <Input
                              value={zone.label}
                              onChange={(e) => updateZone(hp.id, zi, "label", e.target.value)}
                              autoComplete="off"
                              className="h-7 text-xs bg-white dark:bg-gray-800 w-24"
                            />
                            <span className="text-xs text-gray-500 shrink-0">Radius</span>
                            <Input
                              type="number"
                              min={0.5}
                              max={50}
                              step={0.5}
                              value={zone.radius}
                              onChange={(e) => updateZone(hp.id, zi, "radius", parseFloat(e.target.value) || 0)}
                              autoComplete="off"
                              className="h-7 text-xs bg-white dark:bg-gray-800 w-20"
                            />
                            <span className="text-xs text-gray-400 shrink-0">%</span>
                            {hp.zones.length > 1 && (
                              <Button
                                type="button" variant="ghost" size="sm"
                                onClick={() => removeZone(hp.id, zi)}
                                className="h-6 w-6 p-0 hover:text-red-600 shrink-0"
                              >
                                <X className="h-3 w-3" />
                              </Button>
                            )}
                          </div>
                        )
                      })}

                      <Button
                        type="button" variant="outline" size="sm"
                        onClick={() => addZone(hp.id)}
                        className="w-full text-xs h-7"
                      >
                        <Plus className="h-3 w-3 mr-1" /> Add Zone
                      </Button>
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {/* Instructions */}
      <div className="text-xs text-gray-500 dark:text-gray-400 bg-amber-50 dark:bg-amber-950/20 p-2 rounded border border-amber-200 dark:border-amber-800">
        <strong>How it works:</strong> Upload an image and click to place target points.
        Each point has concentric zones defining accuracy levels (inner = most accurate).
        Students will click on the image, and their click is evaluated against the closest hotspot zone.
        The zones are invisible to students.
      </div>
    </div>
  )
}
