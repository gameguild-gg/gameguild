'use client';

import type React from 'react';

import { useMemo, useCallback } from 'react';
import type { CodeFile } from '@/components/ui/source-code/types';

interface UseFileContentProps {
  files: CodeFile[];
  setFiles: React.Dispatch<React.SetStateAction<CodeFile[]>>;
  activeFileId: string;
}

interface UseFileContentReturn {
  activeFile: CodeFile | undefined;
  activeFileContent: string;
  updateActiveFileContent: (content: string) => void;
  getVisibleFiles: (isEditing: boolean) => CodeFile[];
  addSolutionTemplate: () => void;
}

export function insertSolutionTemplate(content: string, language: string): string {
  // Check if a solution function already exists
  if (
    content.includes('function solution') ||
    content.includes('def solution') ||
    content.includes('int solution') ||
    content.includes('auto solution') ||
    content.includes('class Solution')
  ) {
    return content;
  }

  let template = '';

  switch (language) {
    case 'javascript':
      template = `
// Write your solution function here

function solution(a) {
  // Your solution here
  // The 'a' parameter contains the input from "Initial stdin"
  // console.log(a);
  return a;
}
`;
      break;
    case 'typescript':
      template = `
// Write your solution function here
function solution(value: any): any[] {
  // Your solution here
  return [];
}
`;
      break;
    case 'python':
      template = `
# Write your solution function here
def solution(value):
    # Your solution here
    return []
`;
      break;
    case 'lua':
      template = `
-- Write your solution function here
function solution(value)
    -- Your solution here
    return {}
end
`;
      break;
    case 'c':
      template = `
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/**
 * Write your solution function here
 * @param value The input value
 * @return The result
 */
int solution(int value) {
    // Your solution here
    return 0;
}
`;
      break;
    case 'cpp':
      template = `
#include <iostream>
#include <vector>
#include <string>
#include <algorithm>

/**
 * Write your solution function here
 * @param value The input value
 * @return The result
 */
int solution(int value) {
    // Your solution here
    return 0;
}

// For vector return type, use this template instead:
/*
std::vector<int> solution(int value) {
    // Your solution here
    return {};
}
*/
`;
      break;
    case 'h':
      template = `
#ifndef SOLUTION_H
#define SOLUTION_H

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/**
 * Write your solution function declaration here
 * @param value The input value
 * @return The result
 */
int solution(int value);

// Implementation can be provided in a separate .c file
// or inline here if this is a header-only implementation
#ifdef HEADER_IMPLEMENTATION
int solution(int value) {
    // Your solution here
    return 0;
}
#endif

#endif // SOLUTION_H
`;
      break;
    case 'hpp':
      template = `
#ifndef SOLUTION_HPP
#define SOLUTION_HPP

#include <iostream>
#include <vector>
#include <string>
#include <algorithm>

/**
 * Function-based solution
 * @param value The input value
 * @return The result
 */
int solution(int value);

// Class-based solution (alternative)
/*
class Solution {
public:
    // Constructor
    Solution() {}
    
    // Main solution method
    int solve(int value) {
        // Your solution here
        return 0;
    }
    
    // For vector return type
    std::vector<int> solveVector(int value) {
        // Your solution here
        return {};
    }
};
*/

// Implementation can be provided in a separate .cpp file
// or inline here if this is a header-only implementation
#ifdef HEADER_IMPLEMENTATION
int solution(int value) {
    // Your solution here
    return 0;
}
#endif

#endif // SOLUTION_HPP
`;
      break;
    default:
      return content;
  }

  return content + template;
}

export function useFileContent({ files, setFiles, activeFileId }: UseFileContentProps): UseFileContentReturn {
  // Get visible files based on editing mode
  const getVisibleFiles = (isEditing: boolean) => {
    return isEditing ? files : files.filter((file) => file.isVisible);
  };

  // Get active file
  const activeFile = useMemo(() => {
    const visibleFiles = getVisibleFiles(true); // Always check all files first
    // First try to find the active file in all files
    const file = visibleFiles.find((file) => file.id === activeFileId);
    if (file) return file;

    // If active file is not found, select the first file
    return visibleFiles[0] || files[0];
  }, [files, activeFileId]);

  // Get active file content based on selected language
  const activeFileContent = useMemo(() => {
    return activeFile?.content || '';
  }, [activeFile]);

  // Update active file content
  const updateActiveFileContent = useCallback(
    (content: string) => {
      // Check if file is in read-only mode
      const currentFile = files.find((file) => file.id === activeFileId);
      if (currentFile?.readOnlyState === 'always') {
        console.log('File is read-only, cannot update content');
        return;
      }

      console.log('Updating file content:', activeFileId, content.substring(0, 20) + '...');
      setFiles((prevFiles) =>
        prevFiles.map((file) => {
          if (file.id !== activeFileId) return file;
          return { ...file, content };
        }),
      );
    },
    [activeFileId, files, setFiles],
  );

  // Add a function to add the solution template to the active file
  const addSolutionTemplate = useCallback(() => {
    if (!activeFile) return;

    const newContent = insertSolutionTemplate(activeFileContent, activeFile.language || 'javascript');
    updateActiveFileContent(newContent);
  }, [activeFile, activeFileContent, updateActiveFileContent]);

  return {
    activeFile,
    activeFileContent,
    updateActiveFileContent,
    getVisibleFiles,
    addSolutionTemplate,
  };
}
