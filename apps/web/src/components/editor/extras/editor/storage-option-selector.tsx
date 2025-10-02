"use client"

import * as React from "react"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { Card, CardContent } from "@/components/ui/card"
import { Cloud, Database, HardDrive } from "lucide-react"

export type StorageOption = "local" | "gameguild-cloud" | "google-drive"

interface StorageOptionSelectorProps {
  selectedOptions: StorageOption[]
  onSelectionChange: (options: StorageOption[]) => void
  disabled?: boolean
  required?: boolean
  className?: string
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
    icon: <HardDrive className="w-4 h-4" />,
    enabled: true,
  },
  {
    id: "gameguild-cloud",
    label: "GameGuild Cloud",
    description: "Sincronizado com servidor GameGuild",
    icon: <Database className="w-4 h-4" />,
    enabled: false,
    comingSoon: true,
  },
  {
    id: "google-drive",
    label: "Google Drive",
    description: "Sincronizado com sua conta Google Drive",
    icon: <Cloud className="w-4 h-4" />,
    enabled: false,
    comingSoon: true,
  },
]

export function StorageOptionSelector({
  selectedOptions,
  onSelectionChange,
  disabled = false,
  required = true,
  className = "",
}: StorageOptionSelectorProps) {
  const handleOptionToggle = (optionId: StorageOption, checked: boolean) => {
    if (disabled) return

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

  return (
    <div className={`space-y-3 ${className}`}>
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">
          Storage Options {required && <span className="text-red-500">*</span>}
        </Label>
        {required && selectedOptions.length === 0 && (
          <span className="text-xs text-red-500">Selecione pelo menos uma opção</span>
        )}
      </div>
      
      <div className="space-y-2">
        {storageOptions.map((option) => {
          const isSelected = selectedOptions.includes(option.id)
          const isDisabled = disabled || !option.enabled
          
          return (
            <Card 
              key={option.id} 
              className={`transition-all duration-200 ${
                isSelected 
                  ? "border-blue-500 bg-blue-50 dark:bg-blue-950/20" 
                  : "border-gray-200 dark:border-gray-700"
              } ${isDisabled ? "opacity-50" : "hover:border-gray-300 dark:hover:border-gray-600"}`}
            >
              <CardContent className="p-3">
                <div className="flex items-start space-x-3">
                  <Checkbox
                    id={option.id}
                    checked={isSelected}
                    onCheckedChange={(checked) => 
                      handleOptionToggle(option.id, checked as boolean)
                    }
                    disabled={isDisabled}
                    className="mt-0.5"
                  />
                  
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      <div className={isSelected ? "text-blue-600 dark:text-blue-400" : "text-gray-500"}>
                        {option.icon}
                      </div>
                      <Label 
                        htmlFor={option.id}
                        className={`text-sm font-medium cursor-pointer ${
                          isDisabled ? "cursor-not-allowed" : ""
                        } ${isSelected ? "text-blue-900 dark:text-blue-100" : ""}`}
                      >
                        {option.label}
                        {option.comingSoon && (
                          <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200">
                            Em breve
                          </span>
                        )}
                      </Label>
                    </div>
                    <p className={`text-xs ${
                      isSelected 
                        ? "text-blue-700 dark:text-blue-300" 
                        : "text-gray-500 dark:text-gray-400"
                    }`}>
                      {option.description}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>
      
      {selectedOptions.length > 1 && (
        <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
          <div className="flex items-center gap-2 mb-1">
            <Cloud className="w-4 h-4 text-blue-600 dark:text-blue-400" />
            <span className="text-sm font-medium text-blue-800 dark:text-blue-200">
              Múltiplos destinos selecionados
            </span>
          </div>
          <p className="text-xs text-blue-700 dark:text-blue-300">
            O projeto será salvo em todos os destinos selecionados e mantido sincronizado.
          </p>
        </div>
      )}
    </div>
  )
}
