"use client"

import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { X, Lock, Unlock, Hash, Type, Palette, Maximize2 } from "lucide-react"
import type { CodeStudioData, ShikiTheme } from "./types"
import { SHIKI_THEME_CONFIGS } from "./types"
import { 
  ModalSize, 
  getEditorPreferences, 
  setGlobalPreference, 
  setNodeTypePreference,
  hasNodeTypePreference,
  MODAL_SIZE_LABELS 
} from "@/lib/storage/editor/editor-preferences"
import { useState, useEffect, useRef } from "react"

interface SettingsMenuProps {
  data: CodeStudioData
  onDataChange: (newData: Partial<CodeStudioData>) => void
  onClose: () => void
  nodeType?: string
  onModalSizeChange?: (size: ModalSize) => void
}

export function SettingsMenu({ data, onDataChange, onClose, nodeType = 'code-studio', onModalSizeChange }: SettingsMenuProps) {
  const [modalSize, setModalSize] = useState<ModalSize>('widescreen')
  const [applyToAllNodes, setApplyToAllNodes] = useState(true)
  const [isLoading, setIsLoading] = useState(true)
  const timeoutRef = useRef<NodeJS.Timeout | null>(null)
  
  useEffect(() => {
    // Load preferences whenever menu opens
    const loadPreferences = async () => {
      setIsLoading(true)
      
      // Get current modal size
      const prefs = await getEditorPreferences(nodeType)
      setModalSize(prefs.modalSize)
      
      // Check if there's a node-specific preference
      const hasNodeSpecific = await hasNodeTypePreference(nodeType, 'modalSize')
      setApplyToAllNodes(!hasNodeSpecific) // If has node-specific, switch is OFF
      
      setIsLoading(false)
    }
    
    loadPreferences()
  }, [nodeType])
  
  useEffect(() => {
    // Cleanup timeout on unmount
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current)
      }
    }
  }, [])
  
  const handleModalSizeChange = async (size: ModalSize) => {
    setModalSize(size)
    
    // Clear previous timeout
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current)
    }
    
    // Delay actual save and notification by 250ms
    timeoutRef.current = setTimeout(async () => {
      if (applyToAllNodes) {
        await setGlobalPreference('modalSize', size)
      } else {
        await setNodeTypePreference(nodeType, 'modalSize', size)
      }
      
      // Notify parent component after delay
      onModalSizeChange?.(size)
    }, 250)
  }
  
  return (
    <div className="absolute top-10 left-0 z-50 w-72 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg">
      <div className="p-4 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">Settings</h3>
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-6 w-6 p-0"
          >
            <X className="h-3 w-3" />
          </Button>
        </div>
        
        <div className="space-y-3 border-t border-gray-200 dark:border-gray-700 pt-3">
          {/* Read Only Global */}
          <div className="space-y-1">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                {data.readonly ? (
                  <Lock className="h-4 w-4 text-red-500" />
                ) : (
                  <Unlock className="h-4 w-4 text-green-500" />
                )}
                <Label htmlFor="readonly" className="text-sm font-medium cursor-pointer">
                  Read Only Outside Editor
                </Label>
              </div>
              <Switch
                id="readonly"
                checked={data.readonly || false}
                onCheckedChange={(checked) => onDataChange({ readonly: checked })}
              />
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 ml-6">
              When enabled, code is not editable in preview mode
            </p>
          </div>
          
          {/* Show Line Numbers */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Hash className="h-4 w-4 text-blue-500" />
              <Label htmlFor="lineNumbers" className="text-sm font-medium cursor-pointer">
                Line Numbers
              </Label>
            </div>
            <Switch
              id="lineNumbers"
              checked={data.showLineNumbers ?? true}
              onCheckedChange={(checked) => onDataChange({ showLineNumbers: checked })}
            />
          </div>
          
          {/* Font Size */}
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <Type className="h-4 w-4 text-purple-500" />
              <Label htmlFor="fontSize" className="text-sm font-medium">
                Font Size: {data.fontSize || 14}px
              </Label>
            </div>
            <input
              id="fontSize"
              type="range"
              min="10"
              max="24"
              value={data.fontSize || 14}
              onChange={(e) => onDataChange({ fontSize: parseInt(e.target.value) })}
              className="w-full"
            />
          </div>
          
          {/* Shiki Theme */}
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <Palette className="h-4 w-4 text-indigo-500" />
              <Label htmlFor="shikiTheme" className="text-sm font-medium">
                Syntax Theme
              </Label>
            </div>
            <select
              id="shikiTheme"
              value={data.shikiTheme || "github"}
              onChange={(e) => onDataChange({ shikiTheme: e.target.value as ShikiTheme })}
              className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
            >
              {Object.entries(SHIKI_THEME_CONFIGS).map(([key, config]) => (
                <option key={key} value={key}>
                  {config.label}
                </option>
              ))}
            </select>
          </div>
          
          {/* Modal Size */}
          <div className="space-y-3 border-t border-gray-200 dark:border-gray-700 pt-3">
            <div className="flex items-center gap-2">
              <Maximize2 className="h-4 w-4 text-orange-500" />
              <Label className="text-sm font-medium">
                Modal Size: {MODAL_SIZE_LABELS[modalSize]}
              </Label>
            </div>
            
            <div className="space-y-2">
              <input
                type="range"
                min="0"
                max="3"
                step="1"
                value={['compact', 'widescreen', 'ultrawide', 'fullscreen'].indexOf(modalSize)}
                onChange={(e) => {
                  const sizes: ModalSize[] = ['compact', 'widescreen', 'ultrawide', 'fullscreen']
                  const selectedSize = sizes[parseInt(e.target.value)]
                  if (selectedSize) {
                    handleModalSizeChange(selectedSize)
                  }
                }}
                className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-orange-500"
                style={{
                  background: `linear-gradient(to right, 
                    rgb(249, 115, 22) 0%, 
                    rgb(249, 115, 22) ${(['compact', 'widescreen', 'ultrawide', 'fullscreen'].indexOf(modalSize) / 3) * 100}%, 
                    rgb(229, 231, 235) ${(['compact', 'widescreen', 'ultrawide', 'fullscreen'].indexOf(modalSize) / 3) * 100}%, 
                    rgb(229, 231, 235) 100%)`
                }}
              />
              <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 px-1">
                <span>Compact</span>
                <span>Wide</span>
                <span>Ultra</span>
                <span>Full</span>
              </div>
            </div>
            
            <div className="flex items-center justify-between pt-2 border-t border-gray-200 dark:border-gray-700">
              <div className="flex flex-col gap-0.5">
                <Label htmlFor="applyToAll" className="text-xs font-medium text-gray-700 dark:text-gray-300 cursor-pointer">
                  Scope
                </Label>
                <span className="text-xs text-gray-500 dark:text-gray-400">
                  {applyToAllNodes 
                    ? "All projects & nodes" 
                    : "Code Studio nodes only"}
                </span>
              </div>
              <Switch
                id="applyToAll"
                checked={applyToAllNodes}
                onCheckedChange={setApplyToAllNodes}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
