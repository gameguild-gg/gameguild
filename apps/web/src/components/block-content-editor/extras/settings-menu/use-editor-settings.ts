"use client"

import { useState, useEffect } from "react"
import { type ModalSize, getEditorPreferences, getModalSizeClasses } from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

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
  /**
   * Inner panel sizing classes (width/height) for the selected modal size.
   * Fed straight to the modal's container `<div>`.
   */
  modalClassName: string
  /**
   * Outer overlay sizing classes (padding/margin) for the selected modal
   * size. Without applying this, "fullscreen" mode still shows a frame
   * around the modal because the overlay keeps its compact padding.
   */
  containerClassName: string
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

  const modalClasses = modalSize ? getModalSizeClasses(modalSize) : null
  const modalClassName = modalClasses?.modal ?? 'w-full max-w-7xl h-[90vh]'
  const containerClassName = modalClasses?.container ?? 'p-4'

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
    containerClassName,
  }
}
