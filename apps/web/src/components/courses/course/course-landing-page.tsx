import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import type { Program, ProgramContent } from '@/lib/api/generated';
import { ProgramContentType } from '@/lib/api/generated';
import type { CourseViewerAccess } from '@/lib/courses/services/course-viewer-access';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import {
  getCourseShowcase,
  getProgramForCourse,
  listCourseContentPreview,
  type CourseJourneyStep,
  type CourseProjectShowcase,
} from '@/lib/courses/public-programs';
import {
  ArrowRight,
  BookOpen,
  BrainCircuit,
  CalendarClock,
  Check,
  CheckCircle2,
  Code2,
  FileText,
  Layers3,
  MessageSquare,
  ShieldCheck,
  Sparkles,
  Target,
} from 'lucide-react';
import Image from 'next/image';
import { CourseHeader } from './course-header';
import { CourseProjectCarousel } from './course-project-carousel';
import { CourseSelfEnrollButton } from './course-self-enroll-button';

interface CourseLandingPageProps {
  readonly course: Program;
  readonly viewerAccess: CourseViewerAccess;
}

function getString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

function normalizeList(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.map((item) => String(item).trim()).filter(Boolean);
  }

  if (typeof value === 'string') {
    return value
      .split(/[,;\n]/)
      .map((item) => item.trim())
      .filter(Boolean);
  }

  return [];
}

function getMetadataFaq(course: Program): Array<{ question: string; answer: string }> {
  const rawMetadata = getString(course.metadata);
  if (!rawMetadata) return [];

  try {
    const parsed = JSON.parse(rawMetadata) as unknown;
    const landingFaq = parsed && typeof parsed === 'object' && !Array.isArray(parsed)
      ? (parsed as { landingFaq?: unknown }).landingFaq
      : null;

    if (!Array.isArray(landingFaq)) return [];

    return landingFaq
      .map((item) => {
        const question = item && typeof item === 'object' ? getString((item as { question?: unknown }).question) : null;
        const answer = item && typeof item === 'object' ? getString((item as { answer?: unknown }).answer) : null;
        return question && answer ? { question, answer } : null;
      })
      .filter((item): item is { question: string; answer: string } => Boolean(item));
  } catch {
    return [];
  }
}

function getMetadataProjects(course: Program, fallbackImage: string): CourseProjectShowcase[] {
  const rawMetadata = getString(course.metadata);
  if (!rawMetadata) return [];

  try {
    const parsed = JSON.parse(rawMetadata) as unknown;
    const landingProjects = parsed && typeof parsed === 'object' && !Array.isArray(parsed)
      ? (parsed as { landingProjects?: unknown }).landingProjects
      : null;

    if (!Array.isArray(landingProjects)) return [];

    return landingProjects
      .map((item, index): CourseProjectShowcase | null => {
        const title = item && typeof item === 'object' ? getString((item as { title?: unknown }).title) : null;
        const summary = item && typeof item === 'object' ? getString((item as { summary?: unknown }).summary) : null;
        const deliverable = item && typeof item === 'object' ? getString((item as { deliverable?: unknown }).deliverable) : null;

        if (!title || !summary || !deliverable) return null;

        return {
          title,
          summary,
          image: getString((item as { image?: unknown }).image) ?? fallbackImage,
          skills: normalizeList((item as { skills?: unknown }).skills),
          deliverable,
          moduleLabel: getString((item as { moduleLabel?: unknown }).moduleLabel) ?? `Project ${String(index + 1).padStart(2, '0')}`,
        };
      })
      .filter((item): item is CourseProjectShowcase => Boolean(item))
      .slice(0, 6);
  } catch {
    return [];
  }
}

function getContentTypeName(type: number | null | undefined): string {
  switch (type) {
    case ProgramContentType.Page:
      return 'Lesson page';
    case ProgramContentType.Assignment:
      return 'Assignment';
    case ProgramContentType.Questionnaire:
      return 'Questionnaire';
    case ProgramContentType.Discussion:
      return 'Discussion';
    case ProgramContentType.Code:
      return 'Code lab';
    case ProgramContentType.Challenge:
      return 'Challenge';
    case ProgramContentType.Reflection:
      return 'Reflection';
    case ProgramContentType.Survey:
      return 'Survey';
    case ProgramContentType.Lesson:
    default:
      return 'Lesson';
  }
}

