#!/usr/bin/env node

import { chromium } from 'playwright';

const baseUrl = (process.env.PUBLIC_E2E_BASE_URL ?? process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3011').replace(/\/$/, '');

const publicRoutes = [
  ['Home', '/'],
  ['Courses', '/courses'],
  ['Programs', '/programs'],
  ['Testing Lab', '/testing-lab'],
  ['Projects', '/projects'],
  ['Community', '/community'],
  ['Jobs', '/jobs'],
  ['About', '/about'],
  ['Sign in', '/sign-in'],
  ['Sign up', '/sign-up'],
];

function routeUrl(route) {
  return `${baseUrl}${route}`;
}

async function assertNoErrorOverlay(page, label) {
  await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => undefined);
  await page.waitForFunction(() => document.body.innerText.trim().length > 0, undefined, { timeout: 20_000 });

  const body = await page.locator('body').innerText({ timeout: 10_000 });

  if (!body.trim()) {
    throw new Error(`${label}: rendered an empty body`);
  }

  if (/404|page not found|Unhandled Runtime Error|Build Error|Application error/i.test(body)) {
    throw new Error(`${label}: rendered an error surface`);
  }
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
  });
  const page = await context.newPage();
  const consoleErrors = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', (error) => {
    consoleErrors.push(error.message);
  });

  try {
    for (const [label, route] of publicRoutes) {
      await page.goto(routeUrl(route), { waitUntil: 'domcontentloaded' });
      await assertNoErrorOverlay(page, label);
    }

    await page.goto(routeUrl('/'), { waitUntil: 'domcontentloaded' });
    const desktopNav = page.getByRole('navigation', { name: 'Main navigation' });
    await desktopNav.getByRole('link', { name: 'Courses' }).click();
    await page.waitForURL('**/courses');
    await page.getByRole('heading', { name: /Build the game development portfolio/i }).waitFor();

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(routeUrl('/'), { waitUntil: 'domcontentloaded' });
    await page.getByRole('button', { name: 'Open public navigation' }).click();
    await page.getByRole('navigation', { name: 'Mobile navigation' }).getByRole('link', { name: 'Testing Lab' }).click();
    await page.waitForURL('**/testing-lab');
    await page.getByRole('heading', { name: 'Testing Lab' }).waitFor();

    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto(routeUrl('/sign-in'), { waitUntil: 'domcontentloaded' });
    await page.getByRole('heading', { name: /Welcome back to GameGuild/i }).waitFor();
    await page.getByLabel('Email').waitFor();
    await page.getByLabel('Password').waitFor();

    await page.goto(routeUrl('/sign-up'), { waitUntil: 'domcontentloaded' });
    await page.getByRole('heading', { name: /Create your GameGuild account/i }).waitFor();
    await page.getByLabel('Full Name').waitFor();
    await page.getByLabel('Email').waitFor();
    await page.getByLabel('Password', { exact: true }).waitFor();
    await page.getByLabel('Confirm Password').waitFor();

    if (consoleErrors.length > 0) {
      const relevantErrors = consoleErrors.filter((message) => !/favicon/i.test(message));

      if (relevantErrors.length > 0) {
        throw new Error(`Console/page errors detected:\n${relevantErrors.join('\n')}`);
      }
    }

    console.log(`Public browser smoke passed against ${baseUrl}`);
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
