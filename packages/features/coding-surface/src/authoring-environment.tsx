import type { CodingExecutionEnvironment, CodingDraftChangePlan } from '@game-guild/coding';
import { AuthoringField, AuthoringHeading, AuthoringSelect, type AuthoringViewProps } from './authoring-fields';
import { environmentChange } from './authoring-model';

export function CodingAuthoringEnvironment({
  draft,
  issues,
  edit,
  disabled,
  review,
}: AuthoringViewProps & {
  readonly review: (plan: CodingDraftChangePlan, title: string) => void;
}) {
  const options: { value: CodingExecutionEnvironment['runtime']; label: string; disabled?: boolean }[] = [{ value: 'wasi', label: 'WASI terminal' }];
  if (draft.environment.language === 'cpp')
    options.push(
      { value: 'sdl3', label: 'SDL3 canvas - unavailable', disabled: true },
      { value: 'raylib', label: 'raylib canvas - unverified', disabled: true },
      { value: 'allegro5', label: 'Allegro canvas - unverified', disabled: true },
    );
  return (
    <>
      <AuthoringHeading title="Environment" />
      <div className="cs-author-grid">
        <AuthoringSelect
          label="Language"
          value={draft.environment.language}
          disabled={disabled}
          options={[
            { value: 'cpp', label: 'C++' },
            { value: 'c', label: 'C' },
          ]}
          onChange={(language) =>
            review(environmentChange(draft, { language, runtime: language === 'c' ? 'wasi' : draft.environment.runtime }), 'Change language')
          }
        />
        <AuthoringSelect
          label="Execution environment"
          value={draft.environment.runtime}
          disabled={disabled}
          options={options}
          onChange={(runtime) => review(environmentChange(draft, { ...draft.environment, runtime }), 'Change environment')}
        />
      </div>
      <dl className="cs-author-facts">
        <div>
          <dt>Toolchain</dt>
          <dd>
            {draft.toolchain.key} / {draft.toolchain.version}
          </dd>
        </div>
        <div>
          <dt>Output</dt>
          <dd>{draft.environment.runtime === 'wasi' ? 'Terminal' : 'Canvas - execution unavailable'}</dd>
        </div>
      </dl>
      <AuthoringHeading title="Execution limits" />
      <div className="cs-author-grid">
        <AuthoringField
          label="Time limit (ms)"
          value={draft.limits.timeLimitMs}
          numeric
          path="limits.timeLimitMs"
          issues={issues}
          disabled={disabled}
          onChange={(value) =>
            edit((next) => {
              next.limits.timeLimitMs = value;
            })
          }
        />
        <AuthoringField
          label="Output limit (bytes)"
          value={draft.limits.outputLimitBytes}
          numeric
          path="limits.outputLimitBytes"
          issues={issues}
          disabled={disabled}
          onChange={(value) =>
            edit((next) => {
              next.limits.outputLimitBytes = value;
            })
          }
        />
        <AuthoringField
          label="Maximum files"
          value={draft.limits.maxFileCount}
          numeric
          path="limits.maxFileCount"
          issues={issues}
          disabled={disabled}
          onChange={(value) =>
            edit((next) => {
              next.limits.maxFileCount = value;
            })
          }
        />
        <label className="cs-author-field">
          RAM limit - unavailable
          <input type="text" value={draft.limits.memoryLimitBytes} disabled />
        </label>
      </div>
    </>
  );
}
