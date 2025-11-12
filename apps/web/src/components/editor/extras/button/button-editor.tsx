"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { X, Save, MousePointerClick, Eye, ExternalLink, Download, Copy, Mail, ArrowRight } from "lucide-react"
import type { ButtonData, ButtonActionType, ButtonVariant, ButtonSize } from "@/components/editor/nodes/button-node"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

interface ButtonEditorProps {
  initialData?: ButtonData
  onSave: (data: ButtonData) => void
  onCancel: () => void
}

const actionTypes: { value: ButtonActionType; label: string; icon: React.ReactNode }[] = [
  { value: "url", label: "Open Page", icon: <ExternalLink className="h-4 w-4" /> },
  { value: "download", label: "Download File", icon: <Download className="h-4 w-4" /> },
  { value: "copy", label: "Copy Text", icon: <Copy className="h-4 w-4" /> },
  { value: "email", label: "Send Email", icon: <Mail className="h-4 w-4" /> },
]

const variants: { value: ButtonVariant; label: string; description: string }[] = [
  { value: "default", label: "Default", description: "Primary button with solid background" },
  { value: "secondary", label: "Secondary", description: "Muted alternative to default" },
  { value: "destructive", label: "Destructive", description: "For dangerous actions" },
  { value: "outline", label: "Outline", description: "Border with transparent background" },
  { value: "ghost", label: "Ghost", description: "Minimal styling, subtle hover" },
  { value: "link", label: "Link", description: "Styled as hyperlink" },
]

const sizes: { value: ButtonSize; label: string }[] = [
  { value: "sm", label: "Small" },
  { value: "default", label: "Medium" },
  { value: "lg", label: "Large" },
]

