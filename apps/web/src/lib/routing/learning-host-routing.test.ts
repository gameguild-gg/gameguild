import { describe, expect, it } from 'vitest';

import {
  getRequestHostname,
  resolveLearningHostRoute,
  type LearningHostRoutingConfig,
} from './learning-host-routing';

const config: LearningHostRoutingConfig = {
  defaultLocale: 'en-US',
  locales: ['en-US', 'pt-BR'],
  learningOrigin: 'https://learning.gameguild.gg',
  webOrigin: 'https://gameguild.gg',
};

describe('learning host routing', () => {
  it('prefers the forwarded host and strips its port', () => {
    const headers = new Headers({
      host: 'web:3000',
      'x-forwarded-host': 'learning.gameguild.gg:443',
    });

    expect(getRequestHostname(headers)).toBe('learning.gameguild.gg');
  });

  it('rewrites a clean learner URL to the internal localized route', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'learning.gameguild.gg',
      url: new URL('https://learning.gameguild.gg/courses/game-ai/content?module=2'),
    });

    expect(decision).toEqual({
      action: 'rewrite',
      url: 'https://learning.gameguild.gg/en-US/learn/courses/game-ai/content?module=2',
    });
  });

  it('preserves an explicit learner locale', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'learning.gameguild.gg',
      url: new URL('https://learning.gameguild.gg/pt-BR/courses/game-ai'),
    });

    expect(decision).toEqual({
      action: 'rewrite',
      url: 'https://learning.gameguild.gg/pt-BR/learn/courses/game-ai',
    });
  });

  it('redirects the legacy catalog to the commercial catalog', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'learning.gameguild.gg',
      url: new URL('https://learning.gameguild.gg/catalog?area=games'),
    });

    expect(decision).toEqual({
      action: 'redirect',
      status: 308,
      url: 'https://gameguild.gg/courses?area=games',
    });
  });

  it('redirects legacy assignments URLs to activities', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'learning.gameguild.gg',
      url: new URL('https://learning.gameguild.gg/courses/game-ai/assignments?state=open'),
    });

    expect(decision).toEqual({
      action: 'redirect',
      status: 308,
      url: 'https://learning.gameguild.gg/courses/game-ai/activities?state=open',
    });
  });

  it('uses central sign-in and returns to the exact learner URL', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'learning.gameguild.gg',
      url: new URL('https://learning.gameguild.gg/courses/game-ai/lessons/intro?mode=focus'),
      requiresAuthentication: true,
    });

    expect(decision).toEqual({
      action: 'redirect',
      status: 307,
      url: 'https://gameguild.gg/sign-in?redirectTo=https%3A%2F%2Flearning.gameguild.gg%2Fcourses%2Fgame-ai%2Flessons%2Fintro%3Fmode%3Dfocus',
    });
  });

  it('redirects direct internal learner routes on the main host', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'gameguild.gg',
      url: new URL('https://gameguild.gg/pt-BR/learn/courses/game-ai/grades?view=groups'),
    });

    expect(decision).toEqual({
      action: 'redirect',
      status: 308,
      url: 'https://learning.gameguild.gg/pt-BR/courses/game-ai/grades?view=groups',
    });
  });

  it('leaves ordinary website routes untouched', () => {
    const decision = resolveLearningHostRoute({
      config,
      hostname: 'gameguild.gg',
      url: new URL('https://gameguild.gg/courses/game-ai'),
    });

    expect(decision).toEqual({ action: 'next' });
  });
});
