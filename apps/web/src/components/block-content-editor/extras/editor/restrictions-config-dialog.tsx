"use client"

import { useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { X, Plus, Shield } from "lucide-react"
import {
  type NodeRestrictions,
  setBlockRestriction,
  removeBlockRestriction,
  getRestrictions,
  describeRestrictions,
} from "@/components/block-content-editor/lib/storage/editor/project-modes"
import { toast } from "sonner"

interface RestrictionsConfigDialogProps {
  open?: boolean
  onOpenChange?: (open: boolean) => void
  currentRestrictions?: NodeRestrictions
  onRestrictionsChange: (restrictions: NodeRestrictions) => void
  availableBlocks: string[]
  availablePanels: string[]
}

const availableNodeTypes = [
  "code-studio",
  "quiz",
  "image",
  "video",
  "audio",
  "youtube",
  "spotify",
  "gallery",
  "mermaid",
  "vega-lite",
  "table",
]

export function RestrictionsConfigDialog({
  open,
  onOpenChange,
  currentRestrictions,
  onRestrictionsChange,
  availableBlocks,
  availablePanels,
}: RestrictionsConfigDialogProps) {
  const [selectedTarget, setSelectedTarget] = useState<"block" | "panel">("block")
  const [selectedId, setSelectedId] = useState<string>("")
  const [restrictionType, setRestrictionType] = useState<"allow" | "block">("allow")
  const [selectedNodes, setSelectedNodes] = useState<string[]>([])

  const handleAddRestriction = () => {
    if (!selectedId) {
      toast.error("Please select a block or panel")
      return
    }

    if (selectedNodes.length === 0) {
      toast.error("Please select at least one node type")
      return
    }

    let newRestrictions = currentRestrictions || { blocks: {}, panels: {} }

    if (selectedTarget === "block") {
      if (restrictionType === "allow") {
        // Only these nodes allowed
        newRestrictions = setBlockRestriction(newRestrictions, selectedId, "*", selectedNodes)
      } else {
        // These nodes blocked
        newRestrictions = setBlockRestriction(newRestrictions, selectedId, selectedNodes, null)
      }
    }

    onRestrictionsChange(newRestrictions)
    
    // Reset form
    setSelectedNodes([])
    setSelectedId("")
    
    toast.success("Restriction added", {
      description: `${restrictionType === "allow" ? "Allowed" : "Blocked"} nodes configured for ${selectedTarget} ${selectedId}`,
      duration: 2000,
    })
  }

  const handleRemoveRestriction = (target: "block" | "panel", id: string) => {
    let newRestrictions = currentRestrictions || { blocks: {}, panels: {} }

    if (target === "block") {
      newRestrictions = removeBlockRestriction(newRestrictions, id)
    }

    onRestrictionsChange(newRestrictions)
    toast.success("Restriction removed")
  }

  const toggleNodeSelection = (nodeType: string) => {
    setSelectedNodes(prev =>
      prev.includes(nodeType)
        ? prev.filter(n => n !== nodeType)
        : [...prev, nodeType]
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2">
          <Shield className="h-4 w-4" />
          Restrictions
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Node Restrictions Configuration</DialogTitle>
          <DialogDescription>
            Configure which node types are allowed or blocked in specific blocks or panels.
            Panel restrictions apply to all blocks within that panel.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6">
          {/* Current Restrictions */}
          <div className="space-y-2">
            <h3 className="text-sm font-medium">Current Restrictions</h3>
            <div className="space-y-2">
              {/* Block Restrictions */}
              {currentRestrictions?.blocks && Object.entries(currentRestrictions.blocks).length > 0 && (
                <div className="space-y-1">
                  <p className="text-xs text-muted-foreground">Block Restrictions:</p>
                  {Object.entries(currentRestrictions.blocks).map(([blockId, restriction]) => (
                    <div key={blockId} className="flex items-center justify-between p-2 border rounded-lg">
                      <div>
                        <span className="font-medium text-sm">Block {blockId}</span>
                        <p className="text-xs text-muted-foreground">{describeRestrictions(restriction)}</p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleRemoveRestriction("block", blockId)}
                        className="h-7 w-7 p-0"
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              )}

              {(!currentRestrictions?.blocks || Object.keys(currentRestrictions.blocks).length === 0) && (
                <p className="text-sm text-muted-foreground">No custom restrictions configured</p>
              )}
            </div>
          </div>

          {/* Add New Restriction */}
          <div className="space-y-4 border-t pt-4">
            <h3 className="text-sm font-medium">Add New Restriction</h3>

            {/* Target Selection */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Target Type</Label>
                <Select value={selectedTarget} onValueChange={(v) => setSelectedTarget(v as "block" | "panel")}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="block">Block</SelectItem>
                    <SelectItem value="panel">Panel</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label>{selectedTarget === "block" ? "Block" : "Panel"} ID</Label>
                <Select value={selectedId} onValueChange={setSelectedId}>
                  <SelectTrigger>
                    <SelectValue placeholder={`Select ${selectedTarget}...`} />
                  </SelectTrigger>
                  <SelectContent>
                    {(selectedTarget === "block" ? availableBlocks : availablePanels).map(id => (
                      <SelectItem key={id} value={id}>
                        {id}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Restriction Type */}
            <div className="space-y-2">
              <Label>Restriction Type</Label>
              <Select value={restrictionType} onValueChange={(v) => setRestrictionType(v as "allow" | "block")}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="allow">Allow Only (block all others)</SelectItem>
                  <SelectItem value="block">Block (allow all others)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Node Type Selection */}
            <div className="space-y-2">
              <Label>Node Types</Label>
              <div className="flex flex-wrap gap-2">
                {availableNodeTypes.map(nodeType => (
                  <Badge
                    key={nodeType}
                    variant={selectedNodes.includes(nodeType) ? "default" : "outline"}
                    className="cursor-pointer"
                    onClick={() => toggleNodeSelection(nodeType)}
                  >
                    {nodeType}
                  </Badge>
                ))}
              </div>
            </div>

            <Button onClick={handleAddRestriction} className="w-full">
              <Plus className="h-4 w-4 mr-2" />
              Add Restriction
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
