"use client"

import React from "react"
import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { QuizData } from "../../../nodes/quiz-node"

export function CategorizationEditor() {
  const { control, watch, register, setValue } = useFormContext<QuizData>()

  // Get current answers and categories
  const answers = watch("answers") || []
  const categories = watch("matchingPairs") || []

  // Use field array for categories
  const categoriesFieldArray = useFieldArray({
    control,
    name: "matchingPairs",
  })

  // Use field array for answers
  const answersFieldArray = useFieldArray({
    control,
    name: "answers",
  })

  const handleAddCategory = () => {
    categoriesFieldArray.append({
      id: Math.random().toString(36).substring(7),
      left: "", // Using 'left' as category name
      right: "", // Using 'right' as category description
    })
  }

  const handleAddAnswer = () => {
    answersFieldArray.append({
      id: Math.random().toString(36).substring(7),
      text: "",
      isCorrect: false,
      categoryIds: [], // Initialize with empty array
    } as any)
  }

  const handleToggleCategory = (answerIndex: number, categoryId: string) => {
    // Get all current answers
    const allAnswers = watch("answers") || []
    const currentAnswer = allAnswers[answerIndex]
    if (!currentAnswer) return

    // Get current categoryIds from the actual answer object
    const currentCategoryIds = ((currentAnswer as any).categoryIds || []) as string[]
    const index = currentCategoryIds.indexOf(categoryId)
    
    let newCategoryIds: string[]
    if (index > -1) {
      // Remove
      newCategoryIds = currentCategoryIds.filter((id) => id !== categoryId)
    } else {
      // Add
      newCategoryIds = [...currentCategoryIds, categoryId]
    }

    // Use setValue directly on the specific path - this is more reliable than update()
    setValue(`answers.${answerIndex}.categoryIds` as any, newCategoryIds, {
      shouldDirty: true,
      shouldTouch: true,
      shouldValidate: false,
    })
  }

  const isCategorySelected = (answerIndex: number, categoryId: string): boolean => {
    // Always get fresh values from watch
    const currentAnswers = watch("answers") || []
    const answer = currentAnswers[answerIndex]
    if (!answer) {
      console.log(`isCategorySelected: answer ${answerIndex} not found`)
      return false
    }
    const categoryIds = ((answer as any).categoryIds || []) as string[]
    const isSelected = categoryIds.includes(categoryId)
    console.log(`isCategorySelected(${answerIndex}, ${categoryId}): ${isSelected}, categoryIds:`, categoryIds)
    return isSelected
  }

  return (
    <div className="space-y-6">
      {/* Debug button - temporary */}
      <button
        type="button"
        onClick={() => {
          console.log("Current form answers:", watch("answers"))
          console.log("Current form categories:", watch("matchingPairs"))
        }}
        className="text-xs bg-yellow-100 px-2 py-1 rounded"
      >
        Debug Form State
      </button>

      {/* Categories Section */}
      <div className="border rounded-lg p-4 bg-gray-50 dark:bg-gray-800/50">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-medium text-sm">Categories</h3>
          <button
            type="button"
            onClick={handleAddCategory}
            className="inline-flex items-center gap-2 text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
          >
            <Plus className="h-3 w-3" />
            Add Category
          </button>
        </div>

        <div className="space-y-3">
          {categoriesFieldArray.fields.map((category, index) => (
            <div key={category.id} className="flex gap-2">
              <div className="flex-1 space-y-2">
                <Input
                  placeholder="Category name"
                  {...register(`matchingPairs.${index}.left`)}
                  className="text-sm"
                />
                <Textarea
                  placeholder="Category description (optional)"
                  {...register(`matchingPairs.${index}.right`)}
                  className="text-sm resize-none h-10"
                />
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => categoriesFieldArray.remove(index)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950 self-start mt-1"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))}
          {categoriesFieldArray.fields.length === 0 && (
            <div className="text-xs text-gray-500 py-2">No categories yet</div>
          )}
        </div>
      </div>

      {/* Answers Section with Category Assignment */}
      <div className="border rounded-lg p-4 bg-gray-50 dark:bg-gray-800/50">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-medium text-sm">Possible Answers</h3>
          <button
            type="button"
            onClick={handleAddAnswer}
            className="inline-flex items-center gap-2 text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
          >
            <Plus className="h-3 w-3" />
            Add Answer
          </button>
        </div>

        <div className="space-y-3">
          {answersFieldArray.fields.map((answer, answerIndex) => (
            <div key={answer.id} className="border rounded p-3 bg-white dark:bg-gray-900">
              <div className="flex gap-2 mb-3">
                <Input
                  placeholder="Answer text"
                  {...register(`answers.${answerIndex}.text`)}
                  className="text-sm flex-1"
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => answersFieldArray.remove(answerIndex)}
                  className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>

              {/* Category assignment */}
              {categoriesFieldArray.fields.length > 0 && (
                <div>
                  <div className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-2">
                    Assign to categories:
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {categoriesFieldArray.fields.map((category, catIndex) => {
                      // Get the actual category ID from watch, not the fieldArray internal ID
                      const categoryData = watch(`matchingPairs.${catIndex}`)
                      const actualCategoryId = categoryData?.id
                      
                      if (!actualCategoryId) return null
                      
                      return (
                        <label
                          key={category.id}
                          className="inline-flex items-center gap-2 px-3 py-1 border rounded bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer text-xs"
                        >
                          <input
                            type="checkbox"
                            checked={isCategorySelected(answerIndex, actualCategoryId)}
                            onChange={() =>
                              handleToggleCategory(answerIndex, actualCategoryId)
                            }
                            className="rounded"
                          />
                          <span>{categoryData.left || "Category"}</span>
                        </label>
                      )
                    })}
                  </div>
                </div>
              )}
              {categoriesFieldArray.fields.length === 0 && (
                <div className="text-xs text-gray-400">Create categories first</div>
              )}
            </div>
          ))}
          {answersFieldArray.fields.length === 0 && (
            <div className="text-xs text-gray-500 py-2">No answers yet</div>
          )}
        </div>
      </div>
    </div>
  )
}
