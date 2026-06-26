export function EmptyEditorState() {
  return (
    <div className="h-full flex flex-col items-center justify-center bg-gray-50 dark:bg-gray-900 text-gray-500 dark:text-gray-400">
      <img 
        src="/assets/images/icons/icon-128x128.png" 
        alt="GameGuild Icon" 
        className="w-24 h-24 mb-6 opacity-50"
      />
      <h3 className="text-xl font-semibold mb-2">No File Open</h3>
      <p className="text-sm mb-4 flex items-center gap-2">
        Open a file from the File Explorer
      </p>
    </div>
  )
}
