/**
 * Sorting utilities for manager page
 * Handles multi-criteria sorting with normalized scoring
 */

type SortOrder = 'newest' | 'oldest' | 'name' | 'name-desc' | 'size-largest' | 'size-smallest'

interface SortableItem {
  name: string
  size?: number
  totalSize?: number  // For collections
  createdAt?: string
  updatedAt?: string
  created?: string    // For collections
  updated?: string    // For collections
}

/**
 * Apply multi-criteria sorting with averaged scoring
 * Each criterion contributes equally to the final sort order
 */
export function applySorting<T extends SortableItem>(
  items: T[],
  sortOrder: SortOrder[],
  dateField: 'createdAt' | 'updatedAt' | 'created' | 'updated' = 'updatedAt'
): T[] {
  if (!sortOrder || sortOrder.length === 0) {
    // Default: newest first
    const field = dateField
    return [...items].sort((a, b) => {
      const dateA = a[field] ? new Date(a[field]!).getTime() : 0
      const dateB = b[field] ? new Date(b[field]!).getTime() : 0
      return dateB - dateA
    })
  }

  const sorted = [...items]

  // Calculate min/max values for normalization
  const dates = sorted.map(item => {
    const dateStr = item[dateField]
    return dateStr ? new Date(dateStr).getTime() : 0
  })
  const sizes = sorted.map(item => item.size ?? item.totalSize ?? 0)
  const minDate = Math.min(...dates)
  const maxDate = Math.max(...dates)
  const minSize = Math.min(...sizes)
  const maxSize = Math.max(...sizes)

  // Calculate composite score for each item
  sorted.sort((a, b) => {
    let scoreA = 0
    let scoreB = 0

    for (const sortType of sortOrder) {
      let partialScoreA = 0
      let partialScoreB = 0

      switch (sortType) {
        case "newest": {
          // Normalize dates to 0-1 range (newer = higher score)
          const dateA = a[dateField] ? new Date(a[dateField]!).getTime() : 0
          const dateB = b[dateField] ? new Date(b[dateField]!).getTime() : 0
          partialScoreA = maxDate !== minDate ? (dateA - minDate) / (maxDate - minDate) : 0
          partialScoreB = maxDate !== minDate ? (dateB - minDate) / (maxDate - minDate) : 0
          break
        }
        case "oldest": {
          // Normalize dates to 0-1 range (older = higher score)
          const dateA = a[dateField] ? new Date(a[dateField]!).getTime() : 0
          const dateB = b[dateField] ? new Date(b[dateField]!).getTime() : 0
          partialScoreA = maxDate !== minDate ? (maxDate - dateA) / (maxDate - minDate) : 0
          partialScoreB = maxDate !== minDate ? (maxDate - dateB) / (maxDate - minDate) : 0
          break
        }
        case "name": {
          // Alphabetical: earlier in alphabet = higher score
          const comparison = a.name.toLowerCase().localeCompare(b.name.toLowerCase())
          partialScoreA = comparison <= 0 ? 1 : 0
          partialScoreB = comparison >= 0 ? 1 : 0
          break
        }
        case "name-desc": {
          // Reverse alphabetical: later in alphabet = higher score
          const comparisonDesc = b.name.toLowerCase().localeCompare(a.name.toLowerCase())
          partialScoreA = comparisonDesc <= 0 ? 1 : 0
          partialScoreB = comparisonDesc >= 0 ? 1 : 0
          break
        }
        case "size-largest": {
          // Normalize sizes to 0-1 range (larger = higher score)
          const sizeA = a.size ?? a.totalSize ?? 0
          const sizeB = b.size ?? b.totalSize ?? 0
          partialScoreA = maxSize !== minSize ? (sizeA - minSize) / (maxSize - minSize) : 0
          partialScoreB = maxSize !== minSize ? (sizeB - minSize) / (maxSize - minSize) : 0
          break
        }
        case "size-smallest": {
          // Normalize sizes to 0-1 range (smaller = higher score)
          const sizeA = a.size ?? a.totalSize ?? 0
          const sizeB = b.size ?? b.totalSize ?? 0
          partialScoreA = maxSize !== minSize ? (maxSize - sizeA) / (maxSize - minSize) : 0
          partialScoreB = maxSize !== minSize ? (maxSize - sizeB) / (maxSize - minSize) : 0
          break
        }
      }

      scoreA += partialScoreA
      scoreB += partialScoreB
    }

    // Average the scores
    scoreA /= sortOrder.length
    scoreB /= sortOrder.length

    return scoreB - scoreA // Higher score comes first
  })

  return sorted
}
