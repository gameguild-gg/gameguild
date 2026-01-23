/**
 * Essay Editor
 * Configure essay question settings
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import type { EssayEntry } from "../types"

export function EssayEditor() {
  const { register, watch, setValue } = useFormContext<EssayEntry>()
  const showWordCount = watch("showWordCount")

  return (
    <div className="space-y-4">
      <div className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-3 rounded-lg border border-blue-200 dark:border-blue-800">
        <p>Essay questions require manual grading. Configure optional word limits below.</p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label className="text-sm font-medium">Minimum Words</Label>
          <Input
            type="number"
            placeholder="Optional"
            {...register("minWordCount", { valueAsNumber: true })}
            className="bg-white dark:bg-gray-800"
          />
        </div>
        <div className="space-y-2">
          <Label className="text-sm font-medium">Maximum Words</Label>
          <Input
            type="number"
            placeholder="Optional"
            {...register("maxWordCount", { valueAsNumber: true })}
            className="bg-white dark:bg-gray-800"
          />
        </div>
      </div>

      <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
        <Label className="text-sm">Show Word Count</Label>
        <Switch
          checked={showWordCount}
          onCheckedChange={(checked) => setValue("showWordCount", checked)}
        />
      </div>
    </div>
  )
}
