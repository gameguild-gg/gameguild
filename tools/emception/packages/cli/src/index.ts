// @emception/cli — programmatic CLI surface. Phase 9.
export const COMMANDS = ['doctor', 'cdn-export', 'run', 'test'] as const;
export type CliCommand = (typeof COMMANDS)[number];

export {
    formatExportResult, runCdnExport, type CdnExportOptions,
    type CdnExportResult
} from './cdn-export.js';
export { formatReport, runDoctor, type DoctorCheck, type DoctorReport } from './doctor.js';

