import type {
  TestingLabQuestionnaireSchema,
  TestingLabTestingEventConfigurationProjection,
} from "@game-guild/client";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  configure: vi.fn(),
}));

vi.mock("@/lib/testing-lab/events-actions", () => ({
  configureTestingEvent: mocks.configure,
}));

vi.mock("./questionnaire-builder", () => ({
  QuestionnaireBuilder: ({
    value,
    onChange,
  }: {
    value: TestingLabQuestionnaireSchema;
    onChange: (value: TestingLabQuestionnaireSchema) => void;
  }) => (
    <div>
      <span>{value.title}: {value.questions.length} questions</span>
      <button
        type="button"
        onClick={() =>
          onChange({
            ...value,
            questions: [
              {
                id: `${value.title}-question`,
                type: "ShortText",
                prompt: "Why?",
                required: true,
              },
            ],
          })
        }
      >
        Add question to {value.title}
      </button>
    </div>
  ),
}));

import { TestingEventConfigurationEditor } from "./testing-event-configuration-editor";

const configuration = {
  generalRules: "Respect the code of conduct.",
  candidateInstructions: "Bring a playable build.",
  testerInstructions: "Complete every assigned task.",
  projectApplicationSchema: {
    title: "Application",
    questions: [
      { id: "application-1", type: "ShortText", prompt: "Build URL", required: true },
    ],
  },
  testerRegistrationSchema: {
    title: "Registration",
    questions: [
      { id: "registration-1", type: "LongText", prompt: "Needs", required: false },
    ],
  },
  sourceTemplateRevisionId: "revision-7",
} as TestingLabTestingEventConfigurationProjection;

describe("TestingEventConfigurationEditor", () => {
  it("renders a frozen, read-only snapshot after draft", () => {
    render(
      <TestingEventConfigurationEditor
        eventId="event-1"
        status="ApplicationsOpen"
        configuration={configuration}
      />,
    );

    expect(screen.getByText("Frozen")).toBeInTheDocument();
    expect(screen.getByText("Respect the code of conduct.")).toBeInTheDocument();
    expect(screen.getAllByText("1 questions")).toHaveLength(2);
    expect(screen.getByText("revision-7")).toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("uses explicit read-only fallbacks for an empty snapshot", () => {
    render(
      <TestingEventConfigurationEditor
        eventId="event-1"
        status="Completed"
      />,
    );

    expect(screen.getAllByText("Not provided")).toHaveLength(3);
    expect(screen.getAllByText("0 questions")).toHaveLength(2);
    expect(screen.queryByText("Template revision")).not.toBeInTheDocument();
  });

  it("initializes a draft, updates both schemas, and submits the complete package", async () => {
    const user = userEvent.setup();
    mocks.configure.mockResolvedValueOnce({
      success: true,
      message: "Event configuration saved.",
      data: { id: "event-1" },
    });
    render(<TestingEventConfigurationEditor eventId="event-1" />);

    expect(screen.getByText("Project application: 0 questions")).toBeInTheDocument();
    expect(screen.getByText("Tester registration: 0 questions")).toBeInTheDocument();
    await user.type(screen.getByLabelText("General rules"), "Respect others");
    await user.type(screen.getByLabelText("Candidate instructions"), "Upload build");
    await user.type(screen.getByLabelText("Tester instructions"), "Play twice");
    await user.click(screen.getByRole("button", { name: "Add question to Project application" }));
    await user.click(screen.getByRole("button", { name: "Add question to Tester registration" }));
    await user.click(screen.getByRole("button", { name: "Save draft configuration" }));

    await waitFor(() => expect(mocks.configure).toHaveBeenCalledOnce());
    const submitted = mocks.configure.mock.calls[0]![0] as FormData;
    expect(Object.fromEntries(submitted.entries())).toMatchObject({
      eventId: "event-1",
      generalRules: "Respect others",
      candidateInstructions: "Upload build",
      testerInstructions: "Play twice",
    });
    expect(JSON.parse(String(submitted.get("projectApplicationSchemaJson")))).toMatchObject({
      title: "Project application",
      questions: [{ prompt: "Why?" }],
    });
    expect(JSON.parse(String(submitted.get("testerRegistrationSchemaJson")))).toMatchObject({
      title: "Tester registration",
      questions: [{ prompt: "Why?" }],
    });
    expect(await screen.findByText("Event configuration saved.")).toBeInTheDocument();
  });

  it("renders validation failures returned by the server", async () => {
    const user = userEvent.setup();
    mocks.configure.mockResolvedValueOnce({ success: false, error: "Rules are required." });
    render(
      <TestingEventConfigurationEditor
        eventId="event-1"
        configuration={configuration}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Save draft configuration" }));
    expect(await screen.findByText("Rules are required.")).toBeInTheDocument();
  });

  it.each([
    [new Error("Network unavailable"), "Network unavailable"],
    ["untyped failure", "The Testing Lab operation failed."],
  ])("turns a thrown action into visible feedback", async (failure, message) => {
    const user = userEvent.setup();
    mocks.configure.mockRejectedValueOnce(failure);
    render(
      <TestingEventConfigurationEditor
        eventId="event-1"
        configuration={configuration}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Save draft configuration" }));
    expect(await screen.findByText(message)).toBeInTheDocument();
  });
});
