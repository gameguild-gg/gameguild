"use client"

import { useState, useEffect } from "react"
import { type ModalSize, getEditorPreferences, getModalSizeClasses } from "@/lib/storage/editor/editor-preferences"

export interface EditorSettings {
  nodeType: string
  showSettingsMenu: boolean
  setShowSettingsMenu: (show: boolean) => void
  modalSize: ModalSize | null
  setModalSize: (size: ModalSize) => void
  editorFontSize: number
  setEditorFontSize: (size: number) => void
  editorLineNumbers: boolean
  setEditorLineNumbers: (show: boolean) => void
  modalClassName: string
}

export function useEditorSettings(nodeType: string): EditorSettings {
  const [showSettingsMenu, setShowSettingsMenu] = useState(false)
  const [modalSize, setModalSize] = useState<ModalSize | null>(null)
  const [editorFontSize, setEditorFontSize] = useState(14)
  const [editorLineNumbers, setEditorLineNumbers] = useState(true)

  // Load modal size preference
  useEffect(() => {
    getEditorPreferences(nodeType).then((prefs) => {
      setModalSize(prefs.modalSize)
    })
  }, [nodeType])

  // Close settings menu on click outside
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      const target = e.target as HTMLElement
      if (showSettingsMenu && !target.closest('.settings-menu-container')) {
        setShowSettingsMenu(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [showSettingsMenu])

  const modalClassName = modalSize
    ? getModalSizeClasses(modalSize).modal
    : 'w-full max-w-7xl h-[90vh]'

  return {
    nodeType,
    showSettingsMenu,
    setShowSettingsMenu,
    modalSize,
    setModalSize,
    editorFontSize,
    setEditorFontSize,
    editorLineNumbers,
    setEditorLineNumbers,
    modalClassName,
  }
}
