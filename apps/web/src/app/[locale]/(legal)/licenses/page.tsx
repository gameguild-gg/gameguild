import { MarkdownContent } from '@/components/content/markdown';
import { getLicenseContent } from '@/lib/integrations/github/github.actions';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const licenseData = await getLicenseContent();

  if (!licenseData) {
    return (
      <div className="container mx-auto px-4 py-8">
        <h1 className="text-4xl font-bold mb-6">License</h1>
        <p className="text-muted-foreground">Unable to load license information.</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-4xl font-bold mb-4">License</h1>
        <div className="flex flex-col gap-2 text-sm text-muted-foreground">
          <p>
            <strong>License:</strong> {licenseData.name}
          </p>
          <p>
            <strong>SPDX ID:</strong> {licenseData.spdx_id}
          </p>
        </div>
      </div>

      <div className="border-t pt-8">
        <MarkdownContent content={licenseData.content} />
      </div>
    </div>
  );
}
