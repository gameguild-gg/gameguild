import type {
  TestingProjectSummary,
  TestingProjectVersionSummary,
  TestingRequestStatus,
  TestingRequestSummary,
} from './queries';

type UnknownRecord = Record<string, unknown>;

const testingRequestStatuses = [
  'Draft', 'Open', 'Active', 'InProgress', 'Paused', 'Completed', 'Cancelled',
] as const;

function asRecord(value: unknown): UnknownRecord | null {
  if (typeof value !== 'object' || value === null) return null;

  return Object.fromEntries(Object.entries(value));
}

function asString(value: unknown): string | undefined {
  return typeof value === 'string' ? value : undefined;
}

function asNullableString(value: unknown): string | null | undefined {
  return typeof value === 'string' || value === null ? value : undefined;
}

function asNullableNumber(value: unknown): number | null | undefined {
  return typeof value === 'number' || value === null ? value : undefined;
}

function asOptionalBoolean(value: unknown): boolean | undefined {
  return typeof value === 'boolean' ? value : undefined;
}

function asRequestStatus(value: unknown): TestingRequestStatus | null {
  if (typeof value === 'number') return value;
  if (typeof value !== 'string') return null;

  return testingRequestStatuses.find((status) => status === value) ?? null;
}

function asProjectStatus(value: unknown): TestingProjectSummary['status'] {
  return typeof value === 'string' || typeof value === 'number' || value === null ? value : undefined;
}

function mapProject(value: unknown): TestingProjectSummary | null {
  const project = asRecord(value);
  const id = project ? asString(project.id) : undefined;

  if (!project || !id) return null;

  return {
    id,
    title: asNullableString(project.title),
    name: asNullableString(project.name),
    slug: asNullableString(project.slug),
    status: asProjectStatus(project.status),
  };
}

function mapProjectVersion(value: unknown): TestingProjectVersionSummary | null {
  const version = asRecord(value);
  const id = version ? asString(version.id) : undefined;
  const projectId = version ? asString(version.projectId) : undefined;

  if (!version || !id || !projectId) return null;

  return {
    id,
    projectId,
    versionNumber: asNullableString(version.versionNumber),
    status: asNullableString(version.status),
    project: mapProject(version.project),
  };
}

/**
 * Maps the stable Testing Lab request-detail projection into the dashboard read model.
 * The mapper remains narrow so optional project data stays safe at the UI boundary.
 */
export function mapTestingRequestDetail(value: unknown): TestingRequestSummary | null {
  const request = asRecord(value);
  const id = request ? asString(request.id) : undefined;
  const title = request ? asString(request.title) : undefined;
  const status = request ? asRequestStatus(request.status) : null;

  if (!request || !id || !title || status === null) return null;

  return {
    id,
    title,
    description: asNullableString(request.description),
    downloadUrl: asNullableString(request.downloadUrl),
    instructionsContent: asNullableString(request.instructionsContent),
    feedbackFormContent: asNullableString(request.feedbackFormContent),
    maxTesters: asNullableNumber(request.maxTesters),
    currentTesterCount: asNullableNumber(request.currentTesterCount),
    startDate: asNullableString(request.startDate),
    endDate: asNullableString(request.endDate),
    status,
    projectVersionId: asNullableString(request.projectVersionId),
    projectVersion: mapProjectVersion(request.projectVersion),
    isDeleted: asOptionalBoolean(request.isDeleted),
  };
}
