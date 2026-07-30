#!/usr/bin/env node

import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const apiBaseUrl = (process.env.API_BASE_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5297').replace(/\/$/, '');
const webBaseUrl = (process.env.TESTING_LAB_E2E_BASE_URL ?? process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3011').replace(/\/$/, '');
const adminEmail = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? 'admin@game-guild.com';
const adminPassword = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? 'Admin123!';
const headless = !['0', 'false', 'no'].includes((process.env.TESTING_LAB_E2E_HEADLESS ?? 'true').toLowerCase());
const artifactsDirectory = path.resolve(process.env.TESTING_LAB_E2E_ARTIFACTS ?? path.join(process.cwd(), '.tmp', 'testing-lab-browser-e2e'));

function unique() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

async function apiRequest(pathname, init = {}, accessToken, tenantId) {
  const response = await fetch(`${apiBaseUrl}${pathname}`, {
    ...init,
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
      ...(tenantId ? { 'x-tenant-id': tenantId } : {}),
      ...init.headers,
    },
  });
  const body = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(`${init.method ?? 'GET'} ${pathname} failed with ${response.status}: ${JSON.stringify(body)}`);
  }
  return body;
}

async function bootstrap() {
  const auth = await apiRequest('/v1/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ email: adminEmail, password: adminPassword }),
  });
  if (!auth.accessToken || !auth.tenantId) {
    throw new Error('The system administrator session did not expose accessToken and tenantId.');
  }

  const tag = unique();
  const reviewerEmail = `testing-lab-browser-reviewer-${tag}@example.test`;
  const reviewerPassword = 'Str0ng!Passw0rd123!';
  const reviewerAuth = await apiRequest('/v1/auth/sign-up', {
    method: 'POST',
    body: JSON.stringify({
      username: `testing_lab_browser_reviewer_${tag.replace(/[^a-z0-9]/gi, '_')}`,
      email: reviewerEmail,
      password: reviewerPassword,
      tenantId: auth.tenantId,
    }),
  });
  const reviewerId = reviewerAuth.userId ?? reviewerAuth.user?.id;
  if (!reviewerId) throw new Error('The committee reviewer sign-up did not expose a user id.');
  const reviewerMemberships = await apiRequest(`/v1/users/${reviewerId}/memberships?includeInactive=true`, {}, auth.accessToken, auth.tenantId);
  const hasMembership = reviewerMemberships.memberships?.some(
    (membership) => String(membership.tenantId).toLowerCase() === String(auth.tenantId).toLowerCase() && membership.isActive,
  );
  if (!hasMembership) {
    await apiRequest(
      `/v1/users/${reviewerId}/memberships`,
      {
        method: 'POST',
        body: JSON.stringify({
          tenantId: auth.tenantId,
          role: 'Member',
          requiresAcceptance: false,
          invitedByEmail: adminEmail,
          inviteeEmail: reviewerEmail,
        }),
      },
      auth.accessToken,
      auth.tenantId,
    );
  }
  const project = await apiRequest(
    '/v1/projects',
    {
      method: 'POST',
      body: JSON.stringify({
        title: `Browser Lab Project ${tag}`,
        description: 'A playable project created for the Testing Lab browser journey.',
        shortDescription: 'Real browser E2E fixture',
        type: 0,
        visibility: 4,
        status: 2,
        tags: ['testing-lab', 'browser-e2e'],
      }),
    },
    auth.accessToken,
    auth.tenantId,
  );

  const now = Date.now();
  const event = await apiRequest(
    '/v1/testing/events',
    {
      method: 'POST',
      body: JSON.stringify({
        name: `Browser Testing Showcase ${tag}`,
        description: 'A real campus testing event used to verify public, applicant, tester, and manager browser journeys.',
        mode: 1,
        approvalMode: 1,
        applicationsOpenAt: new Date(now - 60_000).toISOString(),
        applicationsCloseAt: new Date(now + 60 * 60_000).toISOString(),
        startsAt: new Date(now + 2 * 60 * 60_000).toISOString(),
        endsAt: new Date(now + 5 * 60 * 60_000).toISOString(),
        requiresFeedback: true,
      }),
    },
    auth.accessToken,
    auth.tenantId,
  );

  const slot = await apiRequest(
    `/v1/testing/events/${event.id}/slots`,
    {
      method: 'POST',
      body: JSON.stringify({
        mode: 1,
        startsAt: new Date(now + 2 * 60 * 60_000).toISOString(),
        endsAt: new Date(now + 4 * 60 * 60_000).toISOString(),
        maxTesters: 4,
        maxProjects: 2,
        campusName: 'Browser E2E Campus',
        roomName: 'Interaction Lab',
        meetingUrl: null,
        locationId: null,
      }),
    },
    auth.accessToken,
    auth.tenantId,
  );

  await apiRequest(
    `/v1/testing/events/${event.id}/committee`,
    {
      method: 'POST',
      body: JSON.stringify({ userId: reviewerId, isChair: true }),
    },
    auth.accessToken,
    auth.tenantId,
  );

  await apiRequest(
    `/v1/testing/events/${event.id}:open-applications`,
    {
      method: 'POST',
    },
    auth.accessToken,
    auth.tenantId,
  );

  return {
    accessToken: auth.accessToken,
    event,
    project,
    reviewerEmail,
    reviewerPassword,
    slot,
    tag,
    tenantId: auth.tenantId,
  };
}

