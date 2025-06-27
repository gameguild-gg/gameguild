"use client"
import { Minus, Plus } from "lucide-react"
import { Button } from "@/components/editor/ui/button"
import { Slider } from "@/components/editor/ui/slider"

interface ImageSizeControlProps {
  size: number
  onChange: (size: number) => void
  className?: string
}

interface SizePreset {
  name: string
  size: number
}

const sizePresets: SizePreset[] = [
  { name: "Full", size: 100 },
  { name: "Regular", size: 75 },
  { name: "Small", size: 50 },
]

export function ImageSizeControl({ size, onChange, className }: ImageSizeControlProps) {
  const decreaseSize = () => {
    const newSize = Math.max(10, size - 10)
    onChange(newSize)
  }

  const increaseSize = () => {
    const newSize = Math.min(100, size + 10)
    onChange(newSize)
  }

  return (
    <div className={`flex flex-col gap-2 ${className}`}>
      <div className="flex items-center justify-between gap-2">
        {sizePresets.map((preset) => (
          <Button
            key={preset.name}
            variant={size === preset.size ? "secondary" : "ghost"}
            size="sm"
            className="flex-1"
            onClick={() => onChange(preset.size)}
          >
            {preset.name}
          </Button>
        ))}
      </div>
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={decreaseSize} disabled={size <= 10}>
          <Minus className="h-4 w-4" />
        </Button>
        <Slider
          value={[size]}
          min={10}
          max={100}
          step={1}
          className="flex-1"
          onValueChange={(values) => onChange(values[0])}
        />
        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={increaseSize} disabled={size >= 100}>
          <Plus className="h-4 w-4" />
        </Button>
        <div className="min-w-[3rem] text-sm text-center">{size}%</div>
      </div>
    </div>
  )
}
