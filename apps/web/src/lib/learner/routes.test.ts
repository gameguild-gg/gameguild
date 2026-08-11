import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  getPathname: ({ href, locale }: { href: string; locale: string }) =>
    locale === 'en-US' ? href : `/${locale}${href}`,
}));

const { createLearnerRoutes, getCentralSignInUrl } = await import('./routes');

describe('learner routes', () => {
  it('uses App Router paths and delegates locale prefixes to next-intl', () => {
    const routes = createLearnerRoutes();

    expect(routes.home).toBe('/learn');
    expect(routes.courses).toBe('/learn/courses');
    expect(routes.course('game-ai')).toBe('/learn/courses/game-ai');
    expect(routes.content('game-ai')).toBe('/learn/courses/game-ai/content');
    expect(routes.lesson('game-ai', 'intro')).toBe('/learn/courses/game-ai/lessons/intro');
    expect(routes.activities('game-ai')).toBe('/learn/courses/game-ai/activities');
    expect(routes.activity('game-ai', 'quiz-1')).toBe('/learn/courses/game-ai/activities/quiz-1');
  });

  it('delegates non-default locale prefixes to next-intl', () => {
    const routes = createLearnerRoutes('pt-BR');

    expect(routes.course('game-ai')).toBe('/pt-BR/learn/courses/game-ai');
    expect(routes.activities('game-ai')).toBe('/pt-BR/learn/courses/game-ai/activities');
  });

  it('falls back to the default locale for an invalid route segment', () => {
    const routes = createLearnerRoutes('invalid-locale');

    expect(routes.course('game-ai')).toBe('/learn/courses/game-ai');
  });

  it('creates an allowlisted central sign-in return URL', () => {
    expect(
      getCentralSignInUrl({
        learningOrigin: 'https://learning.gameguild.gg',
        pathname: '/courses/game-ai/lessons/intro?mode=focus',
        webOrigin: 'https://gameguild.gg',
      }),
    ).toBe(
      'https://gameguild.gg/sign-in?redirectTo=https%3A%2F%2Flearning.gameguild.gg%2Fcourses%2Fgame-ai%2Flessons%2Fintro%3Fmode%3Dfocus',
    );
  });
});
