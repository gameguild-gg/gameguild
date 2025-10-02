"use client"

import * as React from "react"
import { useState } from "react"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Cloud, Database, HardDrive, Settings, CheckCircle } from "lucide-react"
import { GoogleDriveAuthDialog } from "./google-drive-auth-dialog"
import { useGoogleDriveAuth } from "@/hooks/editor/use-google-drive-auth"

export type StorageOption = "local" | "gameguild-cloud" | "google-drive"

interface StorageOptionSelectorProps {
  selectedOptions: StorageOption[]
  onSelectionChange: (options: StorageOption[]) => void
  disabled?: boolean
  required?: boolean
  className?: string
  onGoogleDriveConfigured?: () => void
}

interface StorageOptionConfig {
  id: StorageOption
  label: string
  description: string
  icon: React.ReactNode
  enabled: boolean
  comingSoon?: boolean
}

const storageOptions: StorageOptionConfig[] = [
  {
    id: "local",
    label: "Local Storage",
    description: "Salvo apenas no seu dispositivo",
    icon: <HardDrive className="w-3.5 h-3.5" />,
    enabled: true,
  },
  {
    id: "gameguild-cloud",
    label: "GameGuild Cloud",
    description: "Sincronizado com servidor GameGuild",
    icon: <Database className="w-3.5 h-3.5" />,
    enabled: false,
    comingSoon: true,
  },
  {
    id: "google-drive",
    label: "Google Drive",
    description: "Sincronizado com sua conta Google Drive",
    icon: <Cloud className="w-3.5 h-3.5" />,
    enabled: true, // Now enabled
    comingSoon: false,
  },
]

export function StorageOptionSelector({
  selectedOptions,
  onSelectionChange,
  disabled = false,
  required = true,
  className = "",
  onGoogleDriveConfigured,
}: StorageOptionSelectorProps) {
  const [showGoogleDriveAuth, setShowGoogleDriveAuth] = useState(false)
  const { hasValidSetup: isGoogleDriveConfigured } = useGoogleDriveAuth()
  const handleOptionToggle = (optionId: StorageOption, checked: boolean) => {
    if (disabled) return

    // Special handling for Google Drive
    if (optionId === "google-drive" && checked && !isGoogleDriveConfigured) {
      setShowGoogleDriveAuth(true)
      return
    }

    let newOptions: StorageOption[]
    
    if (checked) {
      // Add option if not already present
      newOptions = selectedOptions.includes(optionId) 
        ? selectedOptions 
        : [...selectedOptions, optionId]
    } else {
      // Remove option, but ensure at least one is selected if required
      newOptions = selectedOptions.filter(opt => opt !== optionId)
      
      // If required and trying to uncheck the last option, prevent it
      if (required && newOptions.length === 0) {
        return
      }
    }
    
    onSelectionChange(newOptions)
  }

  const handleGoogleDriveAuthSuccess = () => {
    // Add Google Drive to selected options if not already present
    if (!selectedOptions.includes("google-drive")) {
      onSelectionChange([...selectedOptions, "google-drive"])
    }
    onGoogleDriveConfigured?.()
  }

  return (
    <div className={`space-y-2 ${className}`}>
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">
          Storage Options {required && <span className="text-red-500">*</span>}
        </Label>
        {required && selectedOptions.length === 0 && (
          <span className="text-xs text-red-500">Selecione pelo menos uma opção</span>
        )}
      </div>
      
      <div className="space-y-1.5">
        {storageOptions.map((option) => {
          const isSelected = selectedOptions.includes(option.id)
          const isDisabled = disabled || (!option.enabled && option.id !== "google-drive")
          const isGoogleDrive = option.id === "google-drive"
          const needsConfiguration = isGoogleDrive && !isGoogleDriveConfigured
          
          return (
            <Card 
              key={option.id} 
              className={`transition-all duration-200 ${
                isSelected 
                  ? "border-blue-500 bg-blue-50 dark:bg-blue-950/20" 
                  : "border-gray-200 dark:border-gray-700"
              } ${isDisabled ? "opacity-50" : "hover:border-gray-300 dark:hover:border-gray-600"}`}
            >
              <CardContent className="p-2">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id={option.id}
                    checked={isSelected}
                    onCheckedChange={(checked) => 
                      handleOptionToggle(option.id, checked as boolean)
                    }
                    disabled={isDisabled}
                    className="shrink-0"
                  />
                  
                  <div className={isSelected ? "text-blue-600 dark:text-blue-400" : "text-gray-500"}>
                    {option.icon}
                  </div>
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between">
                      <Label 
                        htmlFor={option.id}
                        className={`text-sm font-medium cursor-pointer block ${
                          isDisabled ? "cursor-not-allowed" : ""
                        } ${isSelected ? "text-blue-900 dark:text-blue-100" : ""}`}
                      >
                        {option.label}
                        {option.comingSoon && (
                          <span className="ml-1 inline-flex items-center px-1.5 py-0.5 rounded-full text-xs bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200">
                            Em breve
                          </span>
                        )}
                        {isGoogleDrive && isGoogleDriveConfigured && (
                          <CheckCircle className="inline-block w-3 h-3 ml-1 text-green-600" />
                        )}
                      </Label>
                      
                      {/* Configuration button for Google Drive */}
                      {isGoogleDrive && (needsConfiguration || isGoogleDriveConfigured) && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setShowGoogleDriveAuth(true)}
                          className="h-6 px-2 text-xs"
                        >
                          <Settings className="w-3 h-3 mr-1" />
                          {needsConfiguration ? "Configurar" : "Reconfigurar"}
                        </Button>
                      )}
                    </div>
                    
                    <p className={`text-xs truncate ${
                      isSelected 
                        ? "text-blue-700 dark:text-blue-300" 
                        : "text-gray-500 dark:text-gray-400"
                    }`}>
                      {option.description}
                      {isGoogleDrive && needsConfiguration && (
                        <span className="text-amber-600 dark:text-amber-400 ml-1">
                          (Configuração necessária)
                        </span>
                      )}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>
      
      {selectedOptions.length > 1 && (
        <div className="p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
          <div className="flex items-center gap-2">
            <Cloud className="w-3 h-3 text-blue-600 dark:text-blue-400" />
            <span className="text-xs font-medium text-blue-800 dark:text-blue-200">
              Múltiplos destinos selecionados
            </span>
          </div>
          <p className="text-xs text-blue-700 dark:text-blue-300 mt-1">
            O projeto será salvo em todos os destinos selecionados e mantido sincronizado.
          </p>
        </div>
      )}
      
      {/* Google Drive Authentication Dialog */}
      <GoogleDriveAuthDialog
        open={showGoogleDriveAuth}
        onOpenChange={setShowGoogleDriveAuth}
        onAuthSuccess={handleGoogleDriveAuthSuccess}
      />
    </div>
  )
}
