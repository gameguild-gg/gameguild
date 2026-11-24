import type { CodingLearnerDelivery } from '@game-guild/coding';
import type { CodingWorkbenchSession } from './workbench-session-types';
import { CodingSurface } from './coding-surface';

export interface CodingLearnerPreviewProps {
  readonly contextKey: string;
  readonly delivery: CodingLearnerDelivery;
  readonly manifestUrl: string;
  readonly beforeExecution: Promise<void>;
  readonly onReady: (session: CodingWorkbenchSession) => void;
}

export function CodingAuthoringLearnerPreview(props: CodingLearnerPreviewProps) {
  return (
    <section className="cs-author-learner-preview" aria-label="Learner preview workspace">
      <CodingSurface
        mode="learner"
        contextKey={props.contextKey}
        definition={props.delivery}
        manifestUrl={props.manifestUrl}
        beforeExecution={props.beforeExecution}
        onWorkbenchReady={props.onReady}
        enableWorkspace={false}
      />
    </section>
  );
}
