/**
 * Short Answer Question Renderer
 * Simple single-line text input for short text answers
 */

interface ShortAnswerRendererProps {
  question: {
    question: string
  }
  answer: string
  onAnswerChange: (value: string) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function ShortAnswerRenderer({
  answer,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: ShortAnswerRendererProps) {
  return (
    <div className="space-y-2">
      <input
        type="text"
        className="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors text-base"
        placeholder="Enter your answer..."
        value={answer}
        onChange={(e) => onAnswerChange(e.target.value)}
        disabled={disabled || showFeedback}
      />
    </div>
  )
}
