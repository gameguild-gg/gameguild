"use client"

import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { X, Hash, Type, Palette, Maximize2 } from "lucide-react"
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
  const [activeTab, setActiveTab] = useState<'general' | 'code-studio'>('code-studio')
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
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="flex items-center justify-between p-4 pb-0">
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
        
        {/* Tabs */}
        <div className="flex border-b border-gray-200 dark:border-gray-700 px-4 pt-3">
          <button
            onClick={() => setActiveTab('code-studio')}
            className={`px-4 py-2 text-xs font-medium border-b-2 transition-colors ${
              activeTab === 'code-studio'
                ? 'border-orange-500 text-orange-600 dark:text-orange-400'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
            }`}
          >
            Code Studio
          </button>
          <button
            onClick={() => setActiveTab('general')}
            className={`px-4 py-2 text-xs font-medium border-b-2 transition-colors ${
              activeTab === 'general'
                ? 'border-orange-500 text-orange-600 dark:text-orange-400'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
            }`}
          >
            General
          </button>
        </div>
        
        {/* Content */}
        <div className="p-4 space-y-3 overflow-y-auto">
          {activeTab === 'general' && (
            <>
              {/* Modal Size */}
              <div className="space-y-3">
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
            </>
          )}
          
          {activeTab === 'code-studio' && (
            <>
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
            </>
          )}
        </div>
      </div>
    </div>
  )
}
