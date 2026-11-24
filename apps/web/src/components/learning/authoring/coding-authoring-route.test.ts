import { access, readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import route from './coding-authoring-route';

const mocks = vi.hoisted(() => ({
  assessment: vi.fn<(course: string, slug: string) => Promise<{ courseId: string; contentId: string | null } | null>>(),
  content: vi.fn<(courseId: string, contentId: string) => Promise<{ id: string; slug?: string; type: string } | null>>(),
  redirect: vi.fn<(options: { href: string; locale: string }) => never>(),
  notFound: vi.fn<() => never>(),
}));

vi.mock('@/lib/learning', () => ({ getAssessment: mocks.assessment, getContentItem: mocks.content }));
vi.mock('@/i18n/navigation', () => ({ redirect: mocks.redirect }));
vi.mock('next/navigation', () => ({ notFound: mocks.notFound }));

const request = () => ({ params: Promise.resolve({ locale: 'pt-BR', course: 'course name', assessmentSlug: 'code-assessment' }) });

beforeEach(() => {
  vi.clearAllMocks();
  mocks.assessment.mockResolvedValue({ courseId: 'authorized-course', contentId: 'content-id' });
  mocks.content.mockResolvedValue({ id: 'content-id', slug: 'my exercise', type: 'Code' });
  mocks.redirect.mockImplementation((options) => {
    throw Object.assign(new Error('redirect'), options);
  });
  mocks.notFound.mockImplementation(() => {
    throw new Error('not-found');
  });
});

describe('UX04-C: one coding authoring entry', () => {
  it('resolves the authorized content and redirects with locale and encoded route parameters', async () => {
    await expect(route(request())).rejects.toMatchObject({
      message: 'redirect',
      locale: 'pt-BR',
      href: '/workspace/learning/courses/course%20name/content/my%20exercise',
    });
    expect(mocks.assessment).toHaveBeenCalledExactlyOnceWith('course name', 'code-assessment');
    expect(mocks.content).toHaveBeenCalledExactlyOnceWith('authorized-course', 'content-id');
    expect(mocks.notFound).not.toHaveBeenCalled();
  });

  it('uses the current content id when a slug is absent', async () => {
    mocks.content.mockResolvedValueOnce({ id: 'content-id', type: 'Code' });
    await expect(route(request())).rejects.toMatchObject({ href: '/workspace/learning/courses/course%20name/content/content-id' });
  });

  it.each(['missing-assessment', 'unlinked-assessment', 'missing-content', 'wrong-format'] as const)(
    'fails closed for %s instead of mounting a second editor',
    async (failure) => {
      if (failure === 'missing-assessment') mocks.assessment.mockResolvedValueOnce(null);
      if (failure === 'unlinked-assessment') mocks.assessment.mockResolvedValueOnce({ courseId: 'authorized-course', contentId: null });
      if (failure === 'missing-content') mocks.content.mockResolvedValueOnce(null);
      if (failure === 'wrong-format') mocks.content.mockResolvedValueOnce({ id: 'content-id', type: 'Lesson' });
      await expect(route(request())).rejects.toMatchObject({ message: 'not-found' });
      expect(mocks.redirect).not.toHaveBeenCalled();
    },
  );

  it('does not disguise an authorization/query failure as an empty authoring workspace', async () => {
    mocks.assessment.mockRejectedValueOnce(new Error('access-denied'));
    await expect(route(request())).rejects.toMatchObject({ message: 'access-denied' });
    expect(mocks.content).not.toHaveBeenCalled();
    expect(mocks.redirect).not.toHaveBeenCalled();
  });

  it.each(['console', 'workspace'])('routes %s definition links through the single owned server boundary', async (area) => {
    const file = resolve(
      dirname(fileURLToPath(import.meta.url)),
      `../../../app/[locale]/(dashboards)/${area}/learning/courses/[course]/assessments/[assessmentSlug]/coding-definition/page.tsx`,
    );
    const source = await readFile(file, 'utf8');
    expect(source).toMatch(/export \{ default \} from ["']@\/components\/learning\/authoring\/coding-authoring-route["']/);
    expect(source).not.toMatch(/CodingDefinitionEditor|getCodingAssignmentFull|putCodingAssignment/);
  });

  it('retires the owned direct-upsert forms and private host preview after their consumers migrate', async () => {
    const directory = dirname(fileURLToPath(import.meta.url));
    const host = await readFile(resolve(directory, 'lesson-authoring-workspace.tsx'), 'utf8');
    expect(host).toMatch(/CodingAuthoringSurface/);
    expect(host).not.toMatch(/CodingDefinitionEditor|CodingAssignmentPreview|putCodingAssignmentAction|StandardTestEditor|FunctionalTestEditor/);
    for (const file of [
      'coding-assignment-preview.tsx',
      '../console/courses/[course]/assessments/[assessmentId]/coding-definition/coding-definition-editor.tsx',
      '../console/courses/[course]/assessments/[assessmentId]/coding-definition/standard-test-editor.tsx',
      '../console/courses/[course]/assessments/[assessmentId]/coding-definition/functional-test-editor.tsx',
    ])
      await expect(access(resolve(directory, file))).rejects.toMatchObject({ code: 'ENOENT' });
  });
});
