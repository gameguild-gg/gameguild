/**
 * ColorPicker — verbatim port of the Lexical playground
 * `packages/lexical-playground/src/ui/ColorPicker.tsx` with Tailwind
 * classes replacing the playground's stylesheet.
 *
 * Provides a hex input, basic swatch grid, HSV saturation/value picker,
 * and hue slider. The mouse drag math (`MoveWrapper`) and color-space
 * conversions are unchanged.
 */
"use client"

import * as React from "react"
import { useMemo, useRef, useState } from "react"
import { calculateZoomLevel } from "@lexical/utils"
import { cn } from "@game-guild/ui/lib/utils"

let skipAddingToHistoryStack = false

interface ColorPickerProps {
  color: string
  onChange?: (value: string, skipHistoryStack: boolean, skipRefocus: boolean) => void
}

export function parseAllowedColor(input: string) {
  return /^rgb\(\d+, \d+, \d+\)$/.test(input) ? input : ""
}

const basicColors = [
  "#d0021b",
  "#f5a623",
  "#f8e71c",
  "#8b572a",
  "#7ed321",
  "#417505",
  "#bd10e0",
  "#9013fe",
  "#4a90e2",
  "#50e3c2",
  "#b8e986",
  "#000000",
  "#4a4a4a",
  "#9b9b9b",
  "#ffffff",
]

const WIDTH = 214
const HEIGHT = 150

