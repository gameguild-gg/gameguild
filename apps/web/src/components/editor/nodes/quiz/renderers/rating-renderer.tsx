/**
 * Rating Question Renderer
 * Displays a scale of rating buttons (e.g., 1-5)
 */

interface RatingRendererProps {
  question: {
    question: string
    scale: {
      min: number
      max: number
      step: number
    }
  }
  selectedRating?: number | null
  onRatingSelect: (rating: number) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function RatingRenderer({
  question,
  selectedRating,
  onRatingSelect,
  disabled = false,
  showFeedback = false,
}: RatingRendererProps) {
  const { scale } = question
  const ratingOptions = Array.from({ length: scale.max - scale.min + 1 }, (_, i) => scale.min + i)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm text-gray-600 font-medium">{scale.min} (Lowest)</span>
        <span className="text-sm text-gray-600 font-medium">{scale.max} (Highest)</span>
      </div>

      <div className="flex items-center justify-center space-x-3">
        {ratingOptions.map((value) => {
          const isSelected = selectedRating === value

          return (
            <button
              key={value}
              type="button"
              className={`
                w-12 h-12 rounded-lg border-2 font-bold text-lg transition-all duration-200
                ${
                  isSelected
                    ? "border-blue-500 bg-blue-500 text-white shadow-lg scale-110"
                    : "border-gray-300 text-gray-700 hover:border-blue-300 hover:bg-blue-50"
                }
                ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-md cursor-pointer"}
              `}
              onClick={() => !disabled && !showFeedback && onRatingSelect(value)}
              disabled={disabled || showFeedback}
            >
              {value}
            </button>
          )
        })}
      </div>
    </div>
  )
}
