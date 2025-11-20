import type { EditorInstance } from "./types"

interface EditorInstanceSwitchProps {
  editorInstance: EditorInstance
  onToggle: () => void
}

export function EditorInstanceSwitch({ editorInstance, onToggle }: EditorInstanceSwitchProps) {
  return (
    <div
      data-no-drag="true"
      className="absolute top-2 right-2 z-50 flex items-center gap-2 bg-white dark:bg-gray-800 px-3 py-1.5 rounded-lg shadow-md border border-gray-200 dark:border-gray-700"
      onMouseDown={(e) => {
        e.stopPropagation()
      }}
      onClick={(e) => {
        e.stopPropagation()
      }}
    >
      <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
        Instance:
      </span>
      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation()
          onToggle()
        }}
        className="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        style={{
          backgroundColor: editorInstance === "multiple" ? "#3b82f6" : "#6b7280"
        }}
        title={editorInstance === "multiple" ? "Multiple: Opens in all displays" : "Unique: Opens only in this display"}
      >
        <span
          className="inline-block h-4 w-4 transform rounded-full bg-white transition-transform pointer-events-none"
          style={{
            transform: editorInstance === "multiple" ? "translateX(1.5rem)" : "translateX(0.25rem)"
          }}
        />
      </button>
      <span className="text-xs font-bold text-gray-700 dark:text-gray-300 min-w-[1ch]">
        {editorInstance === "multiple" ? "M" : "U"}
      </span>
    </div>
  )
}