export function ButtonEditor({ initialData, onSave, onCancel }: ButtonEditorProps) {
  const [data, setData] = useState<ButtonData>(
    initialData || {
      text: "Click me",
      url: "",
      actionType: "url",
      variant: "default",
      size: "default",
      showIcon: true,
    }
  )

  // Block body scroll and pointer events when modal is open
  useEffect(() => {
    const originalOverflow = document.body.style.overflow
    const originalPointerEvents = document.body.style.pointerEvents

    document.body.style.overflow = "hidden"
    document.body.style.pointerEvents = "none"

    return () => {
      document.body.style.overflow = originalOverflow
      document.body.style.pointerEvents = originalPointerEvents
    }
  }, [])

  const handleSave = () => {
    // Restore body styles before closing
    document.body.style.overflow = ""
    document.body.style.pointerEvents = ""

    onSave(data)
  }

  const handleCancel = () => {
    // Restore body styles before closing
    document.body.style.overflow = ""
    document.body.style.pointerEvents = ""

    onCancel()
  }

  const getActionIcon = () => {
    const action = actionTypes.find((a) => a.value === data.actionType)
    return action?.icon || <ArrowRight className="h-4 w-4" />
  }

  const getUrlPlaceholder = () => {
    switch (data.actionType) {
      case "url":
        return "https://example.com"
      case "download":
        return "https://example.com/file.pdf"
      case "copy":
        return "Text to be copied"
      case "email":
        return "email@example.com"
      default:
        return ""
    }
  }

  const getUrlLabel = () => {
    switch (data.actionType) {
      case "url":
        return "Page URL"
      case "download":
        return "File URL"
      case "copy":
        return "Text to Copy"
      case "email":
        return "Email Address"
      default:
        return "URL"
    }
  }

  return (
    <div
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      style={{ pointerEvents: "auto" }}
      onClick={handleCancel}
      onMouseDown={(e) => e.stopPropagation()}
      onKeyDown={(e) => {
        if (e.key === "Escape") {
          handleCancel()
        }
        e.stopPropagation()
      }}
    >
      <div
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-6xl h-[85vh] flex flex-col"
        style={{ pointerEvents: "auto" }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
        onKeyUp={(e) => e.stopPropagation()}
        onKeyPress={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <MousePointerClick className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Button Editor</h2>

            {/* Current settings display */}
            <div className="ml-4 flex items-center gap-3 pl-4 border-l border-gray-300 dark:border-gray-600">
              <div className="flex items-center gap-2 text-sm">
                <span className="text-gray-600 dark:text-gray-400">Action:</span>
                <div className="flex items-center gap-1 font-medium text-gray-800 dark:text-gray-200 capitalize bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                  {actionTypes.find((a) => a.value === data.actionType)?.icon}
                  <span>{actionTypes.find((a) => a.value === data.actionType)?.label}</span>
                </div>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <span className="text-gray-600 dark:text-gray-400">Style:</span>
                <span className="font-medium text-gray-800 dark:text-gray-200 capitalize bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                  {variants.find((v) => v.value === data.variant)?.label}
                </span>
              </div>
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleCancel}
            className="hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Settings Bar */}
        <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Action Type:
            </Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="gap-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  {actionTypes.find((a) => a.value === data.actionType)?.icon}
                  <span>{actionTypes.find((a) => a.value === data.actionType)?.label}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700">
                {actionTypes.map((action) => (
                  <DropdownMenuItem
                    key={action.value}
                    onClick={() => setData((prev) => ({ ...prev, actionType: action.value }))}
                    className="cursor-pointer dark:hover:bg-gray-700 dark:focus:bg-gray-700"
                  >
                    <div className="flex items-center gap-2">
                      {action.icon}
                      <span>{action.label}</span>
                    </div>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex items-center gap-2">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Style:
            </Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="gap-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  <span className="capitalize">{variants.find((v) => v.value === data.variant)?.label}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-[320px] bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 p-2">
                <div className="space-y-1">
                  {variants.map((variant) => (
                    <button
                      key={variant.value}
                      onClick={() => setData((prev) => ({ ...prev, variant: variant.value }))}
                      className={`w-full text-left p-2 rounded transition-all hover:bg-gray-100 dark:hover:bg-gray-700 ${
                        data.variant === variant.value
                          ? "bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800"
                          : "border border-transparent"
                      }`}
                    >
                      <div className="font-medium text-sm text-gray-900 dark:text-gray-100">
                        {variant.label}
                      </div>
                      <div className="text-xs text-gray-500 dark:text-gray-400">
                        {variant.description}
                      </div>
                    </button>
                  ))}
                </div>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex items-center gap-2">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Size:
            </Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="gap-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  <span>{sizes.find((s) => s.value === data.size)?.label}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700">
                {sizes.map((size) => (
                  <DropdownMenuItem
                    key={size.value}
                    onClick={() => setData((prev) => ({ ...prev, size: size.value }))}
                    className="cursor-pointer dark:hover:bg-gray-700 dark:focus:bg-gray-700"
                  >
                    {size.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        {/* Editor Content */}
        <div className="flex-1 flex min-h-0">
          {/* Left Panel - Editor */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col bg-white dark:bg-gray-900">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                <MousePointerClick className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Button Settings
              </h3>
            </div>

            <div className="flex-1 p-6 overflow-auto bg-white dark:bg-gray-950">
              <div className="space-y-6">
                {/* Button Text */}
                <div className="space-y-2">
                  <Label htmlFor="text" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Button Text *
                  </Label>
                  <Input
                    id="text"
                    value={data.text}
                    onChange={(e) => setData((prev) => ({ ...prev, text: e.target.value }))}
                    placeholder="Click me"
                    className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                  />
                </div>

                {/* URL/Target */}
                <div className="space-y-2">
                  <Label htmlFor="url" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    {getUrlLabel()}
                  </Label>
                  <Input
                    id="url"
                    value={data.url}
                    onChange={(e) => setData((prev) => ({ ...prev, url: e.target.value }))}
                    placeholder={getUrlPlaceholder()}
                    className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                  />
                </div>

                {/* Show Icon Toggle */}
                <div className="flex items-center space-x-3 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                  <Switch
                    id="show-icon"
                    checked={data.showIcon}
                    onCheckedChange={(checked) => setData((prev) => ({ ...prev, showIcon: checked }))}
                  />
                  <Label htmlFor="show-icon" className="text-sm font-medium text-gray-700 dark:text-gray-300 cursor-pointer">
                    Show action icon
                  </Label>
                </div>

                {/* Info Section */}
                <div className="p-4 bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 rounded-lg">
                  <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-2">
                    Action Behavior
                  </h4>
                  <p className="text-xs text-blue-700 dark:text-blue-300">
                    {data.actionType === "url" && "Opens the specified URL in a new tab"}
                    {data.actionType === "download" && "Downloads the file from the specified URL"}
                    {data.actionType === "copy" && "Copies the specified text to clipboard"}
                    {data.actionType === "email" && "Opens default email client with the specified address"}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Right Panel - Preview */}
          <div className="w-1/2 flex flex-col bg-white dark:bg-gray-900">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                <Eye className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Live Preview
              </h3>
            </div>
            <div className="flex-1 p-8 overflow-auto bg-white dark:bg-gray-950 flex items-center justify-center">
              <div className="space-y-4 w-full max-w-md">
                <div className="text-center text-sm text-gray-600 dark:text-gray-400 mb-8">
                  Button appearance in your content
                </div>
                <div className="flex justify-center">
                  <Button variant={data.variant} size={data.size} className="pointer-events-none">
                    {data.text}
                    {data.showIcon && <span className="ml-2">{getActionIcon()}</span>}
                  </Button>
                </div>
                
                {/* Visual indicators */}
                <div className="mt-8 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                  <div className="space-y-2 text-xs">
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Text:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{data.text || "(empty)"}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Action:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{actionTypes.find((a) => a.value === data.actionType)?.label}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Style:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{variants.find((v) => v.value === data.variant)?.label}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Size:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{sizes.find((s) => s.value === data.size)?.label}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Icon:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{data.showIcon ? "Visible" : "Hidden"}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center justify-end gap-2">
            <Button
              variant="outline"
              onClick={handleCancel}
              className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
            >
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
            >
              <Save className="h-4 w-4" />
              Save Button
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
