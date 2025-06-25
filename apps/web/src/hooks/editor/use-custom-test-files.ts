import type { CodeFile, ProgrammingLanguage } from "../components/ui/source-code/types"
import {
  DEFAULT_FIRST_CODE_TEMPLATES,
  FUNCTION_FIRST_CODE_TEMPLATES,
  getExtensionForSelectedLanguage,
} from "../components/ui/source-code/templates/code-templates"

interface UseCustomTestFilesProps {
  files: CodeFile[]
  setFiles: (files: CodeFile[] | ((prev: CodeFile[]) => CodeFile[])) => void
  activeFileId: string
  testCases: Record<
    string,
    {
      type: "simple" | "inout" | "custom" | "function" | "console"
      customCodeFirst?: string | Record<ProgrammingLanguage, string>
      customCodeSecond?: string | Record<ProgrammingLanguage, string>
    }[]
  >
}

export function useCustomTestFiles({ files, setFiles, activeFileId, testCases }: UseCustomTestFilesProps) {
  const createCustomTestFiles = (testIndex: number, fileLanguage: ProgrammingLanguage) => {
    const extension = getExtensionForSelectedLanguage(fileLanguage)
    const testCase = testCases[activeFileId]?.[testIndex]
    const testType = testCase?.type || "custom"

    // Create single file name based on test type
    const fileName = `test-${testType}.${extension}`

    // Check if file already exists
    const fileExists = files.some((file) => file.name === fileName)

    // Get appropriate template based on test type
    const getTemplate = () => {
      if (testType === "function") {
        return FUNCTION_FIRST_CODE_TEMPLATES[fileLanguage] || ""
      } else if (testType === "console") {
        return DEFAULT_FIRST_CODE_TEMPLATES[fileLanguage] || ""
      }
      return DEFAULT_FIRST_CODE_TEMPLATES[fileLanguage] || ""
    }

    // Create or update the file
    if (!fileExists) {
      const content =
        testCase && testCase.customCodeFirst
          ? typeof testCase.customCodeFirst === "string"
            ? testCase.customCodeFirst
            : testCase.customCodeFirst[fileLanguage] || ""
          : getTemplate()

      const newFile: CodeFile = {
        id: crypto.randomUUID(),
        name: fileName,
        content: content,
        language: fileLanguage,
        isMain: true,
        isVisible: true,
        readOnlyState: "hidden",
      }

      setFiles((prev) => [...prev, newFile])
    }
  }

  return {
    createCustomTestFiles,
  }
}
