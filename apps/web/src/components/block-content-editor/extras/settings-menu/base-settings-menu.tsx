"use client"

import { Button } from "@/components/ui/button"
import { X } from "lucide-react"
import { useState, type ReactNode } from "react"

export interface SettingsTab {
  id: string
  label: string
  content: ReactNode
}

interface BaseSettingsMenuProps {
  tabs: SettingsTab[]
  defaultTab?: string
  onClose: () => void
}

export function BaseSettingsMenu({ tabs, defaultTab, onClose }: BaseSettingsMenuProps) {
  const [activeTab, setActiveTab] = useState(defaultTab || tabs[0]?.id || '')
  
  return (
    <div className="absolute top-10 right-0 z-50 w-72 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg">
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
        {tabs.length > 1 && (
          <div className="flex border-b border-gray-200 dark:border-gray-700 px-4 pt-3">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`px-4 py-2 text-xs font-medium border-b-2 transition-colors ${
                  activeTab === tab.id
                    ? 'border-orange-500 text-orange-600 dark:text-orange-400'
                    : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        )}
        
        {/* Content */}
        <div className="p-4 space-y-3 overflow-y-auto">
          {tabs.find(tab => tab.id === activeTab)?.content}
        </div>
      </div>
    </div>
  )
}