export default function ColorPicker({ color, onChange }: Readonly<ColorPickerProps>) {
  const [selfColor, setSelfColor] = useState(transformColor("hex", color))
  const [inputColor, setInputColor] = useState(transformColor("hex", color).hex)
  const innerDivRef = useRef<HTMLDivElement>(null)

  const saturationPosition = useMemo(
    () => ({
      x: (selfColor.hsv.s / 100) * WIDTH,
      y: ((100 - selfColor.hsv.v) / 100) * HEIGHT,
    }),
    [selfColor.hsv.s, selfColor.hsv.v],
  )

  const huePosition = useMemo(
    () => ({
      x: (selfColor.hsv.h / 360) * WIDTH,
    }),
    [selfColor.hsv],
  )

  const emitOnChange = (newColor: string, skipRefocus: boolean = false) => {
    if (innerDivRef.current !== null && onChange) {
      onChange(newColor, skipAddingToHistoryStack, skipRefocus)
    }
  }

  const onSetHex = (hex: string) => {
    setInputColor(hex)
    if (/^#[0-9A-Fa-f]{6}$/i.test(hex)) {
      const newColor = transformColor("hex", hex)
      setSelfColor(newColor)
      emitOnChange(newColor.hex)
    }
  }

  const onMoveSaturation = ({ x, y }: Position) => {
    const newHsv = {
      ...selfColor.hsv,
      s: (x / WIDTH) * 100,
      v: 100 - (y / HEIGHT) * 100,
    }
    const newColor = transformColor("hsv", newHsv)
    setSelfColor(newColor)
    setInputColor(newColor.hex)
    emitOnChange(newColor.hex)
  }

  const onMoveHue = ({ x }: Position) => {
    const newHsv = { ...selfColor.hsv, h: (x / WIDTH) * 360 }
    const newColor = transformColor("hsv", newHsv)
    setSelfColor(newColor)
    setInputColor(newColor.hex)
    emitOnChange(newColor.hex)
  }

  const onBasicColorClick = (e: React.MouseEvent, basicColor: string) => {
    const newColor = transformColor("hex", basicColor)
    setSelfColor(newColor)
    setInputColor(newColor.hex)
    // `isKeyboardInput` heuristic: detail === 0 means it came from a key
    emitOnChange(newColor.hex, e.detail === 0)
  }

  return (
    <div className="space-y-2" style={{ width: WIDTH }} ref={innerDivRef}>
      <label className="flex items-center gap-2 text-xs text-gray-700 dark:text-gray-300">
        <span className="shrink-0">Hex</span>
        <input
          type="text"
          value={inputColor}
          onChange={(e) => {
            e.stopPropagation()
            onSetHex(e.target.value)
          }}
          onKeyDown={(e) => e.stopPropagation()}
          onPointerDown={(e) => e.stopPropagation()}
          className={cn(
            "flex-1 h-7 px-2 rounded border text-xs font-mono",
            "border-gray-300 dark:border-gray-700",
            "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100",
            "focus:outline-none focus:ring-1 focus:ring-blue-500",
          )}
        />
        
      </label>

      <div className="flex flex-wrap gap-1">
        {basicColors.map((basicColor) => (
          <button
            key={basicColor}
            type="button"
            aria-label={basicColor}
            className={cn(
              "w-6 h-6 rounded border border-black/10 dark:border-white/10 cursor-pointer",
              basicColor === selfColor.hex && "ring-2 ring-offset-1 ring-blue-500",
            )}
            style={{ backgroundColor: basicColor }}
            onPointerDown={(e) => e.stopPropagation()}
            onClick={(e) => { e.preventDefault(); e.stopPropagation(); onBasicColorClick(e, basicColor); }}
          />
        ))}
      </div>

      <MoveWrapper
        className="relative w-full rounded cursor-crosshair select-none"
        style={{
          height: HEIGHT,
          backgroundColor: `hsl(${selfColor.hsv.h}, 100%, 50%)`,
          backgroundImage:
            "linear-gradient(to top, #000, transparent), linear-gradient(to right, #fff, transparent)",
        }}
        onChange={onMoveSaturation}
      >
        <div
          className="absolute w-3 h-3 -ml-1.5 -mt-1.5 rounded-full border-2 border-white shadow pointer-events-none"
          style={{
            backgroundColor: selfColor.hex,
            left: saturationPosition.x,
            top: saturationPosition.y,
          }}
        />
      </MoveWrapper>

      <MoveWrapper
        className="relative w-full h-3 rounded cursor-pointer select-none"
        style={{
          backgroundImage:
            "linear-gradient(to right, #f00, #ff0, #0f0, #0ff, #00f, #f0f, #f00)",
        }}
        onChange={onMoveHue}
      >
        <div
          className="absolute w-3 h-3 -ml-1.5 mt-0 rounded-full border-2 border-white shadow pointer-events-none"
          style={{
            backgroundColor: `hsl(${selfColor.hsv.h}, 100%, 50%)`,
            left: huePosition.x,
          }}
        />
      </MoveWrapper>

      <div
        className="w-full h-5 rounded border border-black/10 dark:border-white/10"
        style={{ backgroundColor: selfColor.hex }}
      />

      <button
          type="button"
          onPointerDown={(e) => e.stopPropagation()}
          onClick={(e) => {
            e.preventDefault()
            e.stopPropagation()
            onChange?.("", false, false)
          }}
          title="Clear color"
          aria-label="Clear color"
          className={cn(
            "shrink-0 h-7 px-2 rounded border text-xs",
            "border-gray-300 dark:border-gray-700",
            "bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-200",
            "hover:bg-gray-50 dark:hover:bg-gray-700",
          )}
        >
          Clear
        </button>
    </div>
    
  )
}

export interface Position {
  x: number
  y: number
}

interface MoveWrapperProps {
  className?: string
  style?: React.CSSProperties
  onChange: (position: Position) => void
  children: React.ReactElement
}

function MoveWrapper({ className, style, onChange, children }: MoveWrapperProps) {
  const divRef = useRef<HTMLDivElement>(null)
  const draggedRef = useRef(false)

  const move = (e: React.MouseEvent | MouseEvent): void => {
    if (divRef.current) {
      const { current: div } = divRef
      const { width, height, left, top } = div.getBoundingClientRect()
      const zoom = calculateZoomLevel(div)
      const x = clamp(e.clientX / zoom - left, width, 0)
      const y = clamp(e.clientY / zoom - top, height, 0)
      onChange({ x, y })
    }
  }

  const onMouseDown = (e: React.MouseEvent): void => {
    if (e.button !== 0) return
    e.preventDefault()
    e.stopPropagation()
    move(e)

    const onMouseMove = (_e: MouseEvent): void => {
      draggedRef.current = true
      skipAddingToHistoryStack = true
      move(_e)
    }

    const onMouseUp = (_e: MouseEvent): void => {
      if (draggedRef.current) {
        skipAddingToHistoryStack = false
      }
      document.removeEventListener("mousemove", onMouseMove, false)
      document.removeEventListener("mouseup", onMouseUp, false)
      move(_e)
      draggedRef.current = false
    }

    document.addEventListener("mousemove", onMouseMove, false)
    document.addEventListener("mouseup", onMouseUp, false)
  }

  return (
    <div ref={divRef} className={className} style={style} onMouseDown={onMouseDown} onPointerDown={(e) => e.stopPropagation()}>
      {children}
    </div>
  )
}

