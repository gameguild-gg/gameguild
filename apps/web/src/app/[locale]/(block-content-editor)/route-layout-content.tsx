"use client"

import * as React from "react"
import { usePathname } from "next/navigation"
import { TopMenu } from "@/components/block-content-editor/top-menu"
import { EditorLayoutWrapper } from "@/components/block-content-editor/editor-layout-wrapper"
import { cn } from "@/lib/utils"

export function RouteLayoutContent({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()
  
  // Detect if we are on any editor/viewer page
  const isEditor = 
    pathname.includes("/studio") || 
    pathname.includes("/doc-editor") || 
    pathname.includes("/block-editor") || 
    pathname.includes("/quiz-editor") || 
    pathname.includes("/full-editor") || 
    pathname.includes("/viewer")

  return (
    <EditorLayoutWrapper
      style={{
        "--lexical-toolbar-offset": isEditor ? "var(--editor-toolbar-offset, 12px)" : "80px"
      } as React.CSSProperties}
    >
      {!isEditor && <TopMenu />}
      <div 
        className={cn(
          "flex flex-col flex-1",
          isEditor ? "mt-16 2xl:mt-0" : "mt-20"
        )}
      >
        {children}
      </div>
    </EditorLayoutWrapper>
  )
}
