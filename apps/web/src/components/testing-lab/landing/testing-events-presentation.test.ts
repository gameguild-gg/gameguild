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
        slots: [],
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
});
