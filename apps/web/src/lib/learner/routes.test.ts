import { describe, expect, it } from 'vitest';

import { createLearnerRoutes, getCentralSignInUrl } from './routes';

describe('learner routes', () => {
  it('keeps visible learner URLs free from the internal learn prefix', () => {
    const routes = createLearnerRoutes();

    expect(routes.home).toBe('/');
    expect(routes.courses).toBe('/courses');
    expect(routes.course('game-ai')).toBe('/courses/game-ai');
    expect(routes.content('game-ai')).toBe('/courses/game-ai/content');
    expect(routes.lesson('game-ai', 'intro')).toBe('/courses/game-ai/lessons/intro');
    expect(routes.activity('game-ai', 'quiz-1')).toBe('/courses/game-ai/activities/quiz-1');
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
