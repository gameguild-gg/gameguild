"use client"

import { useState } from "react"
import { FilePlus, Upload } from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"

interface FileSourceMenuProps {
  onCreateNew: () => void
  onAddFromAssets: () => void
  trigger?: React.ReactNode
}

export function FileSourceMenu({ 
  onCreateNew, 
  onAddFromAssets, 
  trigger 
}: FileSourceMenuProps) {
  const [open, setOpen] = useState(false)

  const handleCreateNew = () => {
    setOpen(false)
    onCreateNew()
  }

  const handleAddFromAssets = () => {
    setOpen(false)
    onAddFromAssets()
  }

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        {trigger || (
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0" title="Add File">
            <FilePlus className="h-3 w-3" />
          </Button>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56">
        <DropdownMenuItem onClick={handleCreateNew} className="cursor-pointer">
          <FilePlus className="h-4 w-4 mr-2" />
          <span>Create New File</span>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={handleAddFromAssets} className="cursor-pointer">
          <Upload className="h-4 w-4 mr-2" />
          <span>Add from Assets</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
