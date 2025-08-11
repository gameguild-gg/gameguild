'use client';

import { Globe, MapPin } from 'lucide-react';

interface LocationChipProps {
  isOnline: boolean;
  variant?: 'default' | 'compact' | 'inline';
  className?: string;
}

export function LocationChip({ isOnline, variant = 'default', className = '' }: LocationChipProps) {
  const baseClasses = 'inline-flex items-center justify-between font-medium transition-all duration-200';

  // Fixed width for consistent appearance
  const widthClasses = {
    default: 'w-24',
    compact: 'w-20',
    inline: 'w-16',
  };

  // Variant styles - fully rounded and no shadows, more padding
  const variantStyles = {
    default: isOnline
      ? 'px-4 py-2 rounded-full text-sm bg-gradient-to-br from-blue-900/40 to-cyan-900/40 backdrop-blur-md border border-blue-600/40 text-blue-200 hover:from-blue-900/50 hover:to-cyan-900/50 hover:border-blue-600/60'
      : 'px-4 py-2 rounded-full text-sm bg-gradient-to-br from-purple-900/40 to-indigo-900/40 backdrop-blur-md border border-purple-600/40 text-purple-200 hover:from-purple-900/50 hover:to-indigo-900/50 hover:border-purple-600/60',
    compact: isOnline
      ? 'px-3 py-1.5 rounded-full text-xs bg-gradient-to-br from-blue-900/35 to-cyan-900/35 backdrop-blur-md border border-blue-600/35 text-blue-200 hover:from-blue-900/45 hover:to-cyan-900/45'
      : 'px-3 py-1.5 rounded-full text-xs bg-gradient-to-br from-purple-900/35 to-indigo-900/35 backdrop-blur-md border border-purple-600/35 text-purple-200 hover:from-purple-900/45 hover:to-indigo-900/45',
    inline: isOnline
      ? 'px-2 py-1 rounded-full text-xs bg-gradient-to-br from-blue-900/30 to-cyan-900/30 backdrop-blur-md border border-blue-600/30 text-blue-200'
      : 'px-2 py-1 rounded-full text-xs bg-gradient-to-br from-purple-900/30 to-indigo-900/30 backdrop-blur-md border border-purple-600/30 text-purple-200',
  };

  const iconSizes = {
    default: 'h-4 w-4',
    compact: 'h-3 w-3',
    inline: 'h-3 w-3',
  };

  const Icon = isOnline ? Globe : MapPin;
  const iconColor = isOnline ? 'text-blue-400' : 'text-purple-400';

  return (
    <div className={`${baseClasses} ${variantStyles[variant]} ${widthClasses[variant]} ${className}`}>
      <Icon className={`${iconSizes[variant]} ${iconColor}`} />
      <span>{isOnline ? 'Online' : 'On-site'}</span>
    </div>
  );
}
