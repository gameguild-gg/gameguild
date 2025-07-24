'use client';

import { Program } from '@/lib/programs/programs.actions';
import { ProgramGrid } from './program-grid';
import { ProgramRow } from './program-row';
import { ProgramTable } from './program-table';
import { ProgramEmptyState } from './program-empty-state';

interface ProgramContentProps {
  programs: Program[];
  viewMode: 'cards' | 'row' | 'table';
  hasFilters: boolean;
  onCreateProgram?: () => void;
  onClearFilters?: () => void;
}

export function ProgramContent({ programs, viewMode, hasFilters, onCreateProgram, onClearFilters }: ProgramContentProps) {
  if (programs.length === 0) {
    return <ProgramEmptyState hasFilters={hasFilters} hasPrograms={false} onCreateProgram={onCreateProgram} onClearFilters={onClearFilters} />;
  }

  switch (viewMode) {
    case 'cards':
      return <ProgramGrid programs={programs} />;
    case 'row':
      return (
        <div className="space-y-2">
          {programs.map((program) => (
            <ProgramRow key={program.id} program={program} />
          ))}
        </div>
      );
    case 'table':
      return <ProgramTable programs={programs} />;
    default:
      return <ProgramGrid programs={programs} />;
  }
}
