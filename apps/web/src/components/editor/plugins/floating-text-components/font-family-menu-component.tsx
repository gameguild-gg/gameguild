"use client"

import { useCallback, useState, useEffect } from "react"
import { $getSelection, $isRangeSelection } from "lexical"
import { Type } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/editor/ui/dropdown-menu"

interface FontFamilyMenuComponentProps {
  editor: any
  currentFontFamily: string
}

interface FontFamily {
  name: string
  family: string
  variations: FontVariation[]
}

interface FontVariation {
  name: string
  style: string
  weight: string
}

const GROUPED_FONT_FAMILIES: { [key: string]: FontFamily[] } = {
  Cursive: [
    {
      name: "Pacifico",
      family: "'Pacifico', cursive",
      variations: [{ name: "Regular", style: "normal", weight: "400" }],
    },
    {
      name: "Dancing Script",
      family: "'Dancing Script', cursive",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
      ],
    },
    {
      name: "Great Vibes",
      family: "'Great Vibes', cursive",
      variations: [{ name: "Regular", style: "normal", weight: "400" }],
    },
    {
      name: "Caveat",
      family: "'Caveat', cursive",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
      ],
    },
  ],
  Fantasy: [
    {
      name: "Creepster",
      family: "'Creepster', cursive", // Often categorized as fantasy due to its style
      variations: [{ name: "Regular", style: "normal", weight: "400" }],
    },
    {
      name: "Metal Mania",
      family: "'Metal Mania', cursive", // Similar to Creepster, often used for fantasy/horror
      variations: [{ name: "Regular", style: "normal", weight: "400" }],
    },
    {
      name: "Nosifier",
      family: "'Nosifier', cursive", // Another unique, fantasy-like font
      variations: [{ name: "Regular", style: "normal", weight: "400" }],
    },
    {
      name: "Permanent Marker",
      family: "'Permanent Marker', cursive", // Hand-drawn, can fit fantasy/casual
      variations: [{ name: "Regular", style: "normal", weight: "400" }],
    },
  ],
  "Humanist Sans": [
    {
      name: "Merriweather Sans",
      family: "'Merriweather Sans', sans-serif",
      variations: [
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
      ],
    },
    {
      name: "PT Sans",
      family: "'PT Sans', sans-serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Work Sans",
      family: "'Work Sans', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Source Sans 3",
      family: "'Source Sans 3', sans-serif",
      variations: [
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
  ],
  "Geometric Sans": [
    {
      name: "Quicksand",
      family: "'Quicksand', sans-serif",
      variations: [
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
      ],
    },
    {
      name: "Nunito Sans",
      family: "'Nunito Sans', sans-serif",
      variations: [
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Josefin Sans",
      family: "'Josefin Sans', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
      ],
    },
    {
      name: "Kanit",
      family: "'Kanit', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
  ],
  "Sans-serif": [
    {
      name: "Arial",
      family: "Arial, sans-serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Liberation Sans",
      family: "Liberation Sans, sans-serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Helvetica",
      family: "Helvetica, sans-serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Verdana",
      family: "Verdana, sans-serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Trebuchet MS",
      family: "Trebuchet MS, sans-serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Open Sans",
      family: "'Open Sans', sans-serif",
      variations: [
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
      ],
    },
    {
      name: "Roboto",
      family: "'Roboto', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Lato",
      family: "'Lato', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Montserrat",
      family: "'Montserrat', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Source Sans Pro",
      family: "'Source Sans Pro', sans-serif",
      variations: [
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Poppins",
      family: "'Poppins', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Nunito",
      family: "'Nunito', sans-serif",
      variations: [
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Inter",
      family: "'Inter', sans-serif",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
  ],
  Serif: [
    {
      name: "Times New Roman",
      family: "Times New Roman, serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Liberation Serif",
      family: "Liberation Serif, serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Georgia",
      family: "Georgia, serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Playfair Display",
      family: "'Playfair Display', serif",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Merriweather",
      family: "'Merriweather', serif",
      variations: [
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
  ],
  "Monospace (Programming)": [
    {
      name: "Courier New",
      family: "Courier New, monospace",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Liberation Mono",
      family: "Liberation Mono, monospace",
      variations: [
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Italic", style: "italic", weight: "400" },
        { name: "Bold Italic", style: "italic", weight: "700" },
      ],
    },
    {
      name: "Fira Code",
      family: "'Fira Code', monospace",
      variations: [
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
      ],
    },
    {
      name: "JetBrains Mono",
      family: "'JetBrains Mono', monospace",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
      ],
    },
    {
      name: "Roboto Mono",
      family: "'Roboto Mono', monospace",
      variations: [
        { name: "Thin", style: "normal", weight: "100" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Bold", style: "normal", weight: "700" },
      ],
    },
    {
      name: "Source Code Pro",
      family: "'Source Code Pro', monospace",
      variations: [
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
    {
      name: "Inconsolata",
      family: "'Inconsolata', monospace",
      variations: [
        { name: "Extra Light", style: "normal", weight: "200" },
        { name: "Light", style: "normal", weight: "300" },
        { name: "Regular", style: "normal", weight: "400" },
        { name: "Medium", style: "normal", weight: "500" },
        { name: "Semi Bold", style: "normal", weight: "600" },
        { name: "Bold", style: "normal", weight: "700" },
        { name: "Extra Bold", style: "normal", weight: "800" },
        { name: "Black", style: "normal", weight: "900" },
      ],
    },
  ],
}

const ALL_FONT_FAMILIES_FLAT: FontFamily[] = Object.values(GROUPED_FONT_FAMILIES).flat()

export function FontFamilyMenuComponent({ editor, currentFontFamily }: FontFamilyMenuComponentProps) {
  // ADDED: Local state to immediately reflect selection
  const [displayedFontFamily, setDisplayedFontFamily] = useState(currentFontFamily)
  const [displayedFontWeight, setDisplayedFontWeight] = useState("400") // Default to regular
  const [displayedFontStyle, setDisplayedFontStyle] = useState("normal") // Default to normal

  // ADDED: Effect to synchronize local state with prop when currentFontFamily changes from editor updates
  useEffect(() => {
    setDisplayedFontFamily(currentFontFamily)
    // Attempt to derive weight/style from currentFontFamily if possible, or reset to default
    const matchedFont = ALL_FONT_FAMILIES_FLAT.find((font) =>
      currentFontFamily.toLowerCase().includes(font.name.toLowerCase()),
    )
    if (matchedFont) {
      // This is a simplification. A more robust solution would parse the actual style from the node.
      // For immediate display, we'll just reset to a common default or the first variation.
      setDisplayedFontWeight("400")
      setDisplayedFontStyle("normal")
    } else {
      // If currentFontFamily is not found in our list, it might be a custom font or default browser font.
      // We can't reliably determine its weight/style from just the family string.
      setDisplayedFontWeight("400")
      setDisplayedFontStyle("normal")
    }
  }, [currentFontFamily])

  const handleFontChange = useCallback(
    (fontFamily: string, fontWeight: string, fontStyle: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes()
          nodes.forEach((node) => {
            if (node.getTextContent()) {
              const currentStyle = node.getStyle() || ""
              // Remove existing font properties
              let newStyle = currentStyle
                .replace(/font-family:\s*[^;]+;?/g, "")
                .replace(/font-weight:\s*[^;]+;?/g, "")
                .replace(/font-style:\s*[^;]+;?/g, "")

              // Add new font properties
              newStyle += `font-family: ${fontFamily}; font-weight: ${fontWeight}; font-style: ${fontStyle};`

              node.setStyle(newStyle.trim())
            }
          })
        }
      })
      // ADDED: Immediately update local state for display
      setDisplayedFontFamily(fontFamily)
      setDisplayedFontWeight(fontWeight)
      setDisplayedFontStyle(fontStyle)
    },
    [editor],
  )

  const getCurrentFontDisplay = () => {
    if (!displayedFontFamily) return "Default"

    const matchedFont = ALL_FONT_FAMILIES_FLAT.find((font) =>
      displayedFontFamily.toLowerCase().includes(font.name.toLowerCase()),
    )

    let displayString = matchedFont ? matchedFont.name : "Custom"

    if (matchedFont) {
      const matchedVariation = matchedFont.variations.find(
        (v) => v.weight === displayedFontWeight && v.style === displayedFontStyle,
      )
      if (matchedVariation && matchedVariation.name !== "Regular") {
        displayString += ` (${matchedVariation.name})`
      }
    }

    return displayString
  }

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Type className="mr-2 h-4 w-4" />
        {/* MODIFIED: Use local state for display */}
        <span>Font: {getCurrentFontDisplay()}</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent side="right" align="start" className="max-h-[90vh] overflow-y-auto">
        {/* MODIFIED: Use local state for display */}
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Current: {getCurrentFontDisplay()}</div>
        <DropdownMenuSeparator />
        {Object.entries(GROUPED_FONT_FAMILIES).map(([categoryName, fontFamilies]) => (
          <DropdownMenuSub key={categoryName}>
            <DropdownMenuSubTrigger>
              <span>{categoryName}</span>
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent side="right" align="start">
              {fontFamilies.map((fontFamily) => (
                <DropdownMenuSub key={fontFamily.name}>
                  <DropdownMenuSubTrigger>
                    <span style={{ fontFamily: fontFamily.family }}>{fontFamily.name}</span>
                  </DropdownMenuSubTrigger>
                  <DropdownMenuSubContent side="right" align="start">
                    {fontFamily.variations.map((variation) => (
                      <DropdownMenuItem
                        key={`${fontFamily.name}-${variation.name}`}
                        onClick={() => handleFontChange(fontFamily.family, variation.weight, variation.style)}
                        onSelect={(e) => e.preventDefault()}
                      >
                        <span
                          style={{
                            fontFamily: fontFamily.family,
                            fontWeight: variation.weight,
                            fontStyle: variation.style,
                          }}
                        >
                          {variation.name}
                        </span>
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuSubContent>
                </DropdownMenuSub>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>
        ))}
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
