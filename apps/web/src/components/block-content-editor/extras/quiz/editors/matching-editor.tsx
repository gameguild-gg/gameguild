/**
 * Matching Editor
 * Configure matching pairs
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import type { MatchingEntry } from "../types"

export function MatchingEditor() {
  const { register, control, watch, setValue } = useFormContext<MatchingEntry>()
  const { fields, append, remove } = useFieldArray({
    control,
    name: "pairs",
  })

  const allowPartialCredit = watch("allowPartialCredit")

  const addPair = () => {
    append({
      id: Math.random().toString(36).substring(7),
      left: "",
      right: "",
    })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Matching Pairs
        </Label>
        <Button type="button" variant="outline" size="sm" onClick={addPair}>
          <Plus className="h-4 w-4 mr-1" />
          Add Pair
        </Button>
      </div>

      {fields.length === 0 && (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
          <p>No pairs configured yet.</p>
          <p className="text-sm">Click &quot;Add Pair&quot; to create matching pairs.</p>
        </div>
      )}

      {fields.map((field, index) => (
        <div
          key={field.id}
          className="grid grid-cols-[1fr,auto,1fr,auto] items-center gap-2 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border"
        >
          <Input
            placeholder="Left item"
            {...register(`pairs.${index}.left`, { required: true })}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
          <span className="text-gray-400">↔</span>
          <Input
            placeholder="Right item"
            {...register(`pairs.${index}.right`, { required: true })}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => remove(index)}
            className="text-red-600 hover:text-red-700 hover:bg-red-50"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ))}

      <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
        <Label className="text-sm">Allow Partial Credit</Label>
        <Switch
          checked={allowPartialCredit}
          onCheckedChange={(checked) => setValue("allowPartialCredit", checked)}
        />
      </div>
    </div>
  )
}
