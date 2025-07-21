"use client"

import { Input } from "@/components/editor/ui/input"
import { Label } from "@/components/editor/ui/label"

interface ColorPaletteProps {
  selectedColor?: string
  onColorChange: (color: string) => void
  showCustomInput?: boolean
  customInputLabel?: string
  className?: string
}

const COLOR_PALETTE = [
  // Row 1: Red Shades
  "#FF0000",
  "#FF3333",
  "#FF6666",
  "#FF9999",
  "#CC0000",
  "#990000",
  "#660000",
  "#330000",
  // Row 2: Orange Shades
  "#FF8C00",
  "#FFA500",
  "#FFBF00",
  "#FFD900",
  "#CC7000",
  "#995400",
  "#663800",
  "#331C00",
  // Row 3: Yellow Shades
  "#FFFF00",
  "#FFFF33",
  "#FFFF66",
  "#FFFF99",
  "#CCCC00",
  "#999900",
  "#666600",
  "#333300",
  // Row 4: Green Shades
  "#00FF00",
  "#33FF33",
  "#66FF66",
  "#99FF99",
  "#00CC00",
  "#009900",
  "#006600",
  "#003300",
  // Row 5: Blue Shades
  "#0000FF",
  "#3333FF",
  "#6666FF",
  "#9999FF",
  "#0000CC",
  "#000099",
  "#000066",
  "#000033",
  // Row 6: Purple Shades
  "#800080",
  "#993399",
  "#B266B2",
  "#CC99CC",
  "#660066",
  "#4D004D",
  "#330033",
  "#1A001A",
  // Row 7: Pink Shades
  "#FFC0CB",
  "#FFD1D9",
  "#FFE2E8",
  "#FFF3F6",
  "#CC99A4",
  "#99727D",
  "#664B56",
  "#33242B",
  // Row 8: Grayscale Shades
  "#000000",
  "#333333",
  "#666666",
  "#999999",
  "#CCCCCC",
  "#E0E0E0",
  "#F0F0F0",
  "#FFFFFF",
]

export function ColorPalette({
  selectedColor = "",
  onColorChange,
  showCustomInput = true,
  customInputLabel = "Custom:",
  className = "",
}: ColorPaletteProps) {
  return (
    <div className={`p-2 ${className}`}>
      <div className="grid grid-cols-8 gap-1 mb-2">
        {COLOR_PALETTE.map((color) => (
          <button
            key={color}
            className={`w-6 h-6 rounded border-2 hover:scale-110 transition-transform ${
              selectedColor === color ? "border-gray-800 ring-2 ring-blue-500" : "border-gray-300"
            }`}
            style={{ backgroundColor: color }}
            onClick={() => onColorChange(color)}
            title={color}
          />
        ))}
      </div>
      {showCustomInput && (
        <div className="flex items-center gap-2">
          <Label htmlFor="custom-color" className="text-xs">
            {customInputLabel}
          </Label>
          <Input
            onSelect={(e) => e.preventDefault()}
            id="custom-color"
            type="color"
            value={selectedColor || "#000000"}
            onChange={(e) => onColorChange(e.target.value)}
            onPointerDown={(e) => e.stopPropagation()}
            className="w-12 h-8 p-0 border-0"
          />
          <Input
            type="text"
            placeholder="#000000"
            value={selectedColor}
            onChange={(e) => onColorChange(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && e.currentTarget.value) {
                onColorChange(e.currentTarget.value)
                e.preventDefault()
              }
              e.stopPropagation()
            }}
            className="flex-1 text-xs"
          />
        </div>
      )}
    </div>
  )
}
