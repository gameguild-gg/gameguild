import type { CodeFile, LanguageType, ProgrammingLanguage } from "./types"

// Get the base name of a file (without extension)
export const getBaseName = (fileName: string): string => {
  const lastDotIndex = fileName.lastIndexOf(".")
  return lastDotIndex !== -1 ? fileName.substring(0, lastDotIndex) : fileName
}

// Get the extension of a file
export const getExtension = (fileName: string): string => {
  const lastDotIndex = fileName.lastIndexOf(".")
  return lastDotIndex !== -1 ? fileName.substring(lastDotIndex + 1) : ""
}

// Update the getExtensionForSelectedLanguage function to handle C and C++
export const getExtensionForSelectedLanguage = (selectedLanguage: ProgrammingLanguage): string => {
  switch (selectedLanguage) {
    case "javascript":
      return "js"
    case "typescript":
      return "ts"
    case "python":
      return "py"
    case "lua":
      return "lua"
    case "c":
      return "c"
    case "h":
      return "h"
    case "cpp":
      return "cpp"
    case "hpp":
      return "hpp"
    default:
      return "js"
  }
}

// Get the content for a file
export const getFileContent = (file: CodeFile): string => {
  return file?.content || ""
}

// Update the getExtensionForLanguage function to handle C and C++
export const getExtensionForLanguage = (lang: LanguageType): string => {
  switch (lang) {
    case "javascript":
      return "js"
    case "typescript":
      return "ts"
    case "python":
      return "py"
    case "lua":
      return "lua"
    case "c":
      return "c"
    case "cpp":
      return "cpp"
    case "cheader":
      return "h"
    case "cppheader":
      return "hpp"
    case "h":
      return "h"
    case "hpp":
      return "hpp"
    case "html":
      return "html"
    case "css":
      return "css"
    case "json":
      return "json"
    case "bash":
      return "sh"
    case "xml":
      return "xml"
    case "yaml":
      return "yaml"
    default:
      return "txt"
  }
}

// Update the getLanguageFromExtension function to handle C and C++
export const getLanguageFromExtension = (ext: string): LanguageType => {
  switch (ext) {
    case "js":
      return "javascript"
    case "ts":
      return "typescript"
    case "py":
      return "python"
    case "lua":
      return "lua"
    case "c":
      return "c"
    case "cpp":
      return "cpp"
    case "h":
      return "h"
    case "hpp":
      return "hpp"
    case "html":
      return "html"
    case "css":
      return "css"
    case "json":
      return "json"
    case "sh":
      return "bash"
    case "xml":
      return "xml"
    case "yaml":
      return "yaml"
    default:
      return "text"
  }
}

// Update the getLanguageLabel function to handle C and C++
export const getLanguageLabel = (lang: LanguageType): string => {
  switch (lang) {
    case "javascript":
      return ".js (JavaScript)"
    case "typescript":
      return ".ts (TypeScript)"
    case "python":
      return ".py (Python)"
    case "lua":
      return ".lua (Lua)"
    case "c":
      return ".c (C)"
    case "cpp":
      return ".cpp (C++)"
    case "h":
      return ".h (C Header)"
    case "hpp":
      return ".hpp (C++ Header)"
    case "cheader":
      return ".h (C Header)"
    case "cppheader":
      return ".hpp (C++ Header)"
    case "html":
      return ".html (HTML)"
    case "css":
      return ".css (CSS)"
    case "json":
      return ".json (JSON)"
    case "bash":
      return ".sh (Bash)"
    case "text":
      return ".txt (Plain Text)"
    case "xml":
      return ".xml (XML)"
    case "yaml":
      return ".yaml (YAML)"
    default:
      return ".txt (TXT)"
  }
}

// Update the getFileIcon function to handle C and C++
export const getFileIcon = (file: CodeFile, selectedLanguage?: ProgrammingLanguage) => {
  // Use the file's own language for the icon
  const extension = getExtension(file.name)

  switch (extension) {
    case "js":
      return <span className="text-yellow-500">JS</span>
    case "ts":
      return <span className="text-blue-500">TS</span>
    case "py":
      return <span className="text-green-500">PY</span>
    case "lua":
      return <span className="text-indigo-500">LUA</span>
    case "c":
      return <span className="text-blue-700">C</span>
    case "h":
      return <span className="text-blue-700">H</span>
    case "cpp":
    case "cc":
    case "cxx":
      return <span className="text-blue-600">C++</span>
    case "hpp":
      return <span className="text-blue-600">HPP</span>
    case "html":
      return <span className="text-orange-500">HTML</span>
    case "css":
      return <span className="text-purple-500">CSS</span>
    case "json":
      return <span className="text-gray-500">JSON</span>
    case "sh":
      return <span className="text-gray-500">SH</span>
    case "xml":
      return <span className="text-red-500">XML</span>
    case "yaml":
      return <span className="text-amber-500">YAML</span>
    default:
      return <span className="text-gray-500">TXT</span>
  }
}

export const getStateIcon = (file: CodeFile) => {
  // Since we removed hasStates, we can return a simple icon or nothing
  return null
}
