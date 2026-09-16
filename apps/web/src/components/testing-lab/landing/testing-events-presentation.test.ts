import { describe, expect, it } from "vitest";
import { presentTestingEvents } from "./testing-events-presentation";

describe("presentTestingEvents", () => {
  it("combines real slot capacity, schedule, and location data on the server", () => {
    const [event] = presentTestingEvents([
      {
        id: "event-1",
        name: "Campus night",
        description: "Playtest night.",
        mode: "InPerson",
        status: "ApplicationsOpen",
        startsAt: "2026-08-12T21:00:00.000Z",
        endsAt: "2026-08-12T23:00:00.000Z",
        slots: [
          {
            id: "slot-1",
            campusName: "Main campus",
            roomName: "Lab 2",
            startsAt: "2026-08-12T18:00:00.000Z",
            endsAt: "2026-08-12T20:00:00.000Z",
            maxTesters: 10,
            maxProjects: 3,
            registeredTesterCount: 4,
            approvedProjectCount: 2,
            availableTesterCount: 6,
          },
        ],
      },
    ]);

    expect(event).toMatchObject({
      id: "event-1",
      title: "Campus night",
      mode: "In person",
      status: "open",
      location: "Main campus - Lab 2",
      startsAt: "2026-08-12T18:00:00.000Z",
      endsAt: "2026-08-12T20:00:00.000Z",
      testerCount: 4,
      testerLimit: 10,
      projectCount: 2,
      projectLimit: 3,
      availableTesterCount: 6,
      scheduleCount: 1,
    });
  });

  it("keeps unlimited capacity explicit instead of inventing a limit", () => {
    const [event] = presentTestingEvents([
      {
        id: "event-2",
        mode: "Online",
        status: "Completed",
      },
    ]);

    expect(event).toMatchObject({
      title: "Untitled testing event",
      location: "Online",
      status: "completed",
      testerLimit: null,
      projectLimit: null,
      availableTesterCount: 0,
    });
  });

  it.each([
    ["Scheduled", "open", "Open"],
    ["Active", "in-progress", "In Progress"],
    ["Completed", "completed", "Completed"],
    ["Cancelled", "closed", "Closed"],
    [undefined, "closed", "Closed"],
  ] as const)(
    "maps the %s API status to the public %s state",
    (apiStatus, status, statusLabel) => {
      const [event] = presentTestingEvents([
        { id: "status-event", status: apiStatus, slots: [] },
      ]);

      expect(event).toMatchObject({ status, statusLabel });
    },
  );

  it("uses event dates and a pending location when schedules are incomplete", () => {
    const [event] = presentTestingEvents([
      {
        id: "event-3",
        name: "  ",
        description: "  ",
        mode: "Hybrid",
        status: "ApplicationsClosed",
        startsAt: "2026-09-10T18:00:00.000Z",
        endsAt: "2026-09-10T20:00:00.000Z",
        slots: [
          {
            id: "slot-empty",
            registeredTesterCount: undefined,
            approvedProjectCount: undefined,
            availableTesterCount: undefined,
          },
        ],
      },
    ]);

    expect(event).toMatchObject({
      title: "Untitled testing event",
      description: "A managed GameGuild project testing event.",
      mode: "Hybrid",
      status: "closed",
      startsAt: "2026-09-10T18:00:00.000Z",
      endsAt: "2026-09-10T20:00:00.000Z",
      location: "Location pending",
      testerCount: 0,
      projectCount: 0,
      testerLimit: null,
      projectLimit: null,
      availableTesterCount: null,
      scheduleCount: 1,
    });
  });

  it("sorts slot dates, deduplicates locations, and totals zero capacities", () => {
    const [event] = presentTestingEvents([
      {
        id: undefined,
        name: "  Multi-slot lab  ",
        description: "  Two rooms and two times.  ",
        mode: undefined,
        status: "Scheduled",
        slots: [
          {
            id: "slot-late",
            campusName: "North",
            roomName: "Room 2",
            startsAt: "2026-09-11T18:00:00.000Z",
            endsAt: "2026-09-11T20:00:00.000Z",
            maxTesters: 0,
            maxProjects: 0,
            availableTesterCount: 0,
          },
          {
            id: "slot-early",
            campusName: "North",
            roomName: "Room 2",
            startsAt: "2026-09-10T18:00:00.000Z",
            endsAt: "2026-09-10T20:00:00.000Z",
            maxTesters: 0,
            maxProjects: 0,
            availableTesterCount: 0,
          },
          {
            id: "slot-campus-only",
            campusName: "South",
            startsAt: undefined,
            endsAt: undefined,
            maxTesters: 0,
            maxProjects: 0,
            availableTesterCount: 0,
          },
        ],
      },
    ]);

    expect(event).toMatchObject({
      id: "",
      title: "Multi-slot lab",
      description: "Two rooms and two times.",
      mode: "Online",
      location: "North - Room 2, South",
      startsAt: "2026-09-10T18:00:00.000Z",
      endsAt: "2026-09-11T20:00:00.000Z",
      testerLimit: 0,
      projectLimit: 0,
      availableTesterCount: 0,
      scheduleCount: 3,
    });
  });
});
