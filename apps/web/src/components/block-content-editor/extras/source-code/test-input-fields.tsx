"use client"
import { Button } from "@/components/ui/button"
import { PlusIcon, TrashIcon } from "lucide-react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

interface TestInputFieldsProps {
  inputs: string[]
  setInputs: (inputs: string[]) => void
  label?: string
}

export function TestInputFields({ inputs, setInputs, label = "Input" }: TestInputFieldsProps) {
  const addInput = () => {
    setInputs([...inputs, ""])
  }

  const removeInput = (index: number) => {
    const newInputs = [...inputs]
    newInputs.splice(index, 1)
    setInputs(newInputs)
  }

  const updateInput = (index: number, value: string) => {
    const newInputs = [...inputs]
    newInputs[index] = value
    setInputs(newInputs)
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label>{label}</Label>
        <Button type="button" variant="outline" size="sm" onClick={addInput} className="h-7 px-2">
          <PlusIcon className="h-4 w-4 mr-1" />
          Add Parameter
        </Button>
      </div>

      {inputs.length === 0 && (
        <div className="text-sm text-muted-foreground italic">
          No parameters added. Click "Add Parameter" to add input values.
        </div>
      )}

      {inputs.map((input, index) => (
        <div key={index} className="flex items-center gap-2">
          <div className="flex-grow">
            <Input
              value={input}
              onChange={(e) => updateInput(index, e.target.value)}
              placeholder={`Parameter ${index + 1}`}
            />
          </div>
          <Button type="button" variant="ghost" size="sm" onClick={() => removeInput(index)} className="h-9 px-2">
            <TrashIcon className="h-4 w-4" />
          </Button>
        </div>
      ))}
    </div>
  )
}
