'use client';

import { AlertCircle, CheckCircle, Clock, XCircle } from 'lucide-react';

interface StatusChipProps {
  status: 'open' | 'full' | 'in-progress' | 'closed';
  variant?: 'default' | 'compact' | 'inline';
  className?: string;
}

export function StatusChip({ status, variant = 'default', className = '' }: StatusChipProps) {
  const baseClasses = 'inline-flex items-center justify-between font-semibold transition-all duration-200';

  // Fixed width for consistent appearance
  const widthClasses = {
    default: 'w-28',
    compact: 'w-24',
    inline: 'w-20',
  };

  // Status styles with enhanced visibility - fully rounded and no shadows, more padding
  const statusStyles = {
    open: {
      default: 'px-4 py-2 rounded-full text-sm bg-gradient-to-br from-green-900/40 to-emerald-900/40 backdrop-blur-md border border-green-600/40 text-green-200',
      compact: 'px-3 py-1.5 rounded-full text-xs bg-gradient-to-br from-green-900/35 to-emerald-900/35 backdrop-blur-md border border-green-600/35 text-green-200',
      inline: 'px-2 py-1 rounded-full text-xs bg-gradient-to-br from-green-900/30 to-emerald-900/30 backdrop-blur-md border border-green-600/30 text-green-200',
    },
    full: {
      default: 'px-4 py-2 rounded-full text-sm bg-gradient-to-br from-red-900/40 to-rose-900/40 backdrop-blur-md border border-red-600/40 text-red-200',
      compact: 'px-3 py-1.5 rounded-full text-xs bg-gradient-to-br from-red-900/35 to-rose-900/35 backdrop-blur-md border border-red-600/35 text-red-200',
      inline: 'px-2 py-1 rounded-full text-xs bg-gradient-to-br from-red-900/30 to-rose-900/30 backdrop-blur-md border border-red-600/30 text-red-200',
    },
    'in-progress': {
      default: 'px-4 py-2 rounded-full text-sm bg-gradient-to-br from-blue-900/40 to-cyan-900/40 backdrop-blur-md border border-blue-600/40 text-blue-200',
      compact: 'px-3 py-1.5 rounded-full text-xs bg-gradient-to-br from-blue-900/35 to-cyan-900/35 backdrop-blur-md border border-blue-600/35 text-blue-200',
      inline: 'px-2 py-1 rounded-full text-xs bg-gradient-to-br from-blue-900/30 to-cyan-900/30 backdrop-blur-md border border-blue-600/30 text-blue-200',
    },
    closed: {
      default: 'px-4 py-2 rounded-full text-sm bg-gradient-to-br from-slate-900/40 to-gray-900/40 backdrop-blur-md border border-slate-600/40 text-slate-200',
      compact: 'px-3 py-1.5 rounded-full text-xs bg-gradient-to-br from-slate-900/35 to-gray-900/35 backdrop-blur-md border border-slate-600/35 text-slate-200',
      inline: 'px-2 py-1 rounded-full text-xs bg-gradient-to-br from-slate-900/30 to-gray-900/30 backdrop-blur-md border border-slate-600/30 text-slate-200',
    },
  };

  const iconSizes = {
    default: 'h-4 w-4',
    compact: 'h-3 w-3',
    inline: 'h-3 w-3',
  };

  const getIcon = () => {
    switch (status) {
      case 'open':
        return <CheckCircle className={`${iconSizes[variant]} text-green-400`} />;
      case 'full':
        return <XCircle className={`${iconSizes[variant]} text-red-400`} />;
      case 'in-progress':
        return <Clock className={`${iconSizes[variant]} text-blue-400`} />;
      default:
        return <AlertCircle className={`${iconSizes[variant]} text-slate-400`} />;
    }
  };

  const getLabel = () => {
    switch (status) {
      case 'open':
        return 'Open';
      case 'full':
        return 'Full';
      case 'in-progress':
        return 'Active';
      case 'closed':
        return 'Closed';
      default:
        return 'Unknown';
    }
  };

  return (
    <div className={`${baseClasses} ${statusStyles[status][variant]} ${widthClasses[variant]} ${className}`}>
      {getIcon()}
      <span>{getLabel()}</span>
    </div>
  );
}
