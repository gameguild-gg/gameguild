"use client"

import { useRef, useState } from "react"
import type { ImperativePanelHandle } from "react-resizable-panels"

export function usePanelCollapse() {
  const [collapsedPanels, setCollapsedPanels] = useState<Set<string>>(new Set())
  const panelRefs = useRef<Record<string, ImperativePanelHandle | null>>({})

  const handleToggleCollapse = (panelId: string) => {
    const panelRef = panelRefs.current[panelId]
    if (!panelRef) return

    const isCollapsed = collapsedPanels.has(panelId)
    
    if (isCollapsed) {
      panelRef.expand()
    } else {
      panelRef.collapse()
    }
  }

  const handleCollapsedTabClick = (panelId: string) => {
    const panelRef = panelRefs.current[panelId]
    if (!panelRef) return
    panelRef.expand()
  }

  const onPanelCollapse = (panelId: string) => {
    setCollapsedPanels(prev => new Set(prev).add(panelId))
  }

  const onPanelExpand = (panelId: string) => {
    setCollapsedPanels(prev => {
      const next = new Set(prev)
      next.delete(panelId)
      return next
    })
  }

  return {
    collapsedPanels,
    panelRefs,
    handleToggleCollapse,
    handleCollapsedTabClick,
    onPanelCollapse,
    onPanelExpand,
  }
}