async function assertNoErrorSurface(page, label) {
  await page.waitForLoadState('domcontentloaded');
  await page.locator('body').waitFor({ state: 'visible' });
  const body = await page.locator('body').innerText();
  if (/This page could not be found|Unhandled Runtime Error|Build Error|Application error|Internal server error/i.test(body)) {
    throw new Error(`${label} rendered an error surface at ${page.url()}:\n${body.slice(0, 1600)}`);
  }
}

async function assertNoViewportOverflow(page, label) {
  const dimensions = await page.evaluate(() => ({
    clientWidth: document.documentElement.clientWidth,
    scrollWidth: document.documentElement.scrollWidth,
  }));
  if (dimensions.scrollWidth > dimensions.clientWidth + 2) {
    throw new Error(`${label} overflows the viewport: ${JSON.stringify(dimensions)}`);
  }
}

async function visit(page, pathname, label) {
  await page.goto(`${webBaseUrl}${pathname}`, { waitUntil: 'domcontentloaded' });
  await assertNoErrorSurface(page, label);
  await page.waitForFunction(() => document.readyState !== 'loading');
  await page.waitForTimeout(350);
}

async function signIn(page, email = adminEmail, password = adminPassword) {
  await visit(page, '/sign-in', 'sign in');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.waitForURL(/\/dashboard/, { timeout: 60_000 });
}

async function waitForText(page, text) {
  await page.getByText(text, { exact: false }).filter({ visible: true }).first().waitFor();
}

