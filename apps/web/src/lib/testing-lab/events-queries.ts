import { getRequestAuthContext } from "@/auth";
import {
  createServerClient,
  GeneratedApi,
  type ApiError,
  type Result,
  type TestingLabTestingApplicationStatus,
  type TestingLabTestingApplicationTesterEligibilityProjection,
  type TestingLabTestingEventCommitteeMemberProjection,
  type TestingLabTestingEventFeedbackReviewProjection,
  type TestingLabTestingEventApplicationAccessProjection,
  type TestingLabTestingEventProjection,
  type TestingLabTestingEventSlotProjection,
  type TestingLabTestingEventStatus,
  type TestingLabTestingParticipantDirectoryProjection,
  type TestingLabTestingProjectApplicationProjection,
  type TestingLabTestingSlotRegistrationProjection,
  type TestingLabTestingSlotRegistrationStatus,
  type TestingLabTestingEventTemplateProjection,
} from "@game-guild/client";
import { cache } from "react";

export interface TestingEventsDirectory {
  events: TestingLabTestingEventProjection[];
  accessIssues: string[];
}

export interface TestingApplicationDirectoryEntry {
  event: TestingLabTestingEventProjection;
  application: TestingLabTestingProjectApplicationProjection;
}

export interface TestingApplicationsDirectory {
  entries: TestingApplicationDirectoryEntry[];
  accessIssues: string[];
}

export interface TestingEventManagerData {
  event: TestingLabTestingEventProjection | null;
  slots: TestingLabTestingEventSlotProjection[];
  applications: TestingLabTestingProjectApplicationProjection[];
  applicationAccess: TestingLabTestingEventApplicationAccessProjection | null;
  committee: TestingLabTestingEventCommitteeMemberProjection[];
  registrationsBySlot: Record<
    string,
    TestingLabTestingSlotRegistrationProjection[]
  >;
  accessIssues: string[];
}

export interface TestingEventFeedbackReviewData {
  feedback: TestingLabTestingEventFeedbackReviewProjection[];
  accessIssues: string[];
}

export interface TestingApplicationTesterEligibilityData {
  eligibility: TestingLabTestingApplicationTesterEligibilityProjection[];
  accessIssues: string[];
}

export interface TestingParticipantDirectoryData {
  directory: TestingLabTestingParticipantDirectoryProjection | null;
  accessIssues: string[];
}

export interface TestingParticipantDirectoryOptions {
  search?: string;
  status?: TestingLabTestingSlotRegistrationStatus;
  skip?: number;
  take?: number;
}

export interface TestingEventsDirectoryOptions {
  status?: TestingLabTestingEventStatus;
  skip?: number;
  take?: number;
}

export interface TestingApplicationsDirectoryOptions {
  status?: TestingLabTestingApplicationStatus;
}

export interface TestingEventManagerOptions {
  applicationStatus?: TestingLabTestingApplicationStatus;
}

function createClient(requestAuth = getRequestAuthContext()) {
  return createServerClient({
    baseUrl:
      process.env.API_URL ||
      process.env.NEXT_PUBLIC_API_URL ||
      "http://localhost:8080",
    auth: { getAccessToken: async () => (await requestAuth).token },
    tenant: { getTenantId: async () => (await requestAuth).tenantId },
  });
}

function createModules(requestAuth = getRequestAuthContext()) {
  const client = createClient(requestAuth);

  return {
    events: new GeneratedApi.TestingLabTestingEventsModule(client),
    participation: new GeneratedApi.TestingLabTestingEventParticipationModule(
      client,
    ),
  };
}

function createTemplateModule(requestAuth = getRequestAuthContext()) {
  const client = createClient(requestAuth);
  return new GeneratedApi.TestingLabTestingEventTemplatesModule(client);
}

export async function getTestingEventTemplates(
  includeArchived = false,
): Promise<{
  templates: TestingLabTestingEventTemplateProjection[];
  accessIssues: string[];
}> {
  const result = await read(
    createTemplateModule().getVTestingTemplates("1", { includeArchived }),
    "Event templates",
  );
  return {
    templates: result.data ?? [],
    accessIssues: result.issue ? [result.issue] : [],
  };
}

function getOperationFailureMessage(error: unknown): string {
  if (error instanceof Error) return error.message;

  if (typeof error === "object" && error !== null && "message" in error) {
    const { message } = error;
    if (typeof message === "string" && message.trim().length > 0)
      return message;
  }

  return "Unknown error";
}

async function read<T>(operation: Promise<Result<T, ApiError>>, label: string) {
  try {
    const result = await operation;
    if (result.ok) return { data: result.data, issue: null };
    return {
      data: null,
      issue: `${label} returned ${result.error.status ?? "an error"}: ${result.error.message}`,
    };
  } catch (error) {
    return {
      data: null,
      issue: `${label} failed: ${getOperationFailureMessage(error)}`,
    };
  }
}

export async function getTestingEventsDirectory(
  options: TestingEventsDirectoryOptions = {},
): Promise<TestingEventsDirectory> {
  const api = createModules();
  const result = await read(
    api.events.getTestingEventsForGetTestingEvents({
      status: options.status,
      skip: Math.max(0, options.skip ?? 0),
      take: Math.min(100, Math.max(1, options.take ?? 50)),
    }),
    "Events",
  );

  return {
    events: result.data ?? [],
    accessIssues: result.issue ? [result.issue] : [],
  };
}

export async function getArchivedTestingEventsDirectory(
  options: Pick<TestingEventsDirectoryOptions, "skip" | "take"> = {},
): Promise<TestingEventsDirectory> {
  const result = await read(
    createModules().events.getTestingEventsArchived({
      skip: Math.max(0, options.skip ?? 0),
      take: Math.min(100, Math.max(1, options.take ?? 50)),
    }),
    "Archived events",
  );
  return {
    events: result.data ?? [],
    accessIssues: result.issue ? [result.issue] : [],
  };
}

