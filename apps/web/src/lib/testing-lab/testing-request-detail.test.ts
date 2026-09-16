import { describe, expect, it } from "vitest";
import { mapTestingRequestDetail } from "./testing-request-detail";

describe("mapTestingRequestDetail", () => {
  it("maps the complete stable request projection", () => {
    expect(
      mapTestingRequestDetail({
        id: "request-1",
        title: "Campus build",
        description: "Test the build",
        downloadUrl: null,
        instructionsContent: "Install it",
        feedbackFormContent: "Rate it",
        maxTesters: 10,
        currentTesterCount: 3,
        startDate: "2026-09-20",
        endDate: null,
        status: "InProgress",
        projectVersionId: "version-1",
        projectVersion: {
          id: "version-1",
          projectId: "project-1",
          versionNumber: "1.0.0",
          status: "ReadyForTesting",
          project: {
            id: "project-1",
            title: "Project",
            name: null,
            slug: "project",
            status: 1,
          },
        },
        isDeleted: false,
      }),
    ).toEqual({
      id: "request-1",
      title: "Campus build",
      description: "Test the build",
      downloadUrl: null,
      instructionsContent: "Install it",
      feedbackFormContent: "Rate it",
      maxTesters: 10,
      currentTesterCount: 3,
      startDate: "2026-09-20",
      endDate: null,
      status: "InProgress",
      projectVersionId: "version-1",
      projectVersion: {
        id: "version-1",
        projectId: "project-1",
        versionNumber: "1.0.0",
        status: "ReadyForTesting",
        project: {
          id: "project-1",
          title: "Project",
          name: null,
          slug: "project",
          status: 1,
        },
      },
      isDeleted: false,
    });
  });

  it.each([null, undefined, "request", 42, []])(
    "rejects a non-record request: %s",
    (value) => expect(mapTestingRequestDetail(value)).toBeNull(),
  );

  it.each([
    {},
    { id: "request-1", title: "Title", status: "Unknown" },
    { id: 1, title: "Title", status: "Open" },
    { id: "request-1", title: 1, status: "Open" },
    { id: "request-1", title: "Title", status: null },
  ])("rejects missing or invalid request identity %#", (value) => {
    expect(mapTestingRequestDetail(value)).toBeNull();
  });

  it("accepts numeric status while dropping invalid optional fields", () => {
    expect(
      mapTestingRequestDetail({
        id: "request-1",
        title: "Title",
        status: 3,
        description: 42,
        maxTesters: "ten",
        isDeleted: "false",
        projectVersion: "invalid",
      }),
    ).toMatchObject({
      id: "request-1",
      status: 3,
      description: undefined,
      maxTesters: undefined,
      isDeleted: undefined,
      projectVersion: null,
    });
  });

  it.each([
    { id: "version-1" },
    { projectId: "project-1" },
    { id: 1, projectId: "project-1" },
    { id: "version-1", projectId: 1 },
  ])("drops an invalid project version %#", (projectVersion) => {
    expect(
      mapTestingRequestDetail({
        id: "request-1",
        title: "Title",
        status: "Draft",
        projectVersion,
      })?.projectVersion,
    ).toBeNull();
  });

  it.each([
    null,
    {},
    { id: 1 },
  ])("drops an invalid nested project %#", (project) => {
    expect(
      mapTestingRequestDetail({
        id: "request-1",
        title: "Title",
        status: "Open",
        projectVersion: {
          id: "version-1",
          projectId: "project-1",
          project,
        },
      })?.projectVersion?.project,
    ).toBeNull();
  });

  it("preserves null project status and rejects unsupported status values", () => {
    const base = {
      id: "request-1",
      title: "Title",
      status: "Open",
      projectVersion: {
        id: "version-1",
        projectId: "project-1",
        project: { id: "project-1", status: null },
      },
    };
    expect(mapTestingRequestDetail(base)?.projectVersion?.project?.status).toBeNull();
    expect(
      mapTestingRequestDetail({
        ...base,
        projectVersion: {
          ...base.projectVersion,
          project: { id: "project-1", status: true },
        },
      })?.projectVersion?.project?.status,
    ).toBeUndefined();
  });
});
