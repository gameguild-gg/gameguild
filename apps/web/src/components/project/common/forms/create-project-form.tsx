'use client';

import { Project } from '@/components/legacy/projects/actions';

type onProjectCreatedCallback = (project: Project) => void;

interface CreateProjectFormProps {
  onProjectCreated: onProjectCreatedCallback;
}

export const CreateProjectForm = ({ onProjectCreated }: CreateProjectFormProps) => {
  return <>{/* TODO: Create a form to join a session as a Developer or as a Tester. */}</>;
};
