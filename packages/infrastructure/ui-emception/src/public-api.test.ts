import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

describe('assessment editor public API', () => {
  it('does not expose the retired GameGuild IDE wrapper', () => {
    const source = readFileSync(resolve(process.cwd(), 'src/index.ts'), 'utf8');

    expect(source).toContain("export { CodingAssessmentEditor } from './components/CodingAssessmentEditor';");
    expect(source).toContain("export { createAssessmentSession } from './assessment/session';");
    expect(source).not.toMatch(/components\/Ide|\bas Ide\b|\bIdeHandle\b/);
    expect(source).not.toMatch(/components\/TestResultsPanel|\bTestResultsPanel\b/);
  });
});
