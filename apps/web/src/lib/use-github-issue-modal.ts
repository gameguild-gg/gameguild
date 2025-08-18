'use client';

import { useState } from 'react';

export interface GitHubIssueModalState {
  isOpen: boolean;
  openModal: () => void;
  closeModal: () => void;
}

export function useGitHubIssueModal(): GitHubIssueModalState {
  const [isOpen, setIsOpen] = useState(false);

  const openModal = () => setIsOpen(true);
  const closeModal = () => setIsOpen(false);

  return {
    isOpen,
    openModal,
    closeModal,
  };
}