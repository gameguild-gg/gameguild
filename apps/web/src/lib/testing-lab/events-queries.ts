import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type ApiError,
  type Result,
  type TestingLabTestingApplicationStatus,
  type TestingLabTestingEventCommitteeMemberProjection,
  type TestingLabTestingEventProjection,
  type TestingLabTestingEventSlotProjection,
  type TestingLabTestingEventStatus,
  type TestingLabTestingProjectApplicationProjection,
  type TestingLabTestingSlotRegistrationProjection,
} from '@game-guild/client';

export interface TestingEventsDirectory {
  events: TestingLabTestingEventProjection[];
  accessIssues: string[];
}

export interface TestingEventManagerData {
  event: TestingLabTestingEventProjection | null;
  slots: TestingLabTestingEventSlotProjection[];
  applications: TestingLabTestingProjectApplicationProjection[];
  committee: TestingLabTestingEventCommitteeMemberProjection[];
  registrationsBySlot: Record<string, TestingLabTestingSlotRegistrationProjection[]>;
  accessIssues: string[];
}

export interface TestingEventsDirectoryOptions {
  status?: TestingLabTestingEventStatus;
  skip?: number;
  take?: number;
}

export interface TestingEventManagerOptions {
  applicationStatus?: TestingLabTestingApplicationStatus;
}

function createModules() {
  const client = createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295',
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => (await auth().catch(() => null))?.tenantId ?? null },
  });

  return {
    events: new GeneratedApi.TestinglabTestingeventsModule(client),
    participation: new GeneratedApi.TestinglabTestingeventparticipationModule(client),
  };
}

async function read<T>(operation: Promise<Result<T, ApiError>>, label: string) {
  try {
    const result = await operation;
    if (result.ok) return { data: result.data, issue: null };
    return {
      data: null,
      issue: `${label} returned ${result.error.status ?? 'an error'}: ${result.error.message}`,
    };
  } catch (error) {
    return {
      data: null,
      issue: `${label} failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
    };
  }
}

export async function getTestingEventsDirectory(
  options: TestingEventsDirectoryOptions = {},
): Promise<TestingEventsDirectory> {
  const api = createModules();
  const result = await read(
    api.events.getTestingEvents({
      status: options.status,
      skip: Math.max(0, options.skip ?? 0),
      take: Math.min(100, Math.max(1, options.take ?? 50)),
    }),
    'Events',
  );

  return {
    events: result.data ?? [],
    accessIssues: result.issue ? [result.issue] : [],
  };
}

export async function getTestingEventManagerData(
  eventId: string,
  options: TestingEventManagerOptions = {},
): Promise<TestingEventManagerData> {
  const api = createModules();
  const [eventResult, slotsResult, applicationsResult, committeeResult] = await Promise.all([
    read(api.events.getTestingEvents1(eventId), 'Event'),
    read(api.events.getTestingEventsSlots(eventId), 'Slots'),
    read(
      api.events.getTestingEventsApplications(eventId, {
        status: options.applicationStatus,
        skip: 0,
        take: 100,
      }),
      'Applications',
    ),
    read(api.events.getTestingEventsCommittee(eventId), 'Committee'),
  ]);

  const slots = slotsResult.data ?? [];
  const registrationEntries = await Promise.all(
    slots
      .filter((slot): slot is TestingLabTestingEventSlotProjection & { id: string } => Boolean(slot.id))
      .map(async (slot) => {
        const result = await read(api.participation.getTestingEventsSlotsRegistrations(slot.id), `Registrations for slot ${slot.id}`);
        return [slot.id, result] as const;
      }),
  );

  const registrationsBySlot: Record<string, TestingLabTestingSlotRegistrationProjection[]> = {};
  const accessIssues = [
    eventResult.issue,
    slotsResult.issue,
    applicationsResult.issue,
    committeeResult.issue,
  ].filter((issue): issue is string => Boolean(issue));

  registrationEntries.forEach(([slotId, result]) => {
    registrationsBySlot[slotId] = result.data ?? [];
    if (result.issue) accessIssues.push(result.issue);
  });

  return {
    event: eventResult.data ?? null,
    slots,
    applications: applicationsResult.data ?? [],
    committee: committeeResult.data ?? [],
    registrationsBySlot,
    accessIssues,
  };
}

export {
  getPublicTestingEventExperience,
  getPublicTestingEventsDirectory,
} from './events-public-queries';
