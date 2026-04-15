'use client';

import { MarkdownRenderer } from '@/components/markdown-renderer';
import licenseContent from './license.md';

export default function LicensePage(): React.JSX.Element {
  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-slate-100 px-4 py-12 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto bg-white rounded-lg shadow-lg p-8 sm:p-12">
        <article className="prose prose-slate prose-headings:text-slate-900 prose-p:text-slate-700 prose-a:text-blue-600 hover:prose-a:text-blue-800 prose-strong:text-slate-900 prose-ul:text-slate-700 prose-li:text-slate-700 max-w-none">
          <MarkdownRenderer content={licenseContent} />
        </article>
      </div>
    </div>
  );
}
