import type {
  TestingLabQuestionnaireOutput,
  TestingLabQuestionnaireSchema,
  TestingLabTestingProjectBrief,
} from "@game-guild/client";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  withdraw: vi.fn(),
  fetch: vi.fn(),
}));

vi.mock("@/lib/testing-lab/events-actions", () => ({
  withdrawTestingProjectApplication: mocks.withdraw,
}));

vi.mock("./questionnaire-builder", () => ({
  QuestionnaireBuilder: ({
    value,
    onChange,
  }: {
    value: TestingLabQuestionnaireSchema;
    onChange: (value: TestingLabQuestionnaireSchema) => void;
  }) => (
    <button
      type="button"
      onClick={() =>
        onChange({
          ...value,
          questions: [
            { id: "feedback-1", prompt: "What worked?", type: "FreeText", required: true },
          ],
        })
      }
    >
      Update feedback form
    </button>
  ),
}));

vi.mock("./questionnaire-fieldset", () => ({
  QuestionnaireFieldset: ({
    value,
    onChange,
    onComplete,
    submitLabel,
  }: {
    value: TestingLabQuestionnaireOutput;
    onChange: (value: TestingLabQuestionnaireOutput) => void;
    onComplete?: () => void;
    submitLabel?: string;
  }) => (
    <div>
      <button
        type="button"
        onClick={() =>
          onChange({
            answers: [
              { questionId: "event-1", textValue: "Ready", selectedOptionIds: [] },
            ],
          })
        }
      >
        Change event answer ({value.answers?.length ?? 0})
      </button>
      <button type="button" onClick={onComplete}>
        {submitLabel}
      </button>
    </div>
  ),
}));

vi.stubGlobal("fetch", mocks.fetch);

import { TestingProjectApplication } from "./testing-project-application";

const versions = [
  {
    id: "version-1",
    projectId: "project-1",
    projectTitle: "Asterion",
    versionNumber: "1.0.0",
    status: "ReadyForTesting",
  },
  {
    id: "version-2",
    projectId: "project-2",
    projectTitle: "Wayfinder",
    versionNumber: "2.0.0",
    status: "Released",
  },
  {
    id: "draft-version",
    projectId: "project-3",
    projectTitle: "Draft game",
    versionNumber: "0.1.0",
    status: "Draft",
  },
];

function fetchResult(result: unknown) {
  return Promise.resolve({ json: () => Promise.resolve(result) });
}

