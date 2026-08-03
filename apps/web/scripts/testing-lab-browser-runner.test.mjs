import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const packageJson = JSON.parse(
  await readFile(new URL('../package.json', import.meta.url), 'utf8'),
);

test('runs Testing Lab browser E2E through the isolated shell runner', () => {
  assert.equal(
    packageJson.scripts['test:browser:testing-lab'],
    'bash scripts/testing-lab-browser-e2e.sh',
  );
});

test('isolates the browser journey in a disposable PostgreSQL database', async () => {
  const runner = await readFile(
    new URL('./testing-lab-browser-e2e.sh', import.meta.url),
    'utf8',
  );

  assert.match(runner, /^#!\/usr\/bin\/env bash/m);
  assert.match(runner, /set -euo pipefail/);
  assert.match(runner, /postgres:16-alpine/);
  assert.match(runner, /POSTGRES_PORT="\$\{TESTING_LAB_E2E_POSTGRES_PORT:-\$\(\(43000 \+ RANDOM % 1000\)\)\}"/);
  assert.match(runner, /API_PORT="\$\{TESTING_LAB_E2E_API_PORT:-\$\(\(42000 \+ RANDOM % 1000\)\)\}"/);
  assert.match(runner, /WEB_PORT="\$\{TESTING_LAB_E2E_WEB_PORT:-\$\(\(44000 \+ RANDOM % 1000\)\)\}"/);
  assert.match(runner, /TESTING_LAB_E2E_DATABASE_MODE=disposable/);
  assert.match(runner, /POSTGRES_HOST=127\.0\.0\.1/);
  assert.match(runner, /POSTGRES_PORT=\$\{POSTGRES_PORT\}/);
  assert.match(runner, /POSTGRES_DB=\$\{POSTGRES_DATABASE\}/);
  assert.match(runner, /POSTGRES_USER=\$\{POSTGRES_USER\}/);
  assert.match(runner, /POSTGRES_PASSWORD=\$\{POSTGRES_PASSWORD\}/);
  assert.match(runner, /next dev --webpack/);
  assert.match(runner, /pnpm --filter @game-guild\/client build/);
  assert.match(runner, /TESTING_LAB_E2E_SKIP_CLIENT_BUILD/);
  assert.match(runner, /TESTING_LAB_E2E_LOCK_DIR/);
  assert.match(runner, /mkdir "\$\{LOCK_DIR\}"/);
  assert.match(runner, /rmdir "\$\{LOCK_DIR\}"/);
  assert.match(runner, /NEXT_BUILD_DIR="\$\{WEB_DIR\}\/\.next"/);
  assert.match(runner, /rm -rf -- "\$\{NEXT_BUILD_DIR\}"/);
  assert.match(runner, /taskkill\.exe \/\/PID/);
  assert.match(runner, /ps -W/);
  assert.match(runner, /win_pid/);
  assert.match(runner, /GAMEGUILD_DISABLE_WEBPACK_CACHE=1/);
  assert.match(runner, /assert_port_available "\$\{POSTGRES_PORT\}"/);
  assert.match(runner, /assert_port_available "\$\{API_PORT\}"/);
  assert.match(runner, /assert_port_available "\$\{WEB_PORT\}"/);
  assert.match(runner, /trap cleanup EXIT INT TERM/);
  assert.match(runner, /wait_for_http "http:\/\/127\.0\.0\.1:\$\{API_PORT\}\/ready"/);
  assert.match(runner, /wait_for_http .* "\$\{API_PID\}"/);
  assert.match(runner, /kill -0 "\$\{process_pid\}"/);
  assert.match(runner, /docker rm -f/);
  assert.doesNotMatch(runner, /docker compose down/);
});

test('allows cold SSR route compilation without aborting browser navigation', async () => {
  const journey = await readFile(
    new URL('./testing-lab-browser-e2e.mjs', import.meta.url),
    'utf8',
  );

  assert.match(journey, /page\.setDefaultNavigationTimeout\(120_000\)/);
  assert.match(
    journey,
    /const reviewerPage = await reviewerContext\.newPage\(\);\s+reviewerPage\.setDefaultNavigationTimeout\(120_000\)/,
  );
  assert.match(journey, /async function warmTestingLabSsr()/);
  assert.ok(
    journey.includes("await page.locator('h1').first().waitFor({ state: 'visible' });"),
  );
  assert.match(
    journey,
    /await page\.reload\(\{ waitUntil: 'domcontentloaded' \}\);\s+await waitForClientHydration\(page\);\s+await assertNoErrorSurface/,
  );
  assert.match(journey, /fetch\(\`\$\{webBaseUrl\}\/en-US\/testing-lab\`/);
  assert.ok(
    journey.indexOf('const fixture = await bootstrap();') <
      journey.indexOf('await warmTestingLabSsr();'),
  );
});

test('keeps the raw browser command explicitly unsafe for shared environments', () => {
  assert.equal(
    packageJson.scripts['test:browser:testing-lab:existing'],
    'node scripts/testing-lab-browser-e2e.mjs',
  );
});
test('waits for hydration before every client-side Testing Lab mutation', async () => {
  const journey = await readFile(
    new URL('./testing-lab-browser-e2e.mjs', import.meta.url),
    'utf8',
  );
  const scenarios = [
    ["'authenticated public Testing Lab event'", 'await waitForClientHydration(page);', "page.getByLabel('Existing project')"],
    ["'Testing Lab manager applications'", 'await waitForClientHydration(page);', "page.getByRole('button', { name: 'Review'"],
    ["'committee review applications'", 'await waitForClientHydration(reviewerPage);', "reviewerPage.getByRole('button', { name: 'Vote'"],
    ["'scheduled public Testing Lab event'", 'await waitForClientHydration(page);', "page.getByRole('button', { name: 'Reserve tester seat'"],
  ];

  for (const [visitMarker, hydrationMarker, actionMarker] of scenarios) {
    const visitIndex = journey.indexOf(visitMarker);
    const hydrationIndex = journey.indexOf(hydrationMarker, visitIndex);
    const actionIndex = journey.indexOf(actionMarker, visitIndex);
    assert.ok(visitIndex >= 0 && hydrationIndex > visitIndex && actionIndex > hydrationIndex);
  }
});
test('covers the complete Testing Lab operational browser matrix', async () => {
  const journey = await readFile(
    new URL('./testing-lab-browser-e2e.mjs', import.meta.url),
    'utf8',
  );

  for (const scenario of [
    'general settings persistence',
    'location lifecycle',
    'role and member access lifecycle',
    'attendance and required feedback',
    'filters search and pagination',
    'event cancellation and read-only history',
  ]) {
    assert.ok(
      journey.includes(`[testing-lab-browser-e2e] ${scenario}`),
      `missing browser scenario: ${scenario}`,
    );
  }

  for (const expectedInteraction of [
    "page.getByRole('button', { name: 'Save settings', exact: true })",
    "page.getByRole('button', { name: 'New location', exact: true })",
    "page.getByRole('button', { name: 'New role', exact: true })",
    "page.getByRole('button', { name: 'Manage access', exact: true })",
    "page.getByRole('button', { name: 'Start event', exact: true })",
    "page.getByRole('button', { name: 'Assign tested project', exact: true })",
    "page.getByRole('button', { name: 'Submit required feedback', exact: true })",
    "page.getByRole('button', { name: 'Cancel event', exact: true })",
  ]) {
    assert.ok(journey.includes(expectedInteraction), `missing interaction: ${expectedInteraction}`);
  }
});
test('waits for hydration after SSR reloads before editing locations and roles', async () => {
  const journey = await readFile(
    new URL('./testing-lab-browser-e2e.mjs', import.meta.url),
    'utf8',
  );

  for (const editMarker of [
    "locationRow.getByRole('button', { name: 'Edit', exact: true }).click()",
    "roleRow.getByRole('button', { name: 'Edit', exact: true }).click()",
  ]) {
    const editIndex = journey.indexOf(editMarker);
    const reloadIndex = journey.lastIndexOf("page.reload({ waitUntil: 'domcontentloaded' })", editIndex);
    const hydrationIndex = journey.indexOf('await waitForClientHydration(page);', reloadIndex);
    assert.ok(reloadIndex >= 0 && hydrationIndex > reloadIndex && hydrationIndex < editIndex);
  }
});
