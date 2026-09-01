import "@testing-library/jest-dom/vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ImgHTMLAttributes, ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { CourseShowcase } from "@/lib/courses/public-programs";
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

// The live catalog only serves showcases for courses that exist in production
// (ai4games, intro2gpro), and neither has projects/journey data. The gallery
// and journey rendering paths are covered through this fixture showcase, keyed
// to the advancedAiCourse slug below ("advanced-ai-fixture").
vi.mock("@/lib/courses/public-programs", async () => {
  const actual =
    await vi.importActual<typeof import("@/lib/courses/public-programs")>(
      "@/lib/courses/public-programs",
    );

  const advancedAiShowcase: CourseShowcase = {
    slug: "advanced-ai-fixture",
    programSlug: "game-ai-systems",
    headline:
      "Push beyond fundamentals into tactical AI, influence maps, and production-minded behavior systems.",
    studioPrompt:
      "A deeper AI sequence for students ready to reason about spatial data, tactical choices, and advanced systems.",
    projectResult:
      "A tactical AI prototype using influence, scoring, or advanced decision-making techniques.",
    instructorModel:
      "Advanced technical walkthroughs with implementation details and portfolio framing.",
    portfolioProof:
      "A stronger systems artifact for gameplay programming and technical design portfolios.",
    outcomes: [
      "Design influence-map data that helps agents evaluate danger, pressure, and opportunity.",
      "Build tactical scoring rules that choose actions from readable gameplay constraints.",
      "Combine advanced agent behaviors into a prototype that feels intentional instead of scripted.",
      "Document systems clearly enough for a portfolio review or technical interview.",
    ],
    prerequisites: [
      "Prior game programming practice",
      "Basic AI/pathfinding familiarity",
      "Comfort debugging systems",
    ],
    projects: [
      {
        title: "Influence-map arena",
        summary:
          "Build a tactical top-down scenario where agents read pressure, danger, cover, and opportunity from a spatial influence layer.",
        image:
          "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop",
        skills: ["Influence maps", "Spatial reasoning", "Debug overlays"],
        deliverable:
          "A playable arena with visualized influence values and a written note explaining how the map changes agent behavior.",
        moduleLabel: "Project 01",
      },
      {
        title: "Decision scoring encounter",
        summary:
          "Prototype an encounter where AI chooses movement, attack, retreat, or support behaviors from transparent utility scores.",
        image:
          "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop",
        skills: ["Utility AI", "Tactical scoring", "Behavior debugging"],
        deliverable:
          "A score-driven behavior loop with inspector output that makes each action choice reviewable.",
        moduleLabel: "Project 02",
      },
      {
        title: "Prototype polish pass",
        summary:
          "Package the final AI prototype with readable tuning controls, gameplay framing, and a portfolio-ready implementation breakdown.",
        image:
          "https://images.unsplash.com/photo-1518770660439-4636190af475?w=1400&h=900&fit=crop",
        skills: ["Tuning controls", "Portfolio framing", "Technical writing"],
        deliverable:
          "A short technical case study and recorded walkthrough that show the system, constraints, and tradeoffs.",
        moduleLabel: "Project 03",
      },
    ],
    journey: [
      {
        label: "01",
        title: "Spatial reasoning map",
        body: "Model the tactical space, decide which signals matter, and create a readable influence-map debug view.",
        checkpoint:
          "A map overlay that exposes danger, pressure, and opportunity values.",
        projectTitle: "Influence-map arena",
      },
      {
        label: "02",
        title: "Action scoring rules",
        body: "Turn tactical context into weighted scores that explain why an agent moves, attacks, retreats, or waits.",
        checkpoint: "A scoring table that can be inspected while the encounter runs.",
        projectTitle: "Decision scoring encounter",
      },
      {
        label: "03",
        title: "Behavior composition",
        body: "Combine movement, targeting, and tactical preferences into an encounter that reads as intentional play.",
        checkpoint:
          "A playable prototype with at least two distinct agent responses.",
        projectTitle: "Decision scoring encounter",
      },
      {
        label: "04",
        title: "Stress test and tune",
        body: "Change scenario conditions, test failure cases, and tune weights so the AI remains legible under pressure.",
        checkpoint: "A tuning pass with notes on what changed and why.",
        projectTitle: "Prototype polish pass",
      },
      {
        label: "05",
        title: "Portfolio-ready AI breakdown",
        body: "Package the final build with diagrams, implementation notes, and a concise explanation of design tradeoffs.",
        checkpoint:
          "A publishable case-study outline plus final prototype walkthrough.",
        projectTitle: "Prototype polish pass",
      },
    ],
    faq: [
      {
        question: "Should I take AI for Games first?",
        answer:
          "It is recommended unless you already have implementation experience with game AI fundamentals.",
      },
      {
        question: "What makes this advanced?",
        answer:
          "The work emphasizes layered decision systems, spatial reasoning, and technical tradeoffs.",
      },
    ],
  };

  return {
    ...actual,
    getCourseShowcase: (courseSlug: string | null | undefined) =>
      courseSlug === advancedAiShowcase.slug ? advancedAiShowcase : null,
  };
});

const advancedAiCourse = {
  id: "advanced-ai-fixture-program-1",
  title: "Advanced Game AI",
  description:
    "Master advanced AI techniques for game development, including finite state machines, behavior trees, utility AI, minimax search, Monte Carlo methods, and production AI patterns.",
  slug: "advanced-ai-fixture",
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
          slug: "portfolio-fixture",
          category: "Portfolio",
          description: "Turn a project into a clear professional case study.",
        }}
        viewerAccess={{ state: "signed-out" }}
      />,
    );

    expect(screen.queryByText(/build tactical ai/i)).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", {
        name: /build a portfolio project that proves what you can do/i,
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
        "/learn/courses/advanced-ai-fixture/content",
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
      screen.getAllByText(
        "Checkout advanced-ai-fixture with Advanced AI Course Access",
      ),
    ).toHaveLength(2);
    expect(
      screen.queryByText("Enroll in advanced-ai-fixture"),
    ).not.toBeInTheDocument();
  });
});
