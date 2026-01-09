"use client"

import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Maximize2 } from "lucide-react"
import { 
  ModalSize, 
  getEditorPreferences, 
  setGlobalPreference, 
  setNodeTypePreference,
  hasNodeTypePreference,
  MODAL_SIZE_LABELS 
} from "@/lib/storage/editor/editor-preferences"
import { useState, useEffect, useRef } from "react"

interface SystemSettingsProps {
  nodeType?: string
  onModalSizeChange?: (size: ModalSize) => void
}

export function SystemSettings({ nodeType = 'code-studio', onModalSizeChange }: SystemSettingsProps) {
  const [modalSize, setModalSize] = useState<ModalSize>('widescreen')
  const [applyToAllNodes, setApplyToAllNodes] = useState(true)
  const [isLoading, setIsLoading] = useState(true)
  const timeoutRef = useRef<NodeJS.Timeout | null>(null)
  
  useEffect(() => {
    // Load preferences whenever component mounts
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
  
  if (isLoading) {
    return <div className="text-sm text-gray-500">Loading...</div>
  }
  
  return (
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
  )
}
