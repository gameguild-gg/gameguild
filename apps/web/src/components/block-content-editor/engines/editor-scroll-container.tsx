"use client"

import * as React from "react"
import { createContext, useContext } from "react"
import { cn } from "@/lib/utils"

export type ScrollMode = "page" | "container"

const ScrollContainerContext = createContext<ScrollMode>("page")

export function useScrollMode() {
  return useContext(ScrollContainerContext)
}

interface EditorScrollContainerProps {
  children: React.ReactNode
  mode?: ScrollMode
  maxHeight?: string | number
  height?: string | number
  className?: string
  styled?: boolean
}

export function EditorScrollContainer({
  children,
  mode = "page",
  maxHeight,
  height,
  className,
  styled = false
}: EditorScrollContainerProps) {
  const content = (
    <ScrollContainerContext.Provider value={mode}>
      {children}
    </ScrollContainerContext.Provider>
  )

  if (mode === "container") {
    return (
      <div
        className={cn(
          "w-full flex flex-col min-h-0 overflow-hidden",
          styled && "border border-gray-200 dark:border-gray-800 rounded-lg bg-white dark:bg-gray-950 shadow-sm",
          className
        )}
        style={{
          maxHeight: maxHeight ?? "600px",
          height: height ?? "100%",
        }}
      >
        {content}
      </div>
    )
  }

  // mode === "page"
  return (
    <div className={cn("w-full", className)}>
      {content}
    </div>
  )
}
