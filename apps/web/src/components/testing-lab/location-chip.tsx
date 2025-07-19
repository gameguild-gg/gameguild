'use client';

import { Globe, MapPin } from 'lucide-react';

interface LocationChipProps {
  isOnline: boolean;
  variant?: 'default' | 'compact' | 'inline';
  className?: string;
}

export function LocationChip({ isOnline, variant = 'default', className = '' }: LocationChipProps) {
  const baseClasses = 'inline-flex items-center gap-1.5 font-medium transition-all duration-200';

  // Variant styles
  const variantStyles = {
    default: isOnline
      ? 'px-3 py-1.5 rounded-lg text-sm bg-gradient-to-br from-blue-900/40 to-cyan-900/40 backdrop-blur-md border border-blue-600/40 text-blue-200 hover:from-blue-900/50 hover:to-cyan-900/50 hover:border-blue-600/60 shadow-lg shadow-blue-500/20'
      : 'px-3 py-1.5 rounded-lg text-sm bg-gradient-to-br from-purple-900/40 to-indigo-900/40 backdrop-blur-md border border-purple-600/40 text-purple-200 hover:from-purple-900/50 hover:to-indigo-900/50 hover:border-purple-600/60 shadow-lg shadow-purple-500/20',
    compact: isOnline
      ? 'px-2 py-1 rounded-md text-xs bg-gradient-to-br from-blue-900/35 to-cyan-900/35 backdrop-blur-md border border-blue-600/35 text-blue-200 hover:from-blue-900/45 hover:to-cyan-900/45 shadow-md shadow-blue-500/15'
      : 'px-2 py-1 rounded-md text-xs bg-gradient-to-br from-purple-900/35 to-indigo-900/35 backdrop-blur-md border border-purple-600/35 text-purple-200 hover:from-purple-900/45 hover:to-indigo-900/45 shadow-md shadow-purple-500/15',
    inline: isOnline
      ? 'px-1.5 py-0.5 rounded text-xs bg-gradient-to-br from-blue-900/30 to-cyan-900/30 backdrop-blur-md border border-blue-600/30 text-blue-200'
      : 'px-1.5 py-0.5 rounded text-xs bg-gradient-to-br from-purple-900/30 to-indigo-900/30 backdrop-blur-md border border-purple-600/30 text-purple-200',
  };

  const iconSizes = {
    default: 'h-4 w-4',
    compact: 'h-3 w-3',
    inline: 'h-3 w-3',
  };

  const Icon = isOnline ? Globe : MapPin;
  const iconColor = isOnline ? 'text-blue-400' : 'text-purple-400';

  return (
    <div className={`${baseClasses} ${variantStyles[variant]} ${className}`}>
      <Icon className={`${iconSizes[variant]} ${iconColor}`} />
      <span>{isOnline ? 'Online' : 'On-site'}</span>
    </div>
  );
}
