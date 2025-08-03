'use client';

import { Trophy } from 'lucide-react';

interface RewardChipProps {
  value: string;
  variant?: 'default' | 'compact' | 'inline';
  className?: string;
}

export function RewardChip({ value, variant = 'default', className = '' }: RewardChipProps) {
  const baseClasses = 'inline-flex items-center gap-1.5 font-medium transition-all duration-200';

  // Variant styles - more subtle styling
  const variantStyles = {
    default: 'px-3 py-1.5 rounded-lg text-sm bg-gradient-to-br from-yellow-900/20 to-amber-900/20 backdrop-blur-md border border-yellow-600/25 text-yellow-300 hover:from-yellow-900/30 hover:to-amber-900/30 hover:border-yellow-600/40',
    compact: 'px-2 py-1 rounded-md text-xs bg-gradient-to-br from-yellow-900/18 to-amber-900/18 backdrop-blur-md border border-yellow-600/20 text-yellow-300 hover:from-yellow-900/25 hover:to-amber-900/25',
    inline: 'px-1.5 py-0.5 rounded text-xs bg-gradient-to-br from-yellow-900/15 to-amber-900/15 backdrop-blur-md border border-yellow-600/15 text-yellow-300',
  };

  const iconSizes = {
    default: 'h-4 w-4',
    compact: 'h-3 w-3',
    inline: 'h-3 w-3',
  };

  return (
    <div className={`${baseClasses} ${variantStyles[variant]} ${className}`}>
      <Trophy className={`${iconSizes[variant]} text-yellow-500/70`} />
      <span>{value}</span>
    </div>
  );
}