function clamp(value: number, max: number, min: number) {
  return value > max ? max : value < min ? min : value
}

interface RGB {
  b: number
  g: number
  r: number
}
interface HSV {
  h: number
  s: number
  v: number
}
interface Color {
  hex: string
  hsv: HSV
  rgb: RGB
}

export function toHex(value: string): string {
  if (!value.startsWith("#")) {
    const ctx = document.createElement("canvas").getContext("2d")
    if (!ctx) {
      throw new Error("2d context not supported or canvas already initialized")
    }
    ctx.fillStyle = value
    return ctx.fillStyle
  } else if (value.length === 4 || value.length === 5) {
    value = value
      .split("")
      .map((v, i) => (i ? v + v : "#"))
      .join("")
    return value
  } else if (value.length === 7 || value.length === 9) {
    return value
  }
  return "#000000"
}

function hex2rgb(hex: string): RGB {
  const rbgArr = (
    hex
      .replace(/^#?([a-f\d])([a-f\d])([a-f\d])$/i, (_m, r, g, b) => "#" + r + r + g + g + b + b)
      .substring(1)
      .match(/.{2}/g) || []
  ).map((x) => parseInt(x, 16))

  return {
    b: rbgArr[2] ?? 0,
    g: rbgArr[1] ?? 0,
    r: rbgArr[0] ?? 0,
  }
}

function rgb2hsv({ r, g, b }: RGB): HSV {
  r /= 255
  g /= 255
  b /= 255

  const max = Math.max(r, g, b)
  const d = max - Math.min(r, g, b)

  const h = d
    ? (max === r
        ? (g - b) / d + (g < b ? 6 : 0)
        : max === g
          ? 2 + (b - r) / d
          : 4 + (r - g) / d) * 60
    : 0
  const s = max ? (d / max) * 100 : 0
  const v = max * 100

  return { h, s, v }
}

function hsv2rgb({ h, s, v }: HSV): RGB {
  s /= 100
  v /= 100

  const i = ~~(h / 60)
  const f = h / 60 - i
  const p = v * (1 - s)
  const q = v * (1 - s * f)
  const t = v * (1 - s * (1 - f))
  const index = i % 6

  const r = Math.round([v, q, p, p, t, v][index]! * 255)
  const g = Math.round([t, v, v, q, p, p][index]! * 255)
  const b = Math.round([p, p, t, v, v, q][index]! * 255)

  return { b, g, r }
}

function rgb2hex({ b, g, r }: RGB): string {
  return "#" + [r, g, b].map((x) => x.toString(16).padStart(2, "0")).join("")
}

function transformColor<M extends keyof Color, C extends Color[M]>(format: M, color: C): Color {
  let hex: Color["hex"] = toHex("#121212")
  let rgb: Color["rgb"] = hex2rgb(hex)
  let hsv: Color["hsv"] = rgb2hsv(rgb)

  if (format === "hex") {
    const value = color as Color["hex"]
    hex = toHex(value)
    rgb = hex2rgb(hex)
    hsv = rgb2hsv(rgb)
  } else if (format === "rgb") {
    const value = color as Color["rgb"]
    rgb = value
    hex = rgb2hex(rgb)
    hsv = rgb2hsv(rgb)
  } else if (format === "hsv") {
    const value = color as Color["hsv"]
    hsv = value
    rgb = hsv2rgb(hsv)
    hex = rgb2hex(rgb)
  }

  return { hex, hsv, rgb }
}
