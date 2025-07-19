'use client';

import { Trophy } from 'lucide-react';

interface RewardChipProps {
  value: string;
  variant?: 'default' | 'compact' | 'inline';
  className?: string;
}

export function RewardChip({ value, variant = 'default', className = '' }: RewardChipProps) {
  const baseClasses = 'inline-flex items-center gap-1.5 font-medium transition-all duration-200';

  // Variant styles
  const variantStyles = {
    default:
      'px-3 py-1.5 rounded-lg text-sm bg-gradient-to-br from-yellow-900/40 to-amber-900/40 backdrop-blur-md border border-yellow-600/40 text-yellow-200 hover:from-yellow-900/50 hover:to-amber-900/50 hover:border-yellow-600/60 shadow-lg shadow-yellow-500/20',
    compact:
      'px-2 py-1 rounded-md text-xs bg-gradient-to-br from-yellow-900/35 to-amber-900/35 backdrop-blur-md border border-yellow-600/35 text-yellow-200 hover:from-yellow-900/45 hover:to-amber-900/45 shadow-md shadow-yellow-500/15',
    inline: 'px-1.5 py-0.5 rounded text-xs bg-gradient-to-br from-yellow-900/30 to-amber-900/30 backdrop-blur-md border border-yellow-600/30 text-yellow-200',
  };

  const iconSizes = {
    default: 'h-4 w-4',
    compact: 'h-3 w-3',
    inline: 'h-3 w-3',
  };

  return (
    <div className={`${baseClasses} ${variantStyles[variant]} ${className}`}>
      <Trophy className={`${iconSizes[variant]} text-yellow-400`} />
      <span>{value}</span>
    </div>
  );
}
