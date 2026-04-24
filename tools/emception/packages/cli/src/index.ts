// @emception/cli — programmatic CLI surface. Phase 9.
export const COMMANDS = ['doctor', 'cdn-export', 'run', 'test'] as const;
export type CliCommand = (typeof COMMANDS)[number];

export { formatReport, runDoctor, type DoctorCheck, type DoctorReport } from './doctor.js';
export {
    runCdnExport,
    formatExportResult,
    type CdnExportOptions,
    type CdnExportResult,
} from './cdn-export.js';

