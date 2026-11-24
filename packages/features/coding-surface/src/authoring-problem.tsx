import { Plus, Trash2 } from 'lucide-react';
import { AuthoringAction, AuthoringField, AuthoringHeading, type AuthoringViewProps } from './authoring-fields';
import type { CodingAuthoringSurfaceProps } from './authoring-types';

export function CodingAuthoringProblem({ draft, issues, edit, disabled, createId }: AuthoringViewProps & Pick<CodingAuthoringSurfaceProps, 'createId'>) {
  return (
    <>
      <AuthoringHeading title="Problem" />
      <AuthoringField
        label="Statement"
        value={draft.problem.statement}
        multiline
        path="problem.statement"
        issues={issues}
        disabled={disabled}
        onChange={(value) =>
          edit((next) => {
            next.problem.statement = value;
          })
        }
      />
      <AuthoringHeading title="Examples">
        <AuthoringAction
          label="Add example"
          icon={Plus}
          disabled={disabled || draft.problem.examples.length >= 100}
          onClick={() =>
            edit((next) => {
              next.problem.examples.push({ id: createId('example'), input: '', output: '', explanation: '' });
            })
          }
        />
      </AuthoringHeading>
      {draft.problem.examples.map((example, index) => (
        <fieldset key={example.id} className="cs-author-row">
          <legend>Example {index + 1}</legend>
          <div className="cs-author-grid">
            <AuthoringField
              label="Example input"
              value={example.input}
              multiline
              disabled={disabled}
              onChange={(value) =>
                edit((next) => {
                  next.problem.examples[index]!.input = value;
                })
              }
            />
            <AuthoringField
              label="Example output"
              value={example.output}
              multiline
              disabled={disabled}
              onChange={(value) =>
                edit((next) => {
                  next.problem.examples[index]!.output = value;
                })
              }
            />
          </div>
          <AuthoringField
            label="Explanation"
            value={example.explanation}
            multiline
            disabled={disabled}
            onChange={(value) =>
              edit((next) => {
                next.problem.examples[index]!.explanation = value;
              })
            }
          />
          <AuthoringAction
            compact
            label={`Delete example ${index + 1}`}
            icon={Trash2}
            disabled={disabled}
            onClick={() =>
              edit((next) => {
                next.problem.examples.splice(index, 1);
              })
            }
          />
        </fieldset>
      ))}
      <AuthoringHeading title="Constraints">
        <AuthoringAction
          label="Add constraint"
          icon={Plus}
          disabled={disabled || draft.problem.constraints.length >= 100}
          onClick={() =>
            edit((next) => {
              next.problem.constraints.push('');
            })
          }
        />
      </AuthoringHeading>
      {draft.problem.constraints.map((constraint, index) => (
        <div className="cs-author-inline" key={index}>
          <AuthoringField
            label={`Constraint ${index + 1}`}
            value={constraint}
            path={`problem.constraints.${index}`}
            issues={issues}
            disabled={disabled}
            onChange={(value) =>
              edit((next) => {
                next.problem.constraints[index] = value;
              })
            }
          />
          <AuthoringAction
            compact
            label={`Delete constraint ${index + 1}`}
            icon={Trash2}
            disabled={disabled}
            onClick={() =>
              edit((next) => {
                next.problem.constraints.splice(index, 1);
              })
            }
          />
        </div>
      ))}
    </>
  );
}
