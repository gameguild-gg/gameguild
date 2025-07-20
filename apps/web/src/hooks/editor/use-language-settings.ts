'use client';

import type React from 'react';

import { useState } from 'react';
import type { LanguageType, ProgrammingLanguage } from '@/components/ui/source-code/types';

interface UseLanguageSettingsProps {
  initialAllowedLanguages?: Record<LanguageType, boolean>;
  initialSelectedLanguage?: ProgrammingLanguage;
}

interface UseLanguageSettingsReturn {
  selectedLanguage: ProgrammingLanguage;
  setSelectedLanguage: (lang: ProgrammingLanguage) => void;
  allowedLanguages: Record<LanguageType, boolean>;
  setAllowedLanguages: React.Dispatch<React.SetStateAction<Record<LanguageType, boolean>>>;
  showLanguagesDialog: boolean;
  setShowLanguagesDialog: (show: boolean) => void;
  getAllowedLanguageTypes: () => LanguageType[];
  getAllowedProgrammingLanguages: () => ProgrammingLanguage[];
}

export function useLanguageSettings({
  initialAllowedLanguages = {
    javascript: true,
    typescript: true,
    python: true,
    lua: true,
    c: true,
    cpp: true,
    cheader: true,
    cppheader: true,
    html: true,
    css: true,
    json: true,
    xml: true,
    yaml: true,
    text: true,
    h: false,
    hpp: false,
    markdown: false,
  },
  initialSelectedLanguage = 'javascript',
}: UseLanguageSettingsProps = {}): UseLanguageSettingsReturn {
  const [selectedLanguage, setSelectedLanguage] = useState<ProgrammingLanguage>(initialSelectedLanguage);
  const [allowedLanguages, setAllowedLanguages] = useState<Record<LanguageType, boolean>>(initialAllowedLanguages);
  const [showLanguagesDialog, setShowLanguagesDialog] = useState(false);

  // Get allowed language types
  const getAllowedLanguageTypes = (): LanguageType[] => {
    return Object.entries(allowedLanguages)
      .filter(([_, isAllowed]) => isAllowed)
      .map(([lang]) => lang as LanguageType);
  };

  // Get allowed programming languages
  const getAllowedProgrammingLanguages = (): ProgrammingLanguage[] => {
    return getAllowedLanguageTypes().filter(
      (lang): lang is ProgrammingLanguage =>
        lang === 'javascript' ||
        lang === 'typescript' ||
        lang === 'python' ||
        lang === 'lua' ||
        lang === 'c' ||
        lang === 'cpp' ||
        lang === 'cheader' ||
        lang === 'cppheader',
    );
  };

  return {
    selectedLanguage,
    setSelectedLanguage,
    allowedLanguages,
    setAllowedLanguages,
    showLanguagesDialog,
    setShowLanguagesDialog,
    getAllowedLanguageTypes,
    getAllowedProgrammingLanguages,
  };
}
