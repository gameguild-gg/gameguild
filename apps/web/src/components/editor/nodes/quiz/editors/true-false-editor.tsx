/**
 * True/False Question Editor
 * Simple selector for correct answer
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

export function TrueFalseEditor() {
  const { watch, setValue } = useFormContext()
  const answers = watch("answers")

  const correctAnswer = answers?.find((a: any) => a.isCorrect)?.id || ""

  const handleCorrectAnswerChange = (value: string) => {
    const updatedAnswers = answers.map((a: any) => ({
      ...a,
      isCorrect: a.id === value,
    }))
    setValue("answers", updatedAnswers)
  }

  return (
    <div className="space-y-3">
      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Correct Answer</Label>
      <Select value={correctAnswer} onValueChange={handleCorrectAnswerChange}>
        <SelectTrigger className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600">
          <SelectValue placeholder="Select correct answer" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="true">True</SelectItem>
          <SelectItem value="false">False</SelectItem>
        </SelectContent>
      </Select>
    </div>
  )
}