function getContentIcon(type: number | null | undefined) {
  switch (type) {
    case ProgramContentType.Assignment:
    case ProgramContentType.Code:
    case ProgramContentType.Challenge:
      return Code2;
    case ProgramContentType.Discussion:
    case ProgramContentType.Questionnaire:
      return MessageSquare;
    case ProgramContentType.Page:
    case ProgramContentType.Reflection:
    case ProgramContentType.Survey:
    case ProgramContentType.Lesson:
    default:
      return BookOpen;
  }
}

function getTopLevelMinutes(contents: ProgramContent[]): number {
  return contents.reduce((total, item) => total + (typeof item.estimatedMinutes === 'number' ? item.estimatedMinutes : 0), 0);
}

function getCourseTitle(course: Program): string {
  return getString(course.title) ?? 'GameGuild course';
}

function getCourseImage(course: Program): string {
  const slug = getString(course.slug);
  const program = getProgramForCourse(slug);

  return getString(course.thumbnail) ?? program?.image ?? 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop';
}

const COURSE_SECTION_VISUALS: Record<string, { intro: string; project: string; program: string }> = {
  ai4games: {
    intro: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    project: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop',
    program: 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=1400&h=900&fit=crop',
  },
  ai4games2: {
    intro: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    project: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop',
    program: 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=1400&h=900&fit=crop',
  },
  networking: {
    intro: 'https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=1400&h=900&fit=crop',
    project: 'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=1400&h=900&fit=crop',
    program: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
  },
};

function getCourseSectionVisuals(slug: string | null, heroImage: string, programImage?: string | null) {
  const visuals = slug ? COURSE_SECTION_VISUALS[slug] : null;

  return {
    intro: visuals?.intro ?? programImage ?? heroImage,
    project: visuals?.project ?? heroImage,
    program: visuals?.program ?? programImage ?? heroImage,
  };
}

function getViewerCta(
  course: Program,
  viewerAccess: CourseViewerAccess,
): { label: string; href?: string; kind: 'continue' | 'sign-in' | 'enroll' | 'closed' | 'unavailable' } {
  if (viewerAccess.state === 'has-access' && course.slug) {
    return { label: 'Continue learning', href: `/courses/${course.slug}/content`, kind: 'continue' };
  }

  if (viewerAccess.state === 'signed-out') {
    return { label: 'Sign in to enroll', href: '/sign-in', kind: 'sign-in' };
  }

  if (viewerAccess.state === 'no-access' && course.isEnrollmentOpen && course.slug) {
    return { label: 'Enroll now', kind: 'enroll' };
  }

  if (viewerAccess.state === 'unavailable') {
    return { label: 'Access check unavailable', kind: 'unavailable' };
  }

  return { label: 'Enrollment closed', kind: 'closed' };
}

function makeProjectSlides(
  outcomes: string[],
  courseTitle: string,
  projectResult: string,
  heroImage: string,
  projectImage: string,
  programImage: string,
): CourseProjectShowcase[] {
  const fallbackImages = [projectImage, heroImage, programImage];
  const fallbackTitles = ['System sketch', 'Playable build', 'Portfolio package'];
  const fallbackSkills = [
    ['Problem framing', 'Prototype scope', 'Debug view'],
    ['Applied implementation', 'Iteration loop', 'Reviewable behavior'],
    ['Portfolio framing', 'Technical writing', 'Presentation polish'],
  ];

  return [0, 1, 2].map((index) => {
    const outcome = outcomes[index] ?? projectResult;
    const summary = outcome.endsWith('.') ? outcome : `${outcome}.`;

    return {
      title: index === 2 ? `${courseTitle} final artifact` : fallbackTitles[index],
      summary,
      image: fallbackImages[index],
      skills: fallbackSkills[index],
      deliverable: index === 2 ? projectResult : `A checkpoint artifact that makes ${outcome.toLowerCase()} visible for review.`,
      moduleLabel: `Project ${String(index + 1).padStart(2, '0')}`,
    };
  });
}

