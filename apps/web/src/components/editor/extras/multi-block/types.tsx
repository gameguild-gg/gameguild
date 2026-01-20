"use client"

export interface PanelData {
  id: string
  blockIds: string[]
  defaultSize?: number
  direction?: "horizontal" | "vertical"
}
