import "@testing-library/jest-dom/vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ImgHTMLAttributes, ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { CourseLandingPage } from "./course-landing-page";

type MockImageProps = ImgHTMLAttributes<HTMLImageElement> & {
  alt: string;
  src: string;
  fill?: boolean;
  unoptimized?: boolean;
  priority?: boolean;
};

vi.mock("next/image", () => ({
  default: (props: MockImageProps) => {
    const { alt, unoptimized, ...imageProps } = props;
    delete imageProps.fill;
    delete imageProps.priority;

    // eslint-disable-next-line @next/next/no-img-element
    return (
      <img
        alt={alt}
        data-unoptimized={unoptimized ? "true" : "false"}
        {...imageProps}
      />
    );
  },
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    children,
    href,
    ...rest
  }: {
    children: ReactNode;
    href: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock("./course-self-enroll-button", () => ({
  CourseSelfEnrollButton: ({ courseSlug }: { courseSlug: string }) => (
    <button type="button">Enroll in {courseSlug}</button>
  ),
}));

vi.mock("./course-checkout-button", () => ({
  CourseCheckoutButton: ({
    courseSlug,
    products,
  }: {
    courseSlug: string;
    products: Array<{ name: string }>;
  }) => (
    <button type="button">
      Checkout {courseSlug} with {products[0]?.name}
    </button>
  ),
}));

const advancedAiCourse = {
  id: "ai4games2-program-1",
  title: "Advanced Game AI",
  description:
    "Master advanced AI techniques for game development, including finite state machines, behavior trees, utility AI, minimax search, Monte Carlo methods, and production AI patterns.",
  slug: "ai4games2",
  thumbnail:
    "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop",
  estimatedHours: 60,
  category: "GameDevelopment",
  difficulty: "Intermediate",
  isEnrollmentOpen: true,
  visibility: "Public",
  status: "Published",
  programContents: null,
};

describe("CourseLandingPage", () => {
  it("renders a project gallery and checkpoint-based course journey for advanced AI", () => {
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={course}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    const projectGallery = screen.getByRole("region", {
      name: /project gallery/i,
    });
    expect(
      within(projectGallery).getByRole("heading", {
        name: /Influence-map arena/i,
      }),
    ).toBeInTheDocument();
    expect(
      within(projectGallery).getByRole("heading", {
        name: /Decision scoring encounter/i,
      }),
    ).toBeInTheDocument();
    expect(
      within(projectGallery).getByRole("heading", {
        name: /Prototype polish pass/i,
      }),
    ).toBeInTheDocument();
    expect(within(projectGallery).getAllByText(/Deliverable/i)).toHaveLength(3);

    const courseJourney = screen.getByRole("region", {
      name: /course journey/i,
    });
    expect(
      within(courseJourney).getAllByText(/Checkpoint output/i),
    ).toHaveLength(5);
    expect(
      within(courseJourney).getByText(/Spatial reasoning map/i),
    ).toBeInTheDocument();
    expect(
      within(courseJourney).getByText(/Portfolio-ready AI breakdown/i),
    ).toBeInTheDocument();
  });

  it("keeps the decorative hero media behind interactive enrollment controls", () => {
    render(
      <CourseLandingPage
        course={advancedAiCourse}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(screen.getByAltText("Advanced Game AI").parentElement).toHaveClass(
      "pointer-events-none",
    );
    expect(
      screen.getAllByRole("link", { name: /sign in to enroll/i }),
    ).not.toHaveLength(0);
  });

  it("lets students move through the project gallery", async () => {
    const user = userEvent.setup();
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={course}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    const projectGallery = screen.getByRole("region", {
      name: /project gallery/i,
    });
    expect(
      within(projectGallery).getByRole("button", {
        name: /Influence-map arena/i,
      }),
    ).toHaveAttribute("aria-current", "true");

    await user.click(
      within(projectGallery).getByRole("button", {
        name: /show next project/i,
      }),
    );

    expect(
      within(projectGallery).getByRole("button", {
        name: /Decision scoring encounter/i,
      }),
    ).toHaveAttribute("aria-current", "true");
  });

  it("prefers dashboard-editable skills over static showcase outcomes", () => {
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={{
          ...course,
          skillsProvided:
            "Tune combat director pacing, Package readable AI telemetry",
          skillsRequired: "Behavior tree fundamentals, Debugging AI state",
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(screen.getByText("Tune combat director pacing")).toBeInTheDocument();
    expect(
      screen.getByText("Package readable AI telemetry"),
    ).toBeInTheDocument();
    expect(screen.getByText("Behavior tree fundamentals")).toBeInTheDocument();
    expect(screen.getByText("Debugging AI state")).toBeInTheDocument();
  });

  it("uses an edited course description for the hero copy before static showcase text", () => {
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={{
          ...course,
          description: "Instructor-edited hero copy for the public storefront.",
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(
      screen.getByText(
        "Instructor-edited hero copy for the public storefront.",
      ),
    ).toBeInTheDocument();
  });

  it("does not show AI-specific build copy on non-AI courses", () => {
    render(
      <CourseLandingPage
        course={{
          ...advancedAiCourse,
          title: "Portfolio Presentation",
          slug: "portfolio",
          category: "Portfolio",
          description: "Turn a project into a clear professional case study.",
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(screen.queryByText(/build tactical ai/i)).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", {
        name: /turn projects into a portfolio story/i,
      }),
    ).toBeInTheDocument();
  });

  it("renders editor-provided external thumbnail URLs without Next image optimization", () => {
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={{
          ...course,
          title: "External Thumbnail Course",
          thumbnail: "https://example.com/editor-cover.png",
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(screen.getByAltText("External Thumbnail Course")).toHaveAttribute(
      "data-unoptimized",
      "true",
    );
  });

  it("uses dashboard-edited FAQ metadata before static showcase FAQ", () => {
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={{
          ...course,
          metadata: JSON.stringify({
            landingFaq: [
              {
                question: "Is the FAQ editable from the dashboard?",
                answer: "Yes, this public FAQ is metadata-backed.",
              },
            ],
          }),
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(
      screen.getByText("Is the FAQ editable from the dashboard?"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Yes, this public FAQ is metadata-backed."),
    ).toBeInTheDocument();
  });

  it("uses dashboard-edited project metadata before static showcase projects", () => {
    const course = advancedAiCourse;

    render(
      <CourseLandingPage
        course={{
          ...course,
          metadata: JSON.stringify({
            landingProjects: [
              {
                title: "Boss behavior sandbox",
                summary:
                  "Students build a readable boss encounter with inspectable AI states.",
                image: "https://example.com/boss-sandbox.jpg",
                skills: ["State debugging", "Combat pacing"],
                deliverable:
                  "A playable boss encounter with annotated decision logic.",
                moduleLabel: "Project A",
              },
            ],
          }),
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    const projectGallery = screen.getByRole("region", {
      name: /project gallery/i,
    });
    expect(
      within(projectGallery).getByRole("heading", {
        name: /Boss behavior sandbox/i,
      }),
    ).toBeInTheDocument();
    expect(
      within(projectGallery).getByText(
        "A playable boss encounter with annotated decision logic.",
      ),
    ).toBeInTheDocument();
    expect(
      within(projectGallery).getByText("State debugging"),
    ).toBeInTheDocument();
  });

  it("sends enrolled learners to the native learner workspace", () => {
    render(
      <CourseLandingPage
        course={advancedAiCourse}
        viewerAccess={{ state: "has-access" }}
      />,
    );

    const continueLinks = screen.getAllByRole("link", {
      name: /continue learning/i,
    });
    expect(continueLinks).not.toHaveLength(0);
    for (const link of continueLinks) {
      expect(link).toHaveAttribute(
        "href",
        "/learn/courses/ai4games2/content",
      );
    }
  });

  it("renders checkout CTA instead of free enrollment when products are linked", () => {
    render(
      <CourseLandingPage
        course={advancedAiCourse}
        viewerAccess={{ state: "no-access" }}
        products={[
          {
            id: "product-1",
            name: "Advanced AI Course Access",
            price: 49,
            currency: "USD",
          },
        ]}
      />,
    );

    expect(
      screen.getAllByText("Checkout ai4games2 with Advanced AI Course Access"),
    ).toHaveLength(2);
    expect(screen.queryByText("Enroll in ai4games2")).not.toBeInTheDocument();
  });
});
