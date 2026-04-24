// @emception/cli — programmatic CLI surface. Phase 9.
export const COMMANDS = ['doctor', 'cdn-export', 'run', 'test'] as const;
export type CliCommand = (typeof COMMANDS)[number];

export { runDoctor, formatReport, type DoctorCheck, type DoctorReport } from './doctor.js';
