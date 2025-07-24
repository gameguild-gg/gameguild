'use client';

import { Button } from '@/components/ui/button';
import type { SerializedButtonNode } from '../../nodes/button-node';
import { cn } from '@/lib/utils';
import { ArrowRight, Copy, Download, ExternalLink, Mail } from 'lucide-react';

export function PreviewButton({ node }: { node: SerializedButtonNode }) {
  if (!node?.data) {
    console.error('Invalid button node structure:', node);
    return null;
  }

  const { text, url, actionType, variant, size, showIcon } = node.data;

  const getActionIcon = () => {
    switch (actionType) {
      case 'url':
        return <ExternalLink className="h-4 w-4" />;
      case 'download':
        return <Download className="h-4 w-4" />;
      case 'copy':
        return <Copy className="h-4 w-4" />;
      case 'email':
        return <Mail className="h-4 w-4" />;
      default:
        return <ArrowRight className="h-4 w-4" />;
    }
  };

  const handleButtonAction = () => {
    switch (actionType) {
      case 'url':
        window.open(url, '_blank');
        break;
      case 'download':
        const link = document.createElement('a');
        link.href = url;
        link.download = '';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        break;
      case 'copy':
        navigator.clipboard.writeText(url);
        break;
      case 'email':
        window.location.href = `mailto:${url}`;
        break;
    }
  };

  return (
    <div className="my-4 flex justify-center">
      <Button variant={variant} size={size} className={cn(size === 'icon' && 'p-0 w-10 h-10 rounded-full')} onClick={handleButtonAction}>
        {size !== 'icon' ? text : ''}
        {showIcon && <span className={size !== 'icon' ? 'ml-2' : ''}>{getActionIcon()}</span>}
      </Button>
    </div>
  );
}
