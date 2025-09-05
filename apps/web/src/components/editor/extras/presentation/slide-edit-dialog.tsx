"use client"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Slider } from "@/components/ui/slider"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { SlideRenderer } from "@/components/editor/nodes/presentation/slide-renderer"
import type { Slide, SlideLayout, SlideTheme, ImageFilters, ImageSize } from "@/components/editor/nodes/presentation/types"

interface SlideEditDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  slide: Slide | null
  onSave: (slide: Slide) => void
}

export function SlideEditDialog({ open, onOpenChange, slide, onSave }: SlideEditDialogProps) {
  const [editedSlide, setEditedSlide] = useState<Slide | null>(null)

  useEffect(() => {
    if (slide) {
      setEditedSlide({
        ...slide,
        filters: slide.filters || {
          brightness: 100,
          contrast: 100,
          saturation: 100,
          blur: 0,
          hueRotate: 0,
          opacity: 100,
        },
        imageSize: slide.imageSize || {
          width: 100,
          height: 100,
          objectFit: "cover",
        },
      })
    }
  }, [slide])

  const handleSave = () => {
    if (editedSlide) {
      onSave(editedSlide)
    }
  }

  const handleFilterChange = (filterName: keyof ImageFilters, value: number) => {
    if (!editedSlide) return
    
    setEditedSlide({
      ...editedSlide,
      filters: {
        ...editedSlide.filters!,
        [filterName]: value,
      },
    })
  }

  const handleSizeChange = (sizeName: keyof ImageSize, value: number | string) => {
    if (!editedSlide) return
    
    setEditedSlide({
      ...editedSlide,
      imageSize: {
        ...editedSlide.imageSize!,
        [sizeName]: value,
      },
    })
  }

  const resetFilters = () => {
    if (!editedSlide) return
    
    setEditedSlide({
      ...editedSlide,
      filters: {
        brightness: 100,
        contrast: 100,
        saturation: 100,
        blur: 0,
        hueRotate: 0,
        opacity: 100,
      },
    })
  }

  const resetSize = () => {
    if (!editedSlide) return
    
    setEditedSlide({
      ...editedSlide,
      imageSize: {
        width: 100,
        height: 100,
        objectFit: "cover",
      },
    })
  }

  if (!editedSlide) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-6xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit Slide</DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left side - Controls */}
          <div className="space-y-6">
            <Tabs defaultValue="content" className="w-full">
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="content">Content</TabsTrigger>
                <TabsTrigger value="filters">Filters</TabsTrigger>
                <TabsTrigger value="size">Size</TabsTrigger>
              </TabsList>

              <TabsContent value="content" className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="title">Title</Label>
                  <Input
                    id="title"
                    value={editedSlide.title}
                    onChange={(e) => setEditedSlide({ ...editedSlide, title: e.target.value })}
                    placeholder="Slide title"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="content">Content</Label>
                  <Textarea
                    id="content"
                    value={editedSlide.content}
                    onChange={(e) => setEditedSlide({ ...editedSlide, content: e.target.value })}
                    placeholder="Slide content"
                    rows={4}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="layout">Layout</Label>
                  <Select
                    value={editedSlide.layout}
                    onValueChange={(value: SlideLayout) => setEditedSlide({ ...editedSlide, layout: value })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="title">Title</SelectItem>
                      <SelectItem value="content">Content</SelectItem>
                      <SelectItem value="title-content">Title + Content</SelectItem>
                      <SelectItem value="image-text">Image + Text</SelectItem>
                      <SelectItem value="text-image">Text + Image</SelectItem>
                      <SelectItem value="full-image">Full Image</SelectItem>
                      <SelectItem value="two-column">Two Column</SelectItem>
                      <SelectItem value="blank">Blank</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="theme">Theme</Label>
                  <Select
                    value={editedSlide.theme}
                    onValueChange={(value: SlideTheme) => setEditedSlide({ ...editedSlide, theme: value })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="light">Light</SelectItem>
                      <SelectItem value="dark">Dark</SelectItem>
                      <SelectItem value="standard">Standard</SelectItem>
                      <SelectItem value="gradient">Gradient</SelectItem>
                      <SelectItem value="custom">Custom</SelectItem>
                      <SelectItem value="image">Image</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="notes">Notes</Label>
                  <Textarea
                    id="notes"
                    value={editedSlide.notes || ""}
                    onChange={(e) => setEditedSlide({ ...editedSlide, notes: e.target.value })}
                    placeholder="Speaker notes"
                    rows={3}
                  />
                </div>
              </TabsContent>

              <TabsContent value="filters" className="space-y-4">
                <Card>
                  <CardHeader className="pb-3">
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-sm">Image Filters</CardTitle>
                      <Button variant="outline" size="sm" onClick={resetFilters}>
                        Reset
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Brightness</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.filters?.brightness}%</span>
                      </div>
                      <Slider
                        value={[editedSlide.filters?.brightness || 100]}
                        onValueChange={([value]) => handleFilterChange("brightness", value ?? 100)}
                        min={0}
                        max={200}
                        step={1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Contrast</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.filters?.contrast}%</span>
                      </div>
                      <Slider
                        value={[editedSlide.filters?.contrast || 100]}
                        onValueChange={([value]) => handleFilterChange("contrast", value ?? 100)}
                        min={0}
                        max={200}
                        step={1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Saturation</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.filters?.saturation}%</span>
                      </div>
                      <Slider
                        value={[editedSlide.filters?.saturation || 100]}
                        onValueChange={([value]) => handleFilterChange("saturation", value ?? 100)}
                        min={0}
                        max={200}
                        step={1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Blur</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.filters?.blur}px</span>
                      </div>
                      <Slider
                        value={[editedSlide.filters?.blur || 0]}
                        onValueChange={([value]) => handleFilterChange("blur", value ?? 0)}
                        min={0}
                        max={10}
                        step={0.1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Hue Rotate</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.filters?.hueRotate}°</span>
                      </div>
                      <Slider
                        value={[editedSlide.filters?.hueRotate || 0]}
                        onValueChange={([value]) => handleFilterChange("hueRotate", value ?? 0)}
                        min={0}
                        max={360}
                        step={1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Opacity</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.filters?.opacity}%</span>
                      </div>
                      <Slider
                        value={[editedSlide.filters?.opacity || 100]}
                        onValueChange={([value]) => handleFilterChange("opacity", value ?? 100)}
                        min={0}
                        max={100}
                        step={1}
                        className="w-full"
                      />
                    </div>
                  </CardContent>
                </Card>
              </TabsContent>

              <TabsContent value="size" className="space-y-4">
                <Card>
                  <CardHeader className="pb-3">
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-sm">Image Size & Fit</CardTitle>
                      <Button variant="outline" size="sm" onClick={resetSize}>
                        Reset
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Width</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.imageSize?.width}%</span>
                      </div>
                      <Slider
                        value={[editedSlide.imageSize?.width || 100]}
                        onValueChange={([value]) => handleSizeChange("width", value ?? 100)}
                        min={10}
                        max={200}
                        step={1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <Label>Height</Label>
                        <span className="text-sm text-muted-foreground">{editedSlide.imageSize?.height}%</span>
                      </div>
                      <Slider
                        value={[editedSlide.imageSize?.height || 100]}
                        onValueChange={([value]) => handleSizeChange("height", value ?? 100)}
                        min={10}
                        max={200}
                        step={1}
                        className="w-full"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label>Object Fit</Label>
                      <Select
                        value={editedSlide.imageSize?.objectFit || "cover"}
                        onValueChange={(value: "cover" | "contain" | "fill" | "scale-down") => 
                          handleSizeChange("objectFit", value)
                        }
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="cover">Cover</SelectItem>
                          <SelectItem value="contain">Contain</SelectItem>
                          <SelectItem value="fill">Fill</SelectItem>
                          <SelectItem value="scale-down">Scale Down</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </CardContent>
                </Card>
              </TabsContent>
            </Tabs>
          </div>

          {/* Right side - Preview */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold">Preview</h3>
              <div className="text-sm text-muted-foreground">
                Live preview of your changes
              </div>
            </div>
            
            <div className="border rounded-lg overflow-hidden bg-white">
              <div className="aspect-video">
                <SlideRenderer slide={editedSlide} customThemeColor={null} />
              </div>
            </div>

            {/* Quick stats */}
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div className="space-y-1">
                <div className="font-medium">Layout</div>
                <div className="text-muted-foreground capitalize">{editedSlide.layout}</div>
              </div>
              <div className="space-y-1">
                <div className="font-medium">Theme</div>
                <div className="text-muted-foreground capitalize">{editedSlide.theme}</div>
              </div>
            </div>
          </div>
        </div>

        <Separator />

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave}>
            Save Changes
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
