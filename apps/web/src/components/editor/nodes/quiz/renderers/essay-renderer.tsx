/**
 * Essay Question Renderer
 * Multi-line textarea for longer essay-style answers
 */

interface EssayRendererProps {
  question: {
    question: string
    minWords?: number
    maxWords?: number
  }
  answer: string
  onAnswerChange: (value: string) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function EssayRenderer({
  question,
  answer,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: EssayRendererProps) {
  const wordCount = answer.trim() ? answer.trim().split(/\s+/).length : 0

  return (
    <div className="space-y-2">
      <textarea
        className="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors text-base resize-vertical"
        placeholder="Write your essay here..."
        value={answer}
        onChange={(e) => onAnswerChange(e.target.value)}
        disabled={disabled || showFeedback}
        rows={6}
      />
      {(question.minWords || question.maxWords) && (
        <div className="text-sm text-gray-600 flex justify-between">
          <span>Word count: {wordCount}</span>
          {question.minWords && question.maxWords && (
            <span>
              Required: {question.minWords}-{question.maxWords} words
            </span>
          )}
          {question.minWords && !question.maxWords && <span>Minimum: {question.minWords} words</span>}
          {!question.minWords && question.maxWords && <span>Maximum: {question.maxWords} words</span>}
        </div>
      )}
    </div>
  )
}
