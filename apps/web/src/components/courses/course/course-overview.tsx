'use client';

import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';
import { Badge } from '@/components/ui/badge';
import { Program, ProgramContent, ProgramContentType } from '@/lib/api/generated';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { getCourseShowcase, getProgramForCourse, listCourseContentPreview } from '@/lib/courses/public-programs';
import { BookOpen, CheckCircle2, Code, FileText, HelpCircle, MessageSquare, Rocket, Target } from 'lucide-react';
import { useState } from 'react';

interface CourseOverviewProps {
  readonly course: Program;
  readonly levelConfig?: {
    name: string;
    color: string;
    bgColor: string;
  };
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

function getContentIcon(type: number | null | undefined) {
  switch (type) {
    case ProgramContentType.Assignment:
    case ProgramContentType.Code:
    case ProgramContentType.Challenge:
      return <Code />;
    case ProgramContentType.Discussion:
    case ProgramContentType.Questionnaire:
      return <MessageSquare />;
    case ProgramContentType.Page:
    case ProgramContentType.Reflection:
    case ProgramContentType.Survey:
    case ProgramContentType.Lesson:
    default:
      return <FileText />;
  }
}

function getContentTypeName(type: number | null | undefined) {
  switch (type) {
    case ProgramContentType.Page:
      return 'Page';
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

function getContentValue(content: ProgramContent | undefined, index: number): string {
  if (content?.id != null) {
    return String(content.id);
  }

  if (content?.title) {
    return String(content.title);
  }

  return `content-${index}`;
}

export function CourseOverview({ course, levelConfig }: CourseOverviewProps) {
  const courseSlug = typeof course.slug === 'string' ? course.slug : null;
  const showcase = getCourseShowcase(courseSlug);
  const program = getProgramForCourse(courseSlug);
  const courseDifficulty = course.difficulty as string | number | null | undefined;
  const courseCategory = course.category as string | number | null | undefined;
  const config = levelConfig || getCourseLevelConfig(courseDifficulty);
  const categoryName = getCourseCategoryName(courseCategory);
  const publishedSkills = normalizeList(course.skillsProvided);
  const requiredSkills = normalizeList(course.skillsRequired);
  const topLevelContent = listCourseContentPreview(course.programContents, 10);
  const [selectedContent, setSelectedContent] = useState<ProgramContent | null>(topLevelContent[0] ?? null);

  const outcomes = showcase?.outcomes.length ? showcase.outcomes : publishedSkills.length ? publishedSkills : [
    `Understand the core concepts behind this ${categoryName.toLowerCase()} course.`,
    'Build practical work that can be reviewed and improved.',
    'Translate lessons into a visible portfolio artifact.',
  ];

  const prerequisites = showcase?.prerequisites.length ? showcase.prerequisites : requiredSkills.length ? requiredSkills : [
    config.name === 'Beginner' ? 'No advanced background required.' : `${config.name} level comfort with the course discipline is recommended.`,
  ];

  return (
    <div className="flex flex-col gap-12">
      <section className="grid gap-5 md:grid-cols-3">
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
          <Target className="mb-5 text-sky-200" />
          <h2 className="text-xl font-semibold">Course outcome</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">
            {showcase?.projectResult || `A practical ${categoryName.toLowerCase()} project that demonstrates what students learned.`}
          </p>
        </div>
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
          <Rocket className="mb-5 text-violet-200" />
          <h2 className="text-xl font-semibold">Portfolio proof</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">
            {showcase?.portfolioProof || 'Students leave with a stronger artifact and clearer explanation of their design or engineering decisions.'}
          </p>
        </div>
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
          <BookOpen className="mb-5 text-emerald-200" />
          <h2 className="text-xl font-semibold">Learning model</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">
            {showcase?.instructorModel || 'Structured lessons, practical exercises, and classroom handoff through the GameGuild learning app.'}
          </p>
        </div>
      </section>

      <section className="grid gap-10 lg:grid-cols-[0.75fr_1.25fr]">
        <div>
          <h2 className="text-4xl font-semibold tracking-tight">What you will learn</h2>
          <p className="mt-4 text-base leading-7 text-slate-400">
            The public landing page now exposes the promise of the course: what students practice, what they produce, and which path it belongs to.
          </p>
          <div className="mt-6 flex flex-wrap gap-2">
            <Badge variant="outline" className="border-white/15 text-slate-200">{categoryName}</Badge>
            <Badge variant="outline" className="border-white/15 text-slate-200">{config.name}</Badge>
            {program ? <Badge variant="outline" className="border-white/15 text-slate-200">{program.shortTitle} package</Badge> : null}
          </div>
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          {outcomes.map((outcome) => (
            <div key={outcome} className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-5">
              <CheckCircle2 className="mb-4 text-emerald-200" />
              <p className="text-sm leading-6 text-slate-300">{outcome}</p>
            </div>
          ))}
        </div>
      </section>

      <section id="curriculum" className="grid gap-10 lg:grid-cols-[0.75fr_1.25fr]">
        <div>
          <h2 className="text-4xl font-semibold tracking-tight">Curriculum preview</h2>
          <p className="mt-4 text-base leading-7 text-slate-400">
            The outline is imported from the live course content when available. Students can inspect the structure before entering the classroom.
          </p>
          <div className="mt-6 rounded-[2rem] border border-white/10 bg-white/[0.045] p-5">
            <p className="text-3xl font-semibold">{course.programContents?.length ?? 0}</p>
            <p className="mt-1 text-sm text-slate-500">Published content items</p>
          </div>
        </div>

        {topLevelContent.length > 0 ? (
          <div className="grid gap-6 xl:grid-cols-[0.85fr_1.15fr]">
            <Accordion type="single" collapsible defaultValue={getContentValue(topLevelContent[0], 0)} className="rounded-[2rem] border border-white/10 bg-white/[0.045] px-5">
              {topLevelContent.map((content, index) => (
                <AccordionItem key={getContentValue(content, index)} value={getContentValue(content, index)} className="border-white/10">
                  <AccordionTrigger onClick={() => setSelectedContent(content)} className="gap-4 text-left text-white hover:no-underline">
                    <span className="flex items-center gap-3">
                      <span className="rounded-2xl border border-white/10 bg-black/20 p-2 text-slate-300">
                        {getContentIcon(content.type)}
                      </span>
                      <span>
                        <span className="block font-semibold">{content.title}</span>
                        <span className="mt-1 block text-xs font-normal text-slate-500">
                          {getContentTypeName(content.type)}
                          {content.estimatedMinutes ? ` - ${content.estimatedMinutes} min` : ''}
                        </span>
                      </span>
                    </span>
                  </AccordionTrigger>
                  <AccordionContent className="text-sm leading-6 text-slate-400">
                    {content.description || 'This curriculum item is available inside the course content.'}
                  </AccordionContent>
                </AccordionItem>
              ))}
            </Accordion>

            <div className="min-w-0 rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
              {selectedContent ? (
                <div className="flex min-w-0 flex-col gap-5">
                  <div>
                    <Badge variant="outline" className="border-white/15 text-slate-200">
                      {getContentTypeName(selectedContent.type)}
                    </Badge>
                    <h3 className="mt-4 text-2xl font-semibold tracking-tight">{selectedContent.title}</h3>
                    {selectedContent.description ? <p className="mt-3 text-sm leading-6 text-slate-400">{selectedContent.description}</p> : null}
                  </div>
                  {selectedContent.body ? (
                    <div className="prose prose-slate prose-invert max-h-[420px] max-w-none overflow-auto rounded-2xl border border-white/10 bg-black/20 p-5 text-sm">
                      <MarkdownRenderer content={selectedContent.body} />
                    </div>
                  ) : (
                    <p className="rounded-2xl border border-white/10 bg-black/20 p-5 text-sm leading-6 text-slate-400">
                      Select a curriculum item with published body content to preview it here.
                    </p>
                  )}
                </div>
              ) : null}
            </div>
          </div>
        ) : (
          <div className="rounded-[2rem] border border-dashed border-white/15 bg-white/[0.035] p-8 text-slate-300">
            Curriculum content has not been published for this course yet.
          </div>
        )}
      </section>

      <section className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-7">
          <h2 className="text-3xl font-semibold tracking-tight">Prerequisites</h2>
          <ul className="mt-6 flex flex-col gap-3">
            {prerequisites.map((item) => (
              <li key={item} className="flex gap-3 text-sm leading-6 text-slate-300">
                <CheckCircle2 className="mt-1 shrink-0 text-sky-200" />
                <span>{item}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-7">
          <h2 className="text-3xl font-semibold tracking-tight">Common questions</h2>
          <div className="mt-6 flex flex-col gap-4">
            {(showcase?.faq ?? [
              {
                question: 'Where does learning happen?',
                answer: 'Public pages explain the course, and enrolled learners continue through the dedicated GameGuild learning app.',
              },
              {
                question: 'Can I take this course outside a package?',
                answer: 'Yes. Packages clarify the path, but every course still has its own landing page and course entry point.',
              },
            ]).map((item) => (
              <div key={item.question} className="rounded-2xl border border-white/10 bg-black/20 p-4">
                <div className="flex gap-3">
                  <HelpCircle className="mt-0.5 shrink-0 text-violet-200" />
                  <div>
                    <p className="font-semibold text-white">{item.question}</p>
                    <p className="mt-2 text-sm leading-6 text-slate-400">{item.answer}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
