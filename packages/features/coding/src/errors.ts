export interface CodingParseIssue {
  path: string;
  message: string;
}

export class CodingValidationError extends TypeError {
  readonly issues: readonly CodingParseIssue[];

  constructor(label: string, issues: readonly CodingParseIssue[]) {
    super(`${label}: ${issues.map((issue) => `${issue.path}: ${issue.message}`).join('; ')}`);
    this.name = 'CodingValidationError';
    this.issues = issues;
  }
}
