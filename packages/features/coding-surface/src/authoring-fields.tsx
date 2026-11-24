import { useId, type ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';
import type { CodingAuthoringDraft, CodingParseIssue } from '@game-guild/coding';

export interface AuthoringViewProps {
  readonly draft: CodingAuthoringDraft;
  readonly issues: readonly CodingParseIssue[];
  readonly edit: (operation: (draft: CodingAuthoringDraft) => void) => boolean;
  readonly disabled: boolean;
}

interface TextFieldProps {
  readonly label: string;
  readonly value: string;
  readonly onChange: (value: string) => void;
  readonly path?: string;
  readonly issues?: readonly CodingParseIssue[];
  readonly multiline?: boolean;
  readonly numeric?: boolean;
  readonly disabled?: boolean;
}

export function AuthoringField({ label, value, onChange, path, issues = [], multiline = false, numeric = false, disabled = false }: TextFieldProps) {
  const id = useId();
  const issue = path ? issues.find((entry) => entry.path === path || entry.path.startsWith(`${path}.`)) : undefined;
  const common = {
    id,
    value,
    disabled,
    'data-authoring-field': path,
    'aria-invalid': Boolean(issue),
    'aria-describedby': issue ? `${id}-error` : undefined,
    onChange: (event: { target: { value: string } }) => {
      if (!disabled) onChange(event.target.value);
    },
  };
  return (
    <div className="cs-author-field">
      <label htmlFor={id}>{label}</label>
      {multiline ? <textarea {...common} rows={4} /> : <input {...common} type="text" inputMode={numeric ? 'decimal' : undefined} />}
      {issue ? (
        <span className="cs-author-field-error" id={`${id}-error`}>
          {issue.message}
        </span>
      ) : null}
    </div>
  );
}

export function AuthoringSelect<T extends string>({
  label,
  value,
  options,
  onChange,
  disabled = false,
}: {
  readonly label: string;
  readonly value: T;
  readonly options: readonly { value: T; label: string; disabled?: boolean }[];
  readonly onChange: (value: T) => void;
  readonly disabled?: boolean;
}) {
  const id = useId();
  return (
    <div className="cs-author-field">
      <label htmlFor={id}>{label}</label>
      <select
        id={id}
        value={value}
        disabled={disabled}
        onChange={(event) => {
          const option = options.find((entry) => entry.value === event.target.value);
          if (!disabled && option && !option.disabled) onChange(option.value);
        }}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value} disabled={option.disabled}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}

export function AuthoringCheck({
  label,
  checked,
  onChange,
  disabled = false,
}: {
  readonly label: string;
  readonly checked: boolean;
  readonly onChange: (checked: boolean) => void;
  readonly disabled?: boolean;
}) {
  return (
    <label className="cs-author-check">
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(event) => {
          if (!disabled) onChange(event.target.checked);
        }}
      />
      {label}
    </label>
  );
}

export function AuthoringAction({
  label,
  icon: Icon,
  onClick,
  disabled = false,
  compact = false,
}: {
  readonly label: string;
  readonly icon: LucideIcon;
  readonly onClick: () => void;
  readonly disabled?: boolean;
  readonly compact?: boolean;
}) {
  return (
    <button type="button" className={compact ? 'cs-author-icon' : 'cs-author-action'} aria-label={label} title={label} disabled={disabled} onClick={onClick}>
      <Icon size={16} aria-hidden="true" />
      {compact ? null : <span>{label}</span>}
    </button>
  );
}

export function AuthoringHeading({ title, children }: { readonly title: string; readonly children?: ReactNode }) {
  return (
    <div className="cs-author-task-heading">
      <h2>{title}</h2>
      <div className="cs-author-actions">{children}</div>
    </div>
  );
}
