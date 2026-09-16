import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  enroll: vi.fn(),
  replace: vi.fn(),
}));

vi.mock("@/lib/learner/enrollment-actions", () => ({
  enrollInCourse: mocks.enroll,
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mocks.replace }),
  Link: ({
    children,
    href,
    ...props
  }: {
    children: ReactNode;
    href: string;
  }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

const { CourseAccessGate } = await import("./course-access-gate");

const course = {
  id: "course-1",
  slug: "game-production",
  title: "Game Production",
};

const originalWebUrl = process.env.NEXT_PUBLIC_WEB_URL;

describe("CourseAccessGate", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    delete process.env.NEXT_PUBLIC_WEB_URL;
  });

  afterEach(() => {
    if (originalWebUrl === undefined) {
      delete process.env.NEXT_PUBLIC_WEB_URL;
    } else {
      process.env.NEXT_PUBLIC_WEB_URL = originalWebUrl;
    }
  });

  it("enters course content through the App Router after enrollment", async () => {
    mocks.enroll.mockResolvedValue({ success: true });

    render(
      <CourseAccessGate
        access={
          {
            kind: "enrollment-required",
            course,
          } as never
        }
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Enroll for free" }));

    await waitFor(() => {
      expect(mocks.enroll).toHaveBeenCalledWith("course-1");
      expect(mocks.replace).toHaveBeenCalledWith(
        "/learn/courses/game-production/content",
      );
    });

    expect(screen.getByRole("status")).toHaveTextContent(
      "Enrollment confirmed",
    );
  });

  it("shows enrollment errors without navigating", async () => {
    mocks.enroll.mockResolvedValue({
      success: false,
      error: "Enrollment unavailable",
    });

    render(
      <CourseAccessGate
        access={{ kind: "enrollment-required", course } as never}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Enroll for free" }));

    expect(await screen.findByRole("status")).toHaveTextContent(
      "Enrollment unavailable",
    );
    expect(mocks.replace).not.toHaveBeenCalled();
  });

  it("does not enroll when malformed access data has no course", () => {
    render(
      <CourseAccessGate access={{ kind: "enrollment-required" } as never} />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Enroll for free" }));

    expect(mocks.enroll).not.toHaveBeenCalled();
    expect(screen.getByText("This course")).toBeInTheDocument();
  });

  it("renders checkout pricing and honors the configured storefront URL", () => {
    const { rerender } = render(
      <CourseAccessGate
        access={
          {
            kind: "payment-required",
            course,
            price: null,
            currency: "USD",
          } as never
        }
      />,
    );

    expect(screen.getByText("Purchase required")).toBeInTheDocument();
    expect(screen.getByText("See pricing")).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: /continue to checkout/i }),
    ).toHaveAttribute("href", "http://localhost:3000/courses/game-production");
    expect(
      screen.getByRole("link", { name: /browse catalog/i }),
    ).toHaveAttribute("href", "https://gameguild.gg/courses");

    process.env.NEXT_PUBLIC_WEB_URL = "https://community.example";
    rerender(
      <CourseAccessGate
        access={
          {
            kind: "payment-required",
            course,
            price: 49.5,
            currency: "USD",
          } as never
        }
      />,
    );

    expect(screen.getByText("$49.50")).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: /continue to checkout/i }),
    ).toHaveAttribute(
      "href",
      "https://community.example/courses/game-production",
    );
    expect(
      screen.getByRole("link", { name: /browse catalog/i }),
    ).toHaveAttribute("href", "https://community.example/courses");
  });

  it("explains closed enrollment and unavailable classrooms", () => {
    const { rerender } = render(
      <CourseAccessGate
        access={{ kind: "enrollment-closed", course } as never}
      />,
    );

    expect(screen.getByText("Enrollment is closed")).toBeInTheDocument();
    expect(screen.getByText(/not accepting new learners/i)).toBeInTheDocument();

    rerender(
      <CourseAccessGate
        access={
          {
            kind: "unavailable",
            message: "Course maintenance is in progress.",
          } as never
        }
      />,
    );

    expect(screen.getByText("Classroom unavailable")).toBeInTheDocument();
    expect(
      screen.getAllByText("Course maintenance is in progress."),
    ).toHaveLength(2);
  });
});