function makeJourneyRows(contentPreview: ProgramContent[], outcomes: string[], title: string): CourseJourneyStep[] {
  if (contentPreview.length > 0) {
    return contentPreview.slice(0, 6).map((content, index) => ({
      label: String(index + 1).padStart(2, '0'),
      title: getString(content.title) ?? `Module ${index + 1}`,
      body: getString(content.description) ?? getContentTypeName(content.type),
      checkpoint: `${getContentTypeName(content.type)} completed with a reviewable note or artifact.`,
      projectTitle: index < 2 ? 'System sketch' : index < 4 ? 'Playable build' : `${title} final artifact`,
      minutes: content.estimatedMinutes,
      type: content.type,
    }));
  }

  return [
    {
      label: '01',
      title: 'Map the system',
      body: `Understand the production problem behind ${title}.`,
      checkpoint: 'A concise technical map of the system, constraints, and success criteria.',
      projectTitle: 'System sketch',
    },
    ...outcomes.slice(0, 4).map((outcome, index) => ({
      label: String(index + 2).padStart(2, '0'),
      title: ['Prototype the pattern', 'Stress the behavior', 'Document the tradeoff', 'Polish the artifact'][index] ?? `Stage ${index + 2}`,
      body: outcome,
      checkpoint:
        ['A working prototype slice.', 'A stress-test note with the main failure case.', 'A technical tradeoff explanation.', 'A polished portfolio section.'][
          index
        ] ?? 'A reviewable course checkpoint.',
      projectTitle: index < 2 ? 'Playable build' : `${title} final artifact`,
    })),
    {
      label: '06',
      title: 'Present the result',
      body: 'Package the final work so it can be reviewed, discussed, and reused.',
      checkpoint: 'A final walkthrough with what changed, what works, and what you would improve next.',
      projectTitle: `${title} final artifact`,
    },
  ].slice(0, 6);
}

