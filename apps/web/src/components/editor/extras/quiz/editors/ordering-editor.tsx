/**
 * Ordering Editor
 * Configure items to be ordered
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, Trash2, GripVertical } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import type { OrderingEntry } from "../types"

export function OrderingEditor() {
  const { register, control, watch, setValue } = useFormContext<OrderingEntry>()
  const { fields, append, remove, move } = useFieldArray({
    control,
    name: "items",
  })

  const allowPartialCredit = watch("allowPartialCredit")

  const addItem = () => {
    append({
      id: Math.random().toString(36).substring(7),
      text: "",
      correctPosition: fields.length,
    })
  }

  const handleMoveUp = (index: number) => {
    if (index > 0) {
      move(index, index - 1)
      // Update correctPosition for affected items
      const items = watch("items")
      items.forEach((item, i) => {
        setValue(`items.${i}.correctPosition`, i)
      })
    }
  }

  const handleMoveDown = (index: number) => {
    if (index < fields.length - 1) {
      move(index, index + 1)
      // Update correctPosition for affected items
      const items = watch("items")
      items.forEach((item, i) => {
        setValue(`items.${i}.correctPosition`, i)
      })
    }
  }

  return (
    <div className="space-y-4">
      <div className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-3 rounded-lg border border-blue-200 dark:border-blue-800">
        <p>Enter items in the <strong>correct order</strong>. They will be shuffled when presented to students.</p>
      </div>

      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Items (in correct order)
        </Label>
        <Button type="button" variant="outline" size="sm" onClick={addItem}>
          <Plus className="h-4 w-4 mr-1" />
          Add Item
        </Button>
      </div>

      {fields.length === 0 && (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
          <p>No items configured yet.</p>
          <p className="text-sm">Click &quot;Add Item&quot; to create items.</p>
        </div>
      )}

      {fields.map((field, index) => (
        <div
          key={field.id}
          className="flex items-center gap-2 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border"
        >
          <div className="flex flex-col gap-1">
            <button
              type="button"
              onClick={() => handleMoveUp(index)}
              disabled={index === 0}
              className={`p-1 rounded hover:bg-gray-200 ${index === 0 ? "opacity-30" : ""}`}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
              </svg>
            </button>
            <button
              type="button"
              onClick={() => handleMoveDown(index)}
              disabled={index === fields.length - 1}
              className={`p-1 rounded hover:bg-gray-200 ${index === fields.length - 1 ? "opacity-30" : ""}`}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          </div>

          <span className="w-8 h-8 flex items-center justify-center bg-gray-200 rounded-full text-sm font-medium">
            {index + 1}
          </span>

          <Input
            placeholder="Enter item text"
            {...register(`items.${index}.text`, { required: true })}
            autoComplete="off"
            className="flex-1 bg-white dark:bg-gray-800"
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
