import React from 'react';
import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { Header } from '@/components/common/header';
import { Footer } from '@/components/common/footer/default-footer';
import honestyContent from './honesty.md';

export default async function AcademicHonestyPage(): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <Header />
      <main className="flex-1 container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 rounded-lg p-6 shadow-sm">
            <MarkdownRenderer content={honestyContent} />
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}