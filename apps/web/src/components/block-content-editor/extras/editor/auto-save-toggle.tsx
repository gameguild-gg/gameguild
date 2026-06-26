"use client"

interface AutoSaveToggleProps {
  enabled: boolean
  onToggle: () => void
  disabled?: boolean
}

export function AutoSaveToggle({ enabled, onToggle, disabled = false }: AutoSaveToggleProps) {
  return (
    <button
      onClick={onToggle}
      className="flex items-center gap-2 px-3 py-1.5 text-sm transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
      disabled={disabled}
    >
      <div
        className={`h-2 w-2 rounded-full ${
          enabled && !disabled ? "bg-green-500 animate-pulse" : "bg-gray-400 dark:bg-gray-600"
        }`}
      />
      <span
        className={`font-medium ${
          enabled && !disabled
            ? "text-green-600 dark:text-green-400"
            : "text-gray-500 dark:text-gray-400"
        }`}
      >
        {enabled && !disabled ? "Auto-save" : "Manual"}
      </span>
    </button>
  )
}
