'use client';

import { useRouter } from 'next/navigation';
import { EnhancedProgramManagementContent } from './enhanced-program-management-content';
import { Program } from '@/lib/programs/programs.actions';

interface ClientProgramManagementWrapperProps {
  programs: Program[];
}

export function ClientProgramManagementWrapper({ programs }: ClientProgramManagementWrapperProps) {
  const router = useRouter();

  const handleCreateProgram = () => {
    router.push('/dashboard/courses/create');
  };

  return <EnhancedProgramManagementContent programs={programs} onCreateProgram={handleCreateProgram} />;
}
