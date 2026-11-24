import { expectTypeOf, it } from 'vitest';
import route, { type CodingAuthoringRouteProps } from './coding-authoring-route';

it('UX04-C: preserves both route parameter contracts without rendering another editor', () => {
  expectTypeOf(route).toEqualTypeOf<(props: CodingAuthoringRouteProps) => Promise<never>>();
});
