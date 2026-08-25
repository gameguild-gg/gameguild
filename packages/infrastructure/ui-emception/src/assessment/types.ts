/**
 * GameGuild's coding-assessment wire contract.
 *
 * This contract is intentionally owned by the GameGuild package rather than
 * `emception`: the vanilla runtime only understands generic `TestPlan`s and
 * workspace files. Keep the PascalCase fields aligned with the backend DTO.
 */

export type CodingAssessmentParameterType =
  | 'string'
  | 'boolean'
  | 'integer'
  | 'float';

export type CodingAssessmentFileEncoding = 'text' | 'base64';
export type CodingAssessmentFileVisibility = 'Public' | 'Private';

export interface CodingAssessmentParameter {
  readonly Type: CodingAssessmentParameterType;
  readonly Content: unknown;
}

export interface CodingAssessmentNamedParameter {
  readonly Name: string;
  readonly Type: CodingAssessmentParameterType;
}

export interface CodingAssessmentFunction {
  readonly FunctionName: string;
  readonly Parameters?: readonly CodingAssessmentNamedParameter[];
  readonly ReturnType: { readonly Type: CodingAssessmentParameterType };
}

export interface CodingAssessmentFunctionalCase {
  readonly Inputs: readonly CodingAssessmentParameter[];
  readonly Expected: CodingAssessmentParameter;
}

interface CodingAssessmentTestBase {
  readonly Name?: string | null;
  readonly Weight?: number;
}

export interface CodingAssessmentStandardTest extends CodingAssessmentTestBase {
  readonly kind: 'standard';
  readonly Stdin?: string | null;
  readonly Stdout: string;
  readonly Stderr?: string | null;
  readonly ExitCode?: number | null;
}

export interface CodingAssessmentFunctionalTest extends CodingAssessmentTestBase {
  readonly kind: 'functional';
  readonly Function: CodingAssessmentFunction;
  readonly Cases: readonly CodingAssessmentFunctionalCase[];
}

export type CodingAssessmentTest =
  | CodingAssessmentStandardTest
  | CodingAssessmentFunctionalTest;

export interface CodingAssessmentFile {
  readonly Content: string;
  readonly Encoding?: CodingAssessmentFileEncoding;
  readonly Visibility?: CodingAssessmentFileVisibility;
  readonly Modifiable?: boolean;
}

export interface CodingAssessmentDefinition {
  readonly Type: 'coding-assignment';
  readonly Version: 1;
  readonly Environment: {
    readonly Language?: string;
    readonly Tools?: string;
    readonly LibBundle?: string | null;
    readonly AllowStudentCreateFiles?: boolean;
  };
  readonly Data: {
    readonly Files: Readonly<Record<string, CodingAssessmentFile>>;
  };
  readonly Tests: {
    readonly Public?: readonly CodingAssessmentTest[];
    readonly Private?: readonly CodingAssessmentTest[];
  };
  readonly Grading: unknown;
}
