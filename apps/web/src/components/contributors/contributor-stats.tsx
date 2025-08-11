import React from 'react';
import { numberToAbbreviation } from '@/lib/utils';

interface ContributorStatsProps {
  label: string;
  value: number;
  icon?: React.ReactNode;
  variant?: 'primary' | 'secondary';
}

export const ContributorStats: React.FC<ContributorStatsProps> = ({ label, value, icon, variant = 'secondary' }) => {
  return (
    <div className="flex items-center gap-2">
      {icon && <div className={`flex items-center justify-center w-5 h-5 ${variant === 'primary' ? 'text-primary' : 'text-accent'}`}>{icon}</div>}
      <div className="flex flex-col">
        <span className="text-sm text-muted-foreground uppercase tracking-wide">{label}</span>
        <span className="text-lg font-bold text-foreground">{numberToAbbreviation(value)}</span>
      </div>
    </div>
  );
};