export function CourseLandingPage({ course, viewerAccess }: CourseLandingPageProps) {
  const slug = getString(course.slug);
  const title = getCourseTitle(course);
  const heroImage = getCourseImage(course);
  const showcase = getCourseShowcase(slug);
  const program = getProgramForCourse(slug);
  const sectionVisuals = getCourseSectionVisuals(slug, heroImage, program?.image);
  const categoryName = getCourseCategoryName(course.category as string | number | null | undefined);
  const level = getCourseLevelConfig(course.difficulty as string | number | null | undefined).name;
  const contentPreview = listCourseContentPreview(course.programContents, 8);
  const contentCount = course.programContents?.length ?? 0;
  const previewMinutes = getTopLevelMinutes(contentPreview);
  const publishedSkills = normalizeList(course.skillsProvided);
  const requiredSkills = normalizeList(course.skillsRequired);
  const outcomes = publishedSkills.length
    ? publishedSkills
    : showcase?.outcomes.length
      ? showcase.outcomes
      : [
          `Understand the core craft behind ${categoryName.toLowerCase()}.`,
          'Build practical work that can be reviewed and improved.',
          'Turn course practice into a public portfolio artifact.',
        ];
  const prerequisites = requiredSkills.length
    ? requiredSkills
    : showcase?.prerequisites.length
      ? showcase.prerequisites
      : [level === 'Beginner' ? 'No advanced background required.' : `${level} comfort with the course discipline is recommended.`];
  const projectResult = showcase?.projectResult ?? `A practical ${categoryName.toLowerCase()} project that demonstrates what students learned.`;
  const metadataProjects = getMetadataProjects(course, sectionVisuals.project);
  const projectSlides = metadataProjects.length
    ? metadataProjects
    : publishedSkills.length
    ? makeProjectSlides(outcomes, title, projectResult, heroImage, sectionVisuals.project, sectionVisuals.program)
    : showcase?.projects?.length
      ? showcase.projects
      : makeProjectSlides(outcomes, title, projectResult, heroImage, sectionVisuals.project, sectionVisuals.program);
  const journeyRows = contentPreview.length > 0 || publishedSkills.length
    ? makeJourneyRows(contentPreview, outcomes, title)
    : showcase?.journey?.length
      ? showcase.journey
      : makeJourneyRows(contentPreview, outcomes, title);
  const viewerCta = getViewerCta(course, viewerAccess);
  const finalArtifactLabel = title.toLowerCase().includes('ai') ? 'AI prototype' : 'Portfolio piece';
  const metadataFaq = getMetadataFaq(course);
  const faq = metadataFaq.length > 0 ? metadataFaq : showcase?.faq ?? [
    {
      question: 'Can I take this as a standalone course?',
      answer: 'Yes. Program packages clarify the recommended path, but each course has its own landing page and enrollment state.',
    },
    {
      question: 'What do I leave with?',
      answer: projectResult,
    },
  ];

  return (
    <div className="min-h-screen overflow-hidden bg-[#05070d] text-white">
      <CourseHeader course={course} viewerAccess={viewerAccess} />

      <section className="relative">
        <div className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_50%_0%,rgba(56,189,248,0.12),transparent_36%),linear-gradient(180deg,#05070d_0%,#070a12_48%,#04060b_100%)]" />

        <div className="container mx-auto px-4 py-20">
          <div className="mx-auto flex max-w-7xl flex-col gap-20 md:gap-24 xl:gap-28">
            <section className="grid items-center gap-12 lg:grid-cols-[0.9fr_1.1fr]">
              <div className="max-w-xl">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-sky-200/80">What you will build</p>
                <h2 className="mt-5 text-[2rem] font-semibold leading-[1.12] tracking-tight md:text-5xl xl:text-6xl">
                  Build tactical AI that feels intentional, not scripted.
                </h2>
                <p className="mt-6 text-base leading-8 text-slate-300">
                  This course is structured like a studio sequence: study the pattern, build the prototype, stress the decision model, and turn the final result
                  into portfolio evidence.
                </p>

                <div className="mt-9 grid gap-5">
                  {[
                    ['Production-minded', 'Focus on practical constraints, readable behavior, and implementation tradeoffs.'],
                    ['Hands-on from the start', 'Every concept points back to a build, a checkpoint, or a final artifact.'],
                    ['Portfolio-aware', 'Leave with work that is easier to explain in reviews and interviews.'],
                  ].map(([itemTitle, body], index) => (
                    <div key={itemTitle} className="grid grid-cols-[44px_1fr] gap-4">
                      <span className="grid size-11 place-items-center rounded-2xl border border-white/10 bg-white/[0.04] text-sky-200">
                        {index === 0 ? <BrainCircuit className="size-5" /> : index === 1 ? <Target className="size-5" /> : <FileText className="size-5" />}
                      </span>
                      <span>
                        <span className="block font-semibold text-white">{itemTitle}</span>
                        <span className="mt-1 block text-sm leading-6 text-slate-400">{body}</span>
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="relative">
                <div className="relative aspect-[1.35] overflow-hidden rounded-[2rem] border border-white/10 bg-slate-950 shadow-2xl shadow-black/40">
                  <Image
                    src={sectionVisuals.intro}
                    alt=""
                    fill
                    aria-hidden="true"
                    unoptimized={sectionVisuals.intro.endsWith('.svg')}
                    className="object-cover"
                    sizes="(min-width: 1024px) 56vw, 100vw"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#05070d]/70 via-transparent to-transparent" />
                </div>
                <div className="relative mx-6 -mt-12 rounded-[1.5rem] border border-white/10 bg-[#0b1020]/90 p-6 shadow-2xl shadow-black/40 backdrop-blur md:ml-auto md:max-w-md">
                  <p className="text-sm leading-7 text-slate-300">
                    &quot;{showcase?.portfolioProof ?? 'A course path designed to make the final project useful as public evidence of skill.'}&quot;
                  </p>
                  <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Portfolio proof</p>
                </div>
              </div>
            </section>

            <section className="overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.035] shadow-2xl shadow-black/20">
              <div className="grid gap-8 p-7 md:p-10 lg:grid-cols-[0.72fr_1fr] lg:items-center">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-violet-200/80">Why it works</p>
                  <h2 className="mt-4 text-3xl font-semibold tracking-tight md:text-[2.625rem] md:leading-tight xl:text-5xl">
                    Clear scope, visible progress, portfolio output.
                  </h2>
                  <p className="mt-5 max-w-xl text-sm leading-7 text-slate-400">
                    Know the workload, the depth, the access state, and the artifact students are expected to leave with before entering the classroom.
                  </p>
                </div>

                <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                  {[
                    { label: 'Studio hours', value: `${course.estimatedHours ?? 0}h`, icon: CalendarClock },
                    { label: 'Learning path', value: `${contentCount || journeyRows.length} modules`, icon: BookOpen },
                    { label: 'Level', value: level, icon: Layers3 },
                    { label: 'Final artifact', value: finalArtifactLabel, icon: ShieldCheck },
                  ].map((stat) => {
                    const Icon = stat.icon;

                    return (
                      <div key={stat.label} className="rounded-[1.25rem] border border-white/10 bg-[#0a0f1d] p-5">
                        <Icon className="mb-5 size-5 text-sky-200" />
                        <p className="text-2xl font-semibold tracking-tight">{stat.value}</p>
                        <p className="mt-2 text-xs uppercase tracking-[0.16em] text-slate-500">{stat.label}</p>
                      </div>
                    );
                  })}
                </div>
              </div>
            </section>

            <section className="grid gap-10">
              <div className="mx-auto max-w-3xl text-center">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-sky-200/80">Portfolio project</p>
                <h2 className="mt-5 text-[2rem] font-semibold leading-[1.15] tracking-tight md:text-5xl">Ship a portfolio piece that stands on its own.</h2>
                <p className="mx-auto mt-5 max-w-2xl text-base leading-8 text-slate-400">
                  The course work should feel like a production path, not disconnected lessons. Each milestone becomes a visible project checkpoint with a
                  concrete deliverable.
                </p>
                <Button asChild variant="outline" className="mt-8 border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                  <Link href="#curriculum">
                    View learning path
                    <ArrowRight />
                  </Link>
                </Button>
              </div>

              <CourseProjectCarousel courseTitle={title} projects={projectSlides} />
            </section>

            <section id="curriculum" aria-label="Course journey" className="overflow-hidden rounded-[2rem] border border-white/10 bg-[#080d18]">
              <div className="grid lg:grid-cols-[0.42fr_0.58fr]">
                <div className="border-b border-white/10 p-7 md:p-10 lg:border-b-0 lg:border-r">
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-violet-200/80">Course path</p>
                  <h2 className="mt-5 text-[2rem] font-semibold leading-[1.15] tracking-tight md:text-5xl">From concepts to production-ready systems.</h2>
                  <p className="mt-5 text-base leading-8 text-slate-400">
                    Follow a sequence of checkpoints that steadily turns the course idea into a usable, reviewable project artifact.
                  </p>

                  <div className="mt-8 flex flex-wrap gap-2 text-sm text-slate-300">
                    <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1">{contentCount || journeyRows.length} content items</span>
                    {previewMinutes ? (
                      <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1">{previewMinutes} preview minutes</span>
                    ) : null}
                    <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1">{projectSlides.length} project checkpoints</span>
                  </div>

                  <div className="mt-10 rounded-[1.5rem] border border-violet-300/15 bg-violet-300/10 p-5">
                    <p className="text-sm font-semibold text-violet-100">Journey outcome</p>
                    <p className="mt-3 text-sm leading-7 text-violet-100/75">{projectResult}</p>
                  </div>
                </div>

                <ol className="grid gap-0 divide-y divide-white/10">
                  {journeyRows.map((item) => {
                    const Icon = getContentIcon(item.type);

                    return (
                      <li key={`${item.label}-${item.title}`} className="relative grid gap-5 p-6 md:grid-cols-[92px_1fr] md:p-8">
                        <div className="flex items-start gap-4 md:block">
                          <span className="grid size-14 place-items-center rounded-full border border-violet-300/30 bg-violet-400/10 text-sm font-semibold text-violet-100">
                            {item.label}
                          </span>
                          <div className="mt-1 hidden h-full w-px bg-white/10 md:mx-auto md:block" />
                        </div>

                        <article className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_260px]">
                          <div>
                            <div className="flex items-center gap-3">
                              <Icon className="size-4 text-slate-500" />
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{item.projectTitle}</p>
                            </div>
                            <h3 className="mt-3 text-2xl font-semibold leading-tight text-white">{item.title}</h3>
                            <p className="mt-3 text-sm leading-7 text-slate-400">{item.body}</p>
                            {item.minutes ? <p className="mt-4 text-xs uppercase tracking-[0.16em] text-slate-500">{item.minutes} min</p> : null}
                          </div>

                          <div className="rounded-[1.25rem] border border-white/10 bg-black/20 p-4">
                            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200/80">Checkpoint output</p>
                            <p className="mt-3 text-sm leading-6 text-slate-300">{item.checkpoint}</p>
                          </div>
                        </article>
                      </li>
                    );
                  })}
                </ol>
              </div>
            </section>

            <section className="overflow-hidden rounded-[2rem] border border-indigo-300/20 bg-[linear-gradient(135deg,rgba(37,99,235,0.24),rgba(88,28,135,0.28)_48%,rgba(3,7,18,0.7))] shadow-2xl shadow-black/30">
              <div className="grid gap-8 p-7 md:p-10 lg:grid-cols-[1fr_auto] lg:items-center">
                <div className="max-w-2xl">
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-indigo-100/80">Enrollment</p>
                  <h2 className="mt-4 text-3xl font-semibold tracking-tight md:text-[2.625rem] md:leading-tight xl:text-5xl">Ready to build smarter games?</h2>
                  <p className="mt-5 text-sm leading-7 text-indigo-100/80">
                    {viewerAccess.state === 'has-access'
                      ? 'Your access is active. Continue into the classroom when you are ready.'
                      : viewerAccess.state === 'signed-out'
                        ? 'Sign in to verify access, enroll when available, and continue in the learning app.'
                        : viewerAccess.state === 'no-access'
                          ? course.isEnrollmentOpen
                            ? 'Enrollment is open for your account. Start the course when you are ready.'
                            : 'You are signed in, but enrollment is currently closed for this course.'
                          : 'Access verification is temporarily unavailable. Public course details are still visible.'}
                  </p>
                </div>
                <div className="flex flex-col gap-3 sm:flex-row lg:flex-col">
                  {viewerCta.kind === 'enroll' && course.slug ? (
                    <CourseSelfEnrollButton courseSlug={course.slug} />
                  ) : viewerCta.href ? (
                    <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
                      <Link href={viewerCta.href}>
                        {viewerCta.label}
                        <ArrowRight />
                      </Link>
                    </Button>
                  ) : (
                    <Button size="lg" disabled className="bg-white/20 text-white">
                      {viewerCta.label}
                    </Button>
                  )}
                  <Button asChild size="lg" variant="outline" className="border-white/20 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                    <Link href="/courses">Browse catalog</Link>
                  </Button>
                </div>
              </div>
              <div className="grid border-t border-white/10 md:grid-cols-4">
                {['Project checkpoints', 'Community support', 'Portfolio framing', 'Reusable course access'].map((item) => (
                  <div key={item} className="flex items-center gap-3 border-white/10 px-7 py-4 text-sm text-indigo-100/80 md:border-r md:last:border-r-0">
                    <Check className="size-4 text-sky-200" />
                    {item}
                  </div>
                ))}
              </div>
            </section>

            <section className="mx-auto grid w-full max-w-6xl gap-8 lg:grid-cols-2">
              <div className="rounded-[2rem] border border-white/10 bg-white/[0.035] p-8 md:p-10">
                <Target className="mb-7 text-sky-200" />
                <h2 className="text-2xl font-semibold tracking-tight md:text-3xl">Before you start</h2>
                <ul className="mt-8 flex flex-col gap-4">
                  {prerequisites.map((item) => (
                    <li key={item} className="flex gap-3 text-sm leading-6 text-slate-300">
                      <CheckCircle2 className="mt-1 size-4 shrink-0 text-sky-200" />
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>

              <div className="rounded-[2rem] border border-white/10 bg-white/[0.035] p-8 md:p-10">
                <Sparkles className="mb-7 text-violet-200" />
                <h2 className="text-2xl font-semibold tracking-tight md:text-3xl">How support works</h2>
                <div className="mt-8 grid gap-4">
                  {[
                    'Lessons and exercises stay available while students build.',
                    'Project checkpoints turn practice into visible progress.',
                    'Community and instructor feedback loops surround the work.',
                    'Portfolio framing helps the final artifact read professionally.',
                  ].map((item) => (
                    <p key={item} className="border-l border-white/15 pl-4 text-sm leading-6 text-slate-300">
                      {item}
                    </p>
                  ))}
                </div>
              </div>
            </section>

            {program ? (
              <section className="mx-auto w-full max-w-6xl overflow-hidden rounded-[2rem] border border-white/10 bg-[#080d18]">
                <div className="grid lg:grid-cols-[0.92fr_1.08fr]">
                  <div className="relative min-h-[360px]">
                    <Image src={sectionVisuals.program} alt={program.title} fill className="object-cover" sizes="(min-width: 1024px) 42vw, 100vw" />
                    <div className="absolute inset-0 bg-gradient-to-t from-[#05070d]/75 via-[#05070d]/10 to-transparent" />
                    <div className="absolute bottom-7 left-7 right-7">
                      <Badge variant="outline" className="border-white/15 bg-black/30 text-white backdrop-blur">
                        {program.shortTitle}
                      </Badge>
                      <p className="mt-4 max-w-md text-xl font-semibold leading-7">{program.portfolioResult}</p>
                    </div>
                  </div>
                  <div className="flex flex-col justify-center p-8 md:p-10">
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">Program package</p>
                    <h2 className="mt-4 text-3xl font-semibold tracking-tight md:text-[2.625rem] md:leading-tight xl:text-5xl">{program.title}</h2>
                    <p className="mt-5 text-base leading-8 text-slate-400">{program.longDescription}</p>
                    <div className="mt-8 grid gap-3 sm:grid-cols-2">
                      {program.tools.slice(0, 6).map((tool) => (
                        <span key={tool} className="flex items-center gap-3 rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-sm text-slate-300">
                          <Check className="size-4 text-sky-200" />
                          {tool}
                        </span>
                      ))}
                    </div>
                    <Button asChild variant="outline" className="mt-8 w-fit border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                      <Link href={`/programs/${program.slug}`}>
                        Explore package
                        <ArrowRight />
                      </Link>
                    </Button>
                  </div>
                </div>
              </section>
            ) : null}

            <section className="mx-auto grid w-full max-w-5xl gap-7 pb-10">
              <div className="text-center">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">FAQ</p>
                <h2 className="mt-4 text-3xl font-semibold tracking-tight md:text-5xl">Questions before joining</h2>
              </div>
              <div className="divide-y divide-white/10 overflow-hidden rounded-[1.5rem] border border-white/10 bg-white/[0.035]">
                {faq.map((item) => (
                  <details key={item.question} className="group">
                    <summary className="flex cursor-pointer list-none items-center justify-between gap-6 px-5 py-5 text-left font-semibold text-white marker:hidden md:px-7">
                      <span>{item.question}</span>
                      <span className="text-sm text-slate-500 transition group-open:rotate-45">+</span>
                    </summary>
                    <p className="px-5 pb-6 text-sm leading-7 text-slate-400 md:px-7">{item.answer}</p>
                  </details>
                ))}
              </div>
            </section>
          </div>
        </div>
      </section>
    </div>
  );
}
