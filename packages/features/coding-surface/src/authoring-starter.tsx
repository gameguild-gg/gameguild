import Editor from '@monaco-editor/react';
import { FilePlus2, Plus, Trash2, WandSparkles } from 'lucide-react';
import type { CodingAuthoringFile, CodingFunctionSignature, CodingScalarType } from '@game-guild/coding';
import { AuthoringAction, AuthoringCheck, AuthoringField, AuthoringHeading, AuthoringSelect, type AuthoringViewProps } from './authoring-fields';

const TYPES: readonly { value: CodingScalarType; label: string }[] = (['integer', 'float', 'boolean', 'string'] as const).map((type) => ({
  value: type,
  label: type,
}));

export function CodingAuthoringStarter({
  draft,
  issues,
  edit,
  disabled,
  contextKey,
  selectedFileId,
  selectFile,
  signature,
  setSignature,
  applySignature,
  addFile,
  generate,
  deleteFile,
}: AuthoringViewProps & {
  readonly contextKey: string;
  readonly selectedFileId: string | null;
  readonly selectFile: (id: string) => void;
  readonly signature: CodingFunctionSignature;
  readonly setSignature: (signature: CodingFunctionSignature) => void;
  readonly applySignature: () => void;
  readonly addFile: () => void;
  readonly generate: () => void;
  readonly deleteFile: (file: CodingAuthoringFile) => void;
}) {
  const fileIndex = draft.files.findIndex((file) => file.id === selectedFileId);
  const file = draft.files[fileIndex];
  const pending = draft.exerciseType === 'function' && JSON.stringify(signature) !== JSON.stringify(draft.functionSignature);
  return (
    <>
      <AuthoringHeading title="Starter code">
        <AuthoringAction label="Add file" icon={FilePlus2} disabled={disabled || draft.files.length >= 100} onClick={addFile} />
        <AuthoringAction
          label="Generate starter"
          icon={WandSparkles}
          disabled={disabled || pending || draft.environment.runtime !== 'wasi'}
          onClick={generate}
        />
      </AuthoringHeading>
      {draft.exerciseType === 'function' ? (
        <details className="cs-author-signature" open={!draft.functionSignature?.name || pending}>
          <summary>
            Function signature{' '}
            <code>
              {draft.functionSignature
                ? `${draft.functionSignature.name}(${draft.functionSignature.parameters.map((parameter) => `${parameter.name}: ${parameter.type}`).join(', ')}): ${draft.functionSignature.returnType}`
                : ''}
            </code>
          </summary>
          <fieldset className="cs-author-row">
            <legend>Signature fields</legend>
            <div className="cs-author-grid">
              <AuthoringField
                label="Function name"
                value={signature.name}
                disabled={disabled}
                path="functionSignature.name"
                issues={issues}
                onChange={(name) => setSignature({ ...signature, name })}
              />
              <AuthoringSelect
                label="Return type"
                value={signature.returnType}
                disabled={disabled}
                options={TYPES}
                onChange={(returnType) => setSignature({ ...signature, returnType })}
              />
            </div>
            {signature.parameters.map((parameter, index) => (
              <div className="cs-author-inline" key={index}>
                <AuthoringField
                  label={`Parameter ${index + 1} name`}
                  value={parameter.name}
                  disabled={disabled}
                  onChange={(name) =>
                    setSignature({ ...signature, parameters: signature.parameters.map((item, position) => (position === index ? { ...item, name } : item)) })
                  }
                />
                <AuthoringSelect
                  label={`Parameter ${index + 1} type`}
                  value={parameter.type}
                  disabled={disabled}
                  options={TYPES}
                  onChange={(type) =>
                    setSignature({ ...signature, parameters: signature.parameters.map((item, position) => (position === index ? { ...item, type } : item)) })
                  }
                />
                <AuthoringAction
                  compact
                  label={`Remove parameter ${index + 1}`}
                  icon={Trash2}
                  disabled={disabled}
                  onClick={() => setSignature({ ...signature, parameters: signature.parameters.filter((_, position) => position !== index) })}
                />
              </div>
            ))}
            <div className="cs-author-actions">
              <AuthoringAction
                label="Add parameter"
                icon={Plus}
                disabled={disabled || signature.parameters.length >= 100}
                onClick={() => setSignature({ ...signature, parameters: [...signature.parameters, { name: '', type: 'integer' }] })}
              />
              <button type="button" className="cs-author-action" disabled={disabled || !pending} onClick={applySignature}>
                Apply signature
              </button>
              {pending ? (
                <span role="status" className="cs-author-pending">
                  Unapplied signature changes
                </span>
              ) : null}
            </div>
          </fieldset>
        </details>
      ) : null}
      <div className="cs-author-grid">
        <AuthoringSelect
          label="Initial file"
          value={draft.entryPoint.path}
          disabled={disabled}
          options={[
            { value: '', label: 'Select initial source' },
            ...draft.files
              .filter((item) => item.visibility === 'public' && item.encoding === 'text')
              .map((item) => ({ value: item.path, label: item.path || '(unnamed file)' })),
          ]}
          onChange={(path) =>
            edit((next) => {
              next.entryPoint.path = path;
            })
          }
        />
        <AuthoringCheck
          label="Allow learner-created files"
          checked={draft.allowLearnerCreatedFiles}
          disabled={disabled}
          onChange={(checked) =>
            edit((next) => {
              next.allowLearnerCreatedFiles = checked;
            })
          }
        />
      </div>
      <div className="cs-author-file-tabs" role="tablist" aria-label="Starter files">
        {draft.files.map((item, index) => (
          <button type="button" key={item.id} role="tab" aria-selected={item.id === file?.id} title={item.path} onClick={() => selectFile(item.id)}>
            {item.path.split('/').at(-1) || `File ${index + 1}`}
          </button>
        ))}
      </div>
      {file ? (
        <>
          <div className="cs-author-file-policies">
            <AuthoringField
              label="File path"
              value={file.path}
              disabled={disabled}
              path={`files.${fileIndex}.path`}
              issues={issues}
              onChange={(path) =>
                edit((next) => {
                  if (next.entryPoint.path === file.path) next.entryPoint.path = path;
                  next.files[fileIndex]!.path = path;
                  for (const solution of next.validationSolutions) {
                    for (const replacement of solution.files) {
                      if (replacement.fileId === file.id) replacement.path = path;
                    }
                  }
                })
              }
            />
            <AuthoringSelect
              label="File visibility"
              value={file.visibility}
              disabled={disabled}
              options={[
                { value: 'public', label: 'Public' },
                { value: 'private', label: 'Private' },
              ]}
              onChange={(visibility) =>
                edit((next) => {
                  next.files[fileIndex]!.visibility = visibility;
                  if (visibility === 'private') next.files[fileIndex]!.mutable = false;
                })
              }
            />
            <AuthoringCheck
              label="Learner can edit this file"
              checked={file.mutable}
              disabled={disabled || file.visibility !== 'public' || file.encoding !== 'text'}
              onChange={(checked) =>
                edit((next) => {
                  next.files[fileIndex]!.mutable = checked;
                })
              }
            />
            <AuthoringAction compact label="Delete file" icon={Trash2} disabled={disabled} onClick={() => deleteFile(file)} />
          </div>
          <div className="cs-author-code" aria-label="Starter source">
            {file.encoding === 'text' ? (
              <Editor
                path={`coding-author://${encodeURIComponent(contextKey)}/${encodeURIComponent(file.id)}`}
                value={file.content}
                language={draft.environment.language === 'c' ? 'c' : 'cpp'}
                theme="vs-dark"
                loading={<p role="status">Loading editor...</p>}
                options={{ readOnly: disabled, automaticLayout: true, minimap: { enabled: false }, fontSize: 14, scrollBeyondLastLine: false, tabSize: 2 }}
                onChange={(value) => {
                  if (value !== undefined)
                    edit((next) => {
                      next.files[fileIndex]!.content = value;
                    });
                }}
              />
            ) : (
              <p role="status">Binary file retained. Source editing unavailable.</p>
            )}
          </div>
        </>
      ) : (
        <p role="status">No starter files.</p>
      )}
    </>
  );
}
