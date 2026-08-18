/**
 * Categorization Editor
 * Configure categories and items to categorize
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, Trash2 } from "lucide-react"
import { Button } from "@game-guild/ui/components/button"
import { Input } from "@game-guild/ui/components/input"
import { Label } from "@game-guild/ui/components/label"
import { Textarea } from "@game-guild/ui/components/textarea"
import type { CategorizationEntry } from "@game-guild/quiz"

export function CategorizationEditor() {
  const { control, watch, register, setValue } = useFormContext<CategorizationEntry>()

  const categoriesFieldArray = useFieldArray({
    control,
    name: "categories",
  })

  const itemsFieldArray = useFieldArray({
    control,
    name: "items",
  })

  const categories = watch("categories") || []

  const handleAddCategory = () => {
    categoriesFieldArray.append({
      id: crypto.randomUUID(),
      name: "",
      description: "",
    })
  }

  const handleAddItem = () => {
    itemsFieldArray.append({
      id: crypto.randomUUID(),
      text: "",
      correctCategoryIds: [],
    })
  }

  const handleToggleCategory = (itemIndex: number, categoryId: string) => {
    const items = watch("items") || []
    const currentItem = items[itemIndex]
    if (!currentItem) return

    const currentCategoryIds = currentItem.correctCategoryIds || []
    const index = currentCategoryIds.indexOf(categoryId)

    let newCategoryIds: string[]
    if (index > -1) {
      newCategoryIds = currentCategoryIds.filter((id) => id !== categoryId)
    } else {
      newCategoryIds = [...currentCategoryIds, categoryId]
    }

    setValue(`items.${itemIndex}.correctCategoryIds`, newCategoryIds, {
      shouldDirty: true,
    })
  }

  const isCategorySelected = (itemIndex: number, categoryId: string): boolean => {
    const items = watch("items") || []
    const item = items[itemIndex]
    if (!item) return false
    return (item.correctCategoryIds || []).includes(categoryId)
  }

  return (
    <div className="space-y-6">
      {/* Categories Section */}
      <div className="border rounded-lg p-4 bg-gray-50 dark:bg-gray-800/50">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-medium text-sm">Categories</h3>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={handleAddCategory}
            className="text-blue-600 hover:text-blue-700"
          >
            <Plus className="h-3 w-3 mr-1" />
            Add Category
          </Button>
        </div>

        <div className="space-y-3">
          {categoriesFieldArray.fields.map((category, index) => (
            <div key={category.id} className="flex gap-2">
              <div className="flex-1 space-y-2">
                <Input
                  placeholder="Category name"
                  {...register(`categories.${index}.name`)}
                  autoComplete="off"
                  className="text-sm"
                />
                <Textarea
                  placeholder="Description (optional)"
                  {...register(`categories.${index}.description`)}
                  autoComplete="off"
                  className="text-sm resize-none h-10"
                />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => categoriesFieldArray.remove(index)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50 self-start"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))}
          {categoriesFieldArray.fields.length === 0 && (
            <div className="text-xs text-gray-500 dark:text-gray-400 py-2">No categories yet</div>
          )}
        </div>
      </div>

      {/* Items Section */}
      <div className="border rounded-lg p-4 bg-gray-50 dark:bg-gray-800/50">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-medium text-sm">Items to Categorize</h3>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={handleAddItem}
            className="text-blue-600 hover:text-blue-700"
          >
            <Plus className="h-3 w-3 mr-1" />
            Add Item
          </Button>
        </div>

        <div className="space-y-3">
          {itemsFieldArray.fields.map((item, itemIndex) => (
            <div key={item.id} className="border rounded p-3 bg-white dark:bg-gray-900">
              <div className="flex gap-2 mb-3">
                <Input
                  placeholder="Item text"
                  {...register(`items.${itemIndex}.text`)}
                  autoComplete="off"
                  className="text-sm flex-1"
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => itemsFieldArray.remove(itemIndex)}
                  className="text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>

              {categories.length > 0 && (
                <div>
                  <div className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-2">
                    Correct categories:
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {categories.map((category) => {
                      const isSelected = isCategorySelected(itemIndex, category.id)
                      return (
                        <label
                          key={category.id}
                          className={`
                            inline-flex items-center gap-2 px-3 py-1 border rounded cursor-pointer text-xs
                            ${isSelected
                              ? "bg-blue-100 border-blue-400 text-blue-700"
                              : "bg-white dark:bg-gray-800 border-gray-300 hover:bg-gray-50"
                            }
                          `}
                        >
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => handleToggleCategory(itemIndex, category.id)}
                            className="w-3 h-3"
                          />
                          <span>{category.name || `Category ${categories.indexOf(category) + 1}`}</span>
                        </label>
                      )
                    })}
                  </div>
                </div>
              )}
            </div>
          ))}
          {itemsFieldArray.fields.length === 0 && (
            <div className="text-xs text-gray-500 dark:text-gray-400 py-2">No items yet</div>
          )}
        </div>
      </div>
    </div>
  )
}