export async function getTestingApplicationsDirectory(
  options: TestingApplicationsDirectoryOptions = {},
): Promise<TestingApplicationsDirectory> {
  const api = createModules();
  const eventsResult = await read(
    api.events.getTestingEventsForGetTestingEvents({ skip: 0, take: 100 }),
    "Events",
  );
  const events = eventsResult.data ?? [];
  const applicationResults = await Promise.all(
    events
      .filter(
        (event): event is TestingLabTestingEventProjection & { id: string } =>
          Boolean(event.id) && event.applicationCount !== 0,
      )
      .map(async (event) => ({
        event,
        result: await read(
          api.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications(
            event.id,
            {
              status: options.status,
              skip: 0,
              take: 100,
            },
          ),
          `Applications for ${event.name ?? event.id}`,
        ),
      })),
  );

  return {
    entries: applicationResults.flatMap(({ event, result }) =>
      (result.data ?? []).map((application) => ({ event, application })),
    ),
    accessIssues: [
      eventsResult.issue,
      ...applicationResults.map(({ result }) => result.issue),
    ].filter((issue): issue is string => Boolean(issue)),
  };
}

export async function getTestingParticipantDirectory(
  options: TestingParticipantDirectoryOptions = {},
): Promise<TestingParticipantDirectoryData> {
  const result = await read(
    createModules().participation.getTestingEventsParticipants({
      search: options.search?.trim() || undefined,
      status: options.status,
      skip: Math.max(0, options.skip ?? 0),
      take: Math.min(100, Math.max(1, options.take ?? 25)),
    }),
    "Participants",
  );

  return {
    directory: result.data ?? null,
    accessIssues: result.issue ? [result.issue] : [],
  };
}

export async function getTestingEventManagerData(
  eventId: string,
  options: TestingEventManagerOptions = {},
): Promise<TestingEventManagerData> {
  const api = createModules();
  const [eventResult, applicationsResult, applicationAccessResult] =
    await Promise.all([
      read(
        api.events.getTestingEventsForGetTestingEventsByEventId(eventId),
        "Event",
      ),
      read(
        api.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications(
          eventId,
          {
            status: options.applicationStatus,
            skip: 0,
            take: 100,
          },
        ),
        "Applications",
      ),
      read(
        api.events.getTestingEventsApplicationsAccess(eventId),
        "Application access",
      ),
    ]);

  const canManageApplications =
    applicationAccessResult.data?.canManageApplications === true;
  const [slotsResult, committeeResult] = canManageApplications
    ? await Promise.all([
        read(api.events.getTestingEventsSlots(eventId), "Slots"),
        read(api.events.getTestingEventsCommittee(eventId), "Committee"),
      ])
    : [
        { data: [], issue: undefined },
        { data: [], issue: undefined },
      ];
  const slots = slotsResult.data ?? [];
  const registrationEntries = canManageApplications
    ? await Promise.all(
        slots
          .filter(
            (
              slot,
            ): slot is TestingLabTestingEventSlotProjection & { id: string } =>
              Boolean(slot.id),
          )
          .map(async (slot) => {
            const result = await read(
              api.participation.getTestingEventsSlotsRegistrations(slot.id),
              `Registrations for slot ${slot.id}`,
            );
            return [slot.id, result] as const;
          }),
      )
    : [];

  const registrationsBySlot: Record<
    string,
    TestingLabTestingSlotRegistrationProjection[]
  > = {};
  const accessIssues = [
    eventResult.issue,
    applicationsResult.issue,
    applicationAccessResult.issue,
    ...(canManageApplications
      ? [slotsResult.issue, committeeResult.issue]
      : []),
  ].filter((issue): issue is string => Boolean(issue));

  registrationEntries.forEach(([slotId, result]) => {
    registrationsBySlot[slotId] = result.data ?? [];
    if (result.issue) accessIssues.push(result.issue);
  });

  return {
    event: eventResult.data ?? null,
    slots,
    applications: applicationsResult.data ?? [],
    applicationAccess: applicationAccessResult.data ?? null,
    committee: committeeResult.data ?? [],
    registrationsBySlot,
    accessIssues,
  };
}

export const getTestingEventFeedbackReview = cache(
  async (eventId: string): Promise<TestingEventFeedbackReviewData> => {
    const result = await read(
      createModules().participation.getTestingEventsFeedback(eventId),
      "Event feedback",
    );

    return {
      feedback: result.data ?? [],
      accessIssues: result.issue ? [result.issue] : [],
    };
  },
);

export async function getTestingApplicationTesterEligibility(
  eventId: string,
  testerUserIds: string[],
): Promise<TestingApplicationTesterEligibilityData> {
  const api = createModules();
  const normalizedTesterIds = [...new Set(testerUserIds.filter(Boolean))].slice(
    0,
    100,
  );
  if (normalizedTesterIds.length === 0)
    return { eligibility: [], accessIssues: [] };

  const result = await read(
    api.events.getTestingEventsApplicationsTesterEligibility(eventId, {
      testerUserIds: normalizedTesterIds,
    }),
    "Tester eligibility",
  );

  return {
    eligibility: result.data ?? [],
    accessIssues: result.issue ? [result.issue] : [],
  };
}

export async function getTestingEventWorkspaceData(eventId: string) {
  return getTestingEventManagerData(eventId);
}

export {
  getPublicTestingEventExperience,
  getPublicTestingEventsDirectory,
} from "./events-public-queries";
