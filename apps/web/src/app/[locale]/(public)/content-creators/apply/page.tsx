import { Link } from '@/i18n/navigation';
import { ArrowLeft } from 'lucide-react';
import React from 'react';
import { ContentCreatorApplicationWizard } from '@/components/content-creators/content-creator-application-wizard';

/**
 * Application wizard for the content-creator program (issue #105): three steps —
 * application, creator profile, and first course setup — submitted to the public
 * course-proposal pipeline.
 */
export default function ContentCreatorApplyPage(): React.JSX.Element {
  return (
    <main className="mx-auto w-full max-w-5xl px-4 py-16 sm:px-6 lg:px-8">
      <div className="mb-8 space-y-4 text-center">
        <Link
          href="/content-creators"
          className="inline-flex items-center text-sm font-medium text-muted-foreground transition hover:text-foreground"
        >
          <ArrowLeft className="mr-2 size-4" aria-hidden="true" />
          Back to the creator process
        </Link>
        <h1 className="text-4xl font-semibold tracking-tight text-foreground">Apply to be a content creator</h1>
        <p className="mx-auto max-w-2xl text-base leading-7 text-muted-foreground">
          Complete the three steps below. Everything is submitted together, so you can prepare your creator profile
          and first course sketch before sending the application for review.
        </p>
      </div>

      <ContentCreatorApplicationWizard />
    </main>
  );
}
