import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import React from 'react';
import honestyContent from './honesty.md';

export default async function AcademicHonestyPage(): Promise<React.JSX.Element> {
  return (
    <main className="flex-1 container mx-auto px-4 py-8">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 rounded-lg p-6 shadow-sm">
          <MarkdownRenderer content={honestyContent} />
        </div>
      </div>
    </main>
  );
}