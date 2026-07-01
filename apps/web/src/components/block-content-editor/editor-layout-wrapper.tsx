"use client"

import * as React from "react"
import { cn } from "@/lib/utils"

export interface EditorLayoutWrapperProps {
  children: React.ReactNode
  className?: string
  style?: React.CSSProperties
}

export function EditorLayoutWrapper({
  children,
  className,
  style,
}: EditorLayoutWrapperProps) {
  return (
    <div 
      className={cn("min-h-screen flex flex-col editor-layout-wrapper", className)}
      style={style}
    >
      {children}
    </div>
  )
}
