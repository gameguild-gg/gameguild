import { useState, type CSSProperties, type KeyboardEvent, type ReactElement, type ReactNode } from 'react';
import { BookOpen, Code2, FlaskConical, ListChecks, Maximize2 } from 'lucide-react';
import './learner-solving.css';

export type CodingSolvingView = 'problem' | 'code' | 'cases' | 'results';

export interface CodingSolvingLayoutProps {
  readonly problem: ReactNode;
  readonly editor: ReactNode;
  readonly cases: ReactNode;
  readonly results: ReactNode;
  readonly view: CodingSolvingView;
  readonly onViewChange: (view: CodingSolvingView) => void;
}

/** Shared learner/teacher-preview layout, with no execution or academic effects. */
export function CodingSolvingLayout(props: CodingSolvingLayoutProps): ReactElement {
  const [focused, setFocused] = useState(false);
  const [share, setShare] = useState(36);
  const views = [
    { id: 'problem', name: 'Problem', icon: BookOpen },
    { id: 'code', name: 'Code', icon: Code2 },
    { id: 'cases', name: 'Cases', icon: FlaskConical },
    { id: 'results', name: 'Results', icon: ListChecks },
  ] as const;
  const style: CSSProperties & { '--coding-problem-share': string } = { '--coding-problem-share': `${share}%` };
  function navigate(event: KeyboardEvent<HTMLElement>, index: number) {
    const target =
      event.key === 'Home'
        ? 0
        : event.key === 'End'
          ? views.length - 1
          : event.key === 'ArrowRight'
            ? (index + 1) % views.length
            : event.key === 'ArrowLeft'
              ? (index + views.length - 1) % views.length
              : null;
    if (target === null) return;
    event.preventDefault();
    props.onViewChange(views[target].id);
    event.currentTarget.parentElement?.querySelectorAll('button')[target]?.focus();
  }
  return (
    <section className="cs-solver" aria-label="Coding solving workspace" data-coding-view={props.view} data-focused={focused} style={style}>
      <div className="cs-solver-navigation">
        <nav aria-label="Workspace views">
          {views.map((view, index) => (
            <button
              key={view.id}
              type="button"
              aria-pressed={props.view === view.id}
              onClick={() => props.onViewChange(view.id)}
              onKeyDown={(event) => navigate(event, index)}
            >
              <view.icon size={15} aria-hidden="true" />
              <span>{view.name}</span>
            </button>
          ))}
        </nav>
        <button
          className="cs-solver-focus"
          type="button"
          title="Focus selected view"
          aria-label="Focus selected view"
          aria-pressed={focused}
          onClick={() => setFocused((value) => !value)}
        >
          <Maximize2 size={16} aria-hidden="true" />
        </button>
        <label className="cs-solver-width">
          Problem width
          <input type="range" aria-label="Problem width" min="25" max="60" value={share} onChange={(event) => setShare(Number(event.target.value))} />
        </label>
      </div>
      <div className="cs-solver-panels">
        <article className="cs-solver-region cs-solver-problem" aria-label="Problem view" data-region="problem" data-active={props.view === 'problem'}>
          {props.problem}
        </article>
        <section className="cs-solver-region cs-solver-code" aria-label="Code view" data-region="code" data-active={props.view === 'code'}>
          {props.editor}
        </section>
        <section
          className="cs-solver-region cs-solver-cases"
          aria-label="Cases view"
          data-region="cases"
          data-active={props.view === 'cases'}
          hidden={props.view === 'results'}
        >
          {props.cases}
        </section>
        <section
          className="cs-solver-region cs-solver-results"
          aria-label="Results view"
          data-region="results"
          data-active={props.view === 'results'}
          hidden={props.view !== 'results'}
        >
          {props.results}
        </section>
      </div>
    </section>
  );
}