async function run() {
  await mkdir(artifactsDirectory, { recursive: true });
  const fixture = await bootstrap();
  const browser = await chromium.launch({ headless });
  const browserErrors = [];
  const failedResponses = [];
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  let reviewerContext;

  page.setDefaultTimeout(60_000);
  page.on('pageerror', (error) => browserErrors.push(error.message));
  page.on('console', (message) => {
    if (message.type() === 'error' && !/favicon|cloudflareinsights|Failed to load resource: the server responded with a status of 404/i.test(message.text())) {
      browserErrors.push(message.text());
    }
  });
  page.on('response', (response) => {
    const url = new URL(response.url());
    if (url.searchParams.has('_rsc')) return;
    if (url.origin === webBaseUrl && response.status() >= 400) {
      failedResponses.push(`${response.status()} ${url.pathname}${url.search}`);
    }
  });

  try {
    console.log('[testing-lab-browser-e2e] anonymous directory and event detail');
    await visit(page, '/testing-lab', 'public Testing Lab landing');
    await waitForText(page, 'Game Testing Lab');
    await page.getByRole('link', { name: 'Browse Events', exact: true }).click();
    await page.waitForURL(/\/testing-lab\/events/);
    await waitForText(page, fixture.event.name);
    await assertNoViewportOverflow(page, 'public Testing Lab directory');
    await page.screenshot({ path: path.join(artifactsDirectory, 'public-directory-desktop.png'), fullPage: true });

    await visit(page, `/testing-lab/events/${fixture.event.id}`, 'public Testing Lab event');
    await waitForText(page, 'Schedules and tester capacity');
    await waitForText(page, 'Browser E2E Campus');
    await page.getByRole('link', { name: 'Sign in to apply', exact: true }).waitFor();
    await assertNoViewportOverflow(page, 'public Testing Lab event');

    console.log('[testing-lab-browser-e2e] authenticated project candidacy');
    await signIn(page);
    await visit(page, `/testing-lab/events/${fixture.event.id}`, 'authenticated public Testing Lab event');
    await page.getByLabel('Existing project').selectOption(fixture.project.id);
    await page.getByLabel('Preferred availability').fill('The published campus schedule works for this project.');
    await page.getByRole('button', { name: 'Submit project application', exact: true }).click();
    await waitForText(page, 'Project application submitted.');

    console.log('[testing-lab-browser-e2e] manager review');
    await visit(page, `/dashboard/testing-lab/events/${fixture.event.id}`, 'Testing Lab manager event');
    await waitForText(page, 'Project applications');
    await page.getByRole('button', { name: 'Review', exact: true }).click();
    await waitForText(page, 'Under Review');

    console.log('[testing-lab-browser-e2e] committee reviewer vote');
    reviewerContext = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const reviewerPage = await reviewerContext.newPage();
    reviewerPage.setDefaultTimeout(60_000);
    await signIn(reviewerPage, fixture.reviewerEmail, fixture.reviewerPassword);
    await visit(reviewerPage, `/dashboard/testing-lab/events/${fixture.event.id}`, 'committee review event');
    await reviewerPage.getByRole('button', { name: 'Vote', exact: true }).click();
    await reviewerPage.getByText('Choose decision', { exact: true }).click();
    await reviewerPage.getByRole('option', { name: 'Approve', exact: true }).click();
    await reviewerPage.getByLabel('Comments').fill('Approved by the committee reviewer through the browser.');
    await reviewerPage.getByRole('button', { name: 'Record vote', exact: true }).click();
    await waitForText(reviewerPage, 'Committee vote recorded.');
    await reviewerPage.getByRole('dialog').waitFor({ state: 'hidden' });
    await reviewerContext.close();
    reviewerContext = undefined;

    console.log('[testing-lab-browser-e2e] manager approval and capacity reservation');
    await page.reload({ waitUntil: 'domcontentloaded' });
    await assertNoErrorSurface(page, 'Testing Lab manager event after committee vote');
    await page.getByRole('button', { name: 'Approve', exact: true }).click();
    await page
      .getByRole('combobox')
      .filter({ has: page.getByText('Choose a slot') })
      .click()
      .catch(async () => {
        await page.getByText('Choose a slot', { exact: true }).click();
      });
    await page.getByRole('option').first().click();
    await page.getByLabel('Decision notes').fill('Approved through the real browser management flow.');
    await page.getByRole('button', { name: 'Approve project', exact: true }).click();
    await waitForText(page, 'Project application approved.');
    await page.getByRole('dialog').waitFor({ state: 'hidden' });
    await waitForText(page, 'Approved');

    await page.getByRole('button', { name: 'Close applications', exact: true }).click();
    await waitForText(page, 'Applications closed');
    await page.getByRole('button', { name: 'Schedule event', exact: true }).click();
    await waitForText(page, 'Scheduled');
    console.log('[testing-lab-browser-e2e] tester seat through the public experience');
    await visit(page, `/testing-lab/events/${fixture.event.id}`, 'scheduled public Testing Lab event');
    await page.getByRole('button', { name: 'Reserve tester seat', exact: true }).click();
    await waitForText(page, 'Testing slot registration submitted.');
    await waitForText(page, 'Registered');
    await page.screenshot({ path: path.join(artifactsDirectory, 'event-participation-desktop.png'), fullPage: true });

    console.log('[testing-lab-browser-e2e] manager operations surfaces');
    for (const [pathname, title] of [
      ['/dashboard/testing-lab', 'Testing Lab'],
      ['/dashboard/testing-lab/events', 'Testing events'],
      [`/dashboard/testing-lab/events/${fixture.event.id}`, fixture.event.name],
      ['/dashboard/testing-lab/requests', 'Requests'],
      ['/dashboard/testing-lab/sessions', 'Sessions'],
      ['/dashboard/testing-lab/people', 'People'],
      ['/dashboard/testing-lab/feedback', 'Feedback'],
      ['/dashboard/testing-lab/reports', 'Reports'],
      ['/dashboard/testing-lab/locations', 'Locations'],
      ['/dashboard/testing-lab/settings', 'Settings'],
      ['/dashboard/testing-lab/access', 'Access'],
    ]) {
      await visit(page, pathname, title);
      await waitForText(page, title);
      await assertNoViewportOverflow(page, title);
    }

    console.log('[testing-lab-browser-e2e] mobile public and manager surfaces');
    await page.setViewportSize({ width: 390, height: 844 });
    await visit(page, '/testing-lab/events', 'mobile public Testing Lab directory');
    await waitForText(page, fixture.event.name);
    await assertNoViewportOverflow(page, 'mobile public Testing Lab directory');
    await page.screenshot({ path: path.join(artifactsDirectory, 'public-directory-mobile.png'), fullPage: true });
    await visit(page, `/testing-lab/events/${fixture.event.id}`, 'mobile public Testing Lab event');
    await waitForText(page, 'Schedules and tester capacity');
    await assertNoViewportOverflow(page, 'mobile public Testing Lab event');
    await visit(page, `/dashboard/testing-lab/events/${fixture.event.id}`, 'mobile Testing Lab manager event');
    await waitForText(page, fixture.event.name);
    await assertNoViewportOverflow(page, 'mobile Testing Lab manager event');
    await page.screenshot({ path: path.join(artifactsDirectory, 'manager-event-mobile.png'), fullPage: true });

    const meaningfulFailures = [...new Set(failedResponses)].filter((value) => !/favicon|manifest\.webmanifest/.test(value));
    if (meaningfulFailures.length > 0) {
      throw new Error(`HTTP failures detected:\n${meaningfulFailures.join('\n')}`);
    }
    if (browserErrors.length > 0) {
      throw new Error(`Browser errors detected:\n${[...new Set(browserErrors)].join('\n')}`);
    }

    console.log(`Testing Lab browser E2E passed for ${fixture.tag}. Artifacts: ${artifactsDirectory}`);
  } catch (error) {
    const pageText = await page
      .locator('body')
      .innerText()
      .catch(() => 'Unable to read page body.');
    console.error(`[testing-lab-browser-e2e] failed at ${page.url()}`);
    console.error(`[testing-lab-browser-e2e] HTTP failures: ${[...new Set(failedResponses)].join(', ') || 'none'}`);
    console.error(`[testing-lab-browser-e2e] browser errors: ${[...new Set(browserErrors)].join(' | ') || 'none'}`);
    console.error(`[testing-lab-browser-e2e] page excerpt:\n${pageText.slice(0, 2600)}`);
    throw error;
  } finally {
    if (reviewerContext) await reviewerContext.close();
    await apiRequest(
      `/v1/testing/events/${fixture.event.id}:cancel`,
      {
        method: 'POST',
        body: JSON.stringify({ reason: 'Browser E2E fixture completed.' }),
      },
      fixture.accessToken,
      fixture.tenantId,
    ).catch(() => undefined);
    await browser.close();
  }
}

run().catch((error) => {
  console.error(error instanceof Error ? (error.stack ?? error.message) : error);
  process.exit(1);
});
