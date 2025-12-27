/**
 * Fill in the Blank Question Renderer
 * Renders questions with inline input fields for blanks
 */

interface FillBlankRendererProps {
  question: {
    question: string
  }
  answers: string[]
  onAnswerChange: (index: number, value: string) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function FillBlankRenderer({
  question,
  answers,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: FillBlankRendererProps) {
  const questionParts = question.question.split("___")

  return (
    <div className="space-y-4">
      <div className="text-lg leading-relaxed">
        {questionParts.map((part, index) => (
          <span key={index}>
            {part}
            {index < questionParts.length - 1 && (
              <input
                type="text"
                className="inline-block w-40 mx-2 px-3 py-2 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors"
                placeholder="..."
                value={answers[index] || ""}
                onChange={(e) => onAnswerChange(index, e.target.value)}
                disabled={disabled || showFeedback}
              />
            )}
          </span>
        ))}
      </div>
    </div>
  )
}