describe("TestingProjectApplication extended workflow", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.fetch.mockImplementation(() =>
      fetchResult({
        success: true,
        message: "Draft saved.",
        data: { id: "application-1", projectId: "project-1", status: "Draft" },
      }),
    );
  });

  it("completes the full draft workflow and submits normalized package data", async () => {
    const user = userEvent.setup();
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-1"
        projectVersions={versions}
        candidateInstructions="Upload a playable build."
        generalRules="Respect the community rules."
        requiresFeedback
      />,
    );

    expect(screen.queryByRole("option", { name: /Draft game/ })).not.toBeInTheDocument();
    await user.selectOptions(
      screen.getByRole("combobox", { name: "Eligible project version" }),
      "version-2",
    );
    await user.selectOptions(
      screen.getByRole("combobox", { name: "Eligible project version" }),
      "version-1",
    );
    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    expect(await screen.findByText("Upload a playable build.")).toBeInTheDocument();
    expect(screen.getByText(/Saved application applicat/)).toBeInTheDocument();

    await user.type(screen.getByLabelText("Test objective"), "Validate onboarding");
    await user.type(screen.getByLabelText("Installation and access"), "Download and unzip");
    fireEvent.change(screen.getByLabelText("Test tasks (one per line)"), {
      target: { value: "Launch game\n\nFinish tutorial" },
    });
    await user.type(screen.getByLabelText("Controls"), "Keyboard");
    await user.type(screen.getByLabelText("Known limitations"), "No audio");
    fireEvent.change(screen.getByLabelText("Links (one absolute URL per line)"), {
      target: { value: "https://example.com\n https://docs.example.com " },
    });
    fireEvent.change(screen.getByLabelText("Existing asset reference IDs (optional, one per line)"), {
      target: { value: "asset-1\n\n asset-2 " },
    });
    await user.click(screen.getByRole("button", { name: "Save and continue" }));

    await user.click(screen.getByRole("button", { name: "Update feedback form" }));
    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    await user.click(screen.getByRole("button", { name: "Back to feedback form" }));
    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    await user.click(screen.getByRole("button", { name: /Change event answer/ }));
    await user.click(screen.getByRole("button", { name: "Review application" }));

    expect(await screen.findByText("Asterion · 1.0.0")).toBeInTheDocument();
    expect(screen.getByText("2 test tasks · 1 developer questions")).toBeInTheDocument();
    expect(screen.getByText("Respect the community rules.")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Back" }));
    await user.click(screen.getByRole("button", { name: "Review application" }));
    await user.type(screen.getByLabelText("Preferred availability"), "Weekday evenings");
    await user.click(screen.getByRole("checkbox", { name: /accept the frozen rules/ }));
    await user.click(screen.getByRole("button", { name: "Submit for review" }));

    await waitFor(() => expect(mocks.fetch).toHaveBeenCalledTimes(7));
    const request = mocks.fetch.mock.calls.at(-1)![1] as RequestInit;
    const body = JSON.parse(String(request.body));
    expect(body).toMatchObject({
      eventId: "event-1",
      projectId: "project-1",
      applicationId: "application-1",
      projectVersionId: "version-1",
      acceptedRules: true,
      preferredAvailability: "Weekday evenings",
      submittedAssetReferenceIds: ["asset-1", "asset-2"],
      intent: "submit",
      brief: {
        testObjective: "Validate onboarding",
        testTasks: ["Launch game", "Finish tutorial"],
        links: ["https://example.com", "https://docs.example.com"],
      },
      feedbackQuestionnaire: { questions: [{ id: "feedback-1" }] },
      eventApplicationResponse: { answers: [{ questionId: "event-1" }] },
    });
    expect(request).toMatchObject({
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
    });
  });

  it("moves backward through the wizard without crossing the first step", async () => {
    const user = userEvent.setup();
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-1"
        projectVersions={[versions[0]!]}
      />,
    );
    expect(screen.getByRole("button", { name: "Previous" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    await user.click(screen.getByRole("button", { name: "Previous" }));
    expect(screen.getByRole("combobox", { name: "Eligible project version" })).toBeInTheDocument();
  });

  it.each([
    [{ success: false, error: "Server validation failed." }, "Server validation failed."],
    [null, "The application draft could not be saved."],
  ])("renders unsuccessful draft persistence", async (result, message) => {
    const user = userEvent.setup();
    mocks.fetch.mockImplementationOnce(() => fetchResult(result));
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-1"
        projectVersions={versions}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Save progress" }));
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Eligible project version" })).toBeInTheDocument();
  });

  it("handles a network failure while saving a draft", async () => {
    const user = userEvent.setup();
    mocks.fetch.mockRejectedValueOnce(new Error("Network offline"));
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-1"
        projectVersions={versions}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Save progress" }));
    expect(await screen.findByText("The application draft could not be saved.")).toBeInTheDocument();
  });

  it("handles an unreadable JSON response while saving a draft", async () => {
    const user = userEvent.setup();
    mocks.fetch.mockResolvedValueOnce({
      json: () => Promise.reject(new SyntaxError("Invalid JSON")),
    });
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-1"
        projectVersions={versions}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Save progress" }));
    expect(await screen.findByText("The application draft could not be saved.")).toBeInTheDocument();
  });

  it("keeps a successful draft response without optional application data", async () => {
    const user = userEvent.setup();
    mocks.fetch.mockImplementationOnce(() =>
      fetchResult({ success: true, message: "Draft accepted without a projection." }),
    );
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        application={{ id: "application-1", projectId: "project-1", status: "Draft" }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Save progress" }));
    expect(await screen.findByText("Draft accepted without a projection.")).toBeInTheDocument();
    const body = JSON.parse(String((mocks.fetch.mock.calls[0]![1] as RequestInit).body));
    expect(body.projectVersionId).toBeUndefined();
  });

  it("normalizes incomplete persisted questionnaire and brief projections", async () => {
    const user = userEvent.setup();
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        application={{
          id: "application-1",
          projectId: "project-1",
          projectVersionId: "version-1",
          status: "Draft",
          brief: {} as TestingLabTestingProjectBrief,
          feedbackQuestionnaire: {} as TestingLabQuestionnaireSchema,
        }}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    expect(screen.getByLabelText("Test objective")).toHaveValue("");
    expect(screen.getByLabelText("Installation and access")).toHaveValue("");
    expect(screen.getByLabelText("Controls")).toHaveValue("");
    expect(screen.getByLabelText("Known limitations")).toHaveValue("");
    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    await user.click(screen.getByRole("button", { name: "Save and continue" }));
    await user.click(screen.getByRole("button", { name: "Review application" }));
    expect(await screen.findByText("0 test tasks · 0 developer questions")).toBeInTheDocument();
  });

  it("treats an application without a status as active for project deduplication", () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        applications={[{ id: "application-1", projectId: "project-1" }]}
      />,
    );
    expect(screen.getAllByRole("combobox", { name: "Eligible project version" })).toHaveLength(1);
  });

  it("updates a pending immutable application without allowing a version change", async () => {
    const user = userEvent.setup();
    mocks.fetch.mockImplementation(() =>
      fetchResult({
        success: true,
        message: "Pending application saved.",
        data: { id: "application-1", projectId: "project-1", status: "Pending" },
      }),
    );
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        application={{
          id: "application-1",
          projectId: "project-1",
          projectVersionId: "version-1",
          status: "Pending",
          submissionVersionPolicy: "ReleasedImmutable",
          rulesAcceptedAt: "2026-09-01",
          preferredAvailability: "Morning",
          submittedAssetReferenceIds: ["asset-1"],
          brief: {
            testObjective: "Objective",
            installationAndAccess: "Install",
            testTasks: ["Play"],
            controls: "Keyboard",
            knownLimitations: "None",
            links: [],
          },
        }}
      />,
    );
    expect(screen.getByRole("combobox", { name: "Eligible project version" })).toBeDisabled();
    for (let step = 0; step < 3; step += 1) {
      const continueButton = screen.getByRole("button", { name: "Save and continue" });
      await waitFor(() => expect(continueButton).toBeEnabled());
      await user.click(continueButton);
    }
    const reviewButton = await screen.findByRole("button", { name: "Review application" });
    await waitFor(() => expect(reviewButton).toBeEnabled());
    await user.click(reviewButton);
    const updateButton = await screen.findByRole("button", { name: "Update pending application" });
    await waitFor(() => expect(updateButton).toBeEnabled());
    await user.click(updateButton);
    await waitFor(() => expect(mocks.fetch).toHaveBeenCalledTimes(5));
    const body = JSON.parse(String((mocks.fetch.mock.calls.at(-1)![1] as RequestInit).body));
    expect(body.intent).toBe("save");
  });

  it("freezes applications when submissions are closed", () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications={false}
        projectVersions={versions}
        application={{ id: "application-1", status: "Draft" }}
      />,
    );
    expect(screen.getByText(/package is frozen/)).toBeInTheDocument();
    expect(screen.getByText("Applications are closed.")).toBeInTheDocument();
    expect(screen.queryByText(/decision rationale/i)).not.toBeInTheDocument();
  });

  it("does not create a duplicate wizard for an active project", () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        applications={[
          { id: "application-1", projectId: "project-1", projectVersionId: "version-1", status: "Pending" },
        ]}
      />,
    );
    expect(screen.getAllByText("Pending")).toHaveLength(1);
    expect(screen.getAllByRole("combobox", { name: "Eligible project version" })).toHaveLength(1);
  });

  it("withdraws an application and renders action failures", async () => {
    const user = userEvent.setup();
    mocks.withdraw.mockResolvedValueOnce({ success: true, message: "Application withdrawn." });
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        application={{ id: "application-1", projectId: "project-1", projectVersionId: "version-1", status: "UnderReview" }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Withdraw application" }));
    await waitFor(() => expect(mocks.withdraw).toHaveBeenCalledOnce());
    expect(Object.fromEntries((mocks.withdraw.mock.calls[0]![0] as FormData).entries())).toEqual({
      eventId: "event-1",
      applicationId: "application-1",
    });
    expect(await screen.findByText("Application withdrawn.")).toBeInTheDocument();
  });

  it.each([
    [new Error("Withdrawal offline"), "Withdrawal offline"],
    ["failure", "The Testing Lab operation failed."],
  ])("turns thrown withdrawal failures into visible errors", async (failure, message) => {
    const user = userEvent.setup();
    mocks.withdraw.mockRejectedValueOnce(failure);
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[versions[0]!]}
        application={{ id: "application-1", projectId: "project-1", projectVersionId: "version-1", status: "Waitlisted" }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Withdraw application" }));
    expect(await screen.findByText(message)).toBeInTheDocument();
  });
});
