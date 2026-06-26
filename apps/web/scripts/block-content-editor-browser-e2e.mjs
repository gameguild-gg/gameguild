#!/usr/bin/env node

import { chromium } from 'playwright';

const baseUrl = (
  process.env.BLOCK_EDITOR_E2E_BASE_URL ??
  process.env.PUBLIC_E2E_BASE_URL ??
  process.env.NEXT_PUBLIC_APP_URL ??
  'http://localhost:3011'
).replace(/\/$/, '');

const routeChecks = [
  ['Editor home', '/en-US/block-content-editor', /My Projects|GameGuild Lexical Editor/i],
  ['Studio', '/en-US/block-content-editor/studio', /GameGuild Lexical Editor|Studio|Project/i],
  ['Viewer', '/en-US/block-content-editor/viewer', /GameGuild Lexical Editor|Viewer|Open Project/i],
  ['Quiz editor', '/en-US/block-content-editor/quiz-editor', /GameGuild Lexical Editor|Quiz/i],
  ['Doc editor', '/en-US/block-content-editor/doc-editor', /GameGuild Lexical Editor|Document|Project/i],
  ['Block editor', '/en-US/block-content-editor/block-editor', /GameGuild Lexical Editor|Project/i],
  ['Full editor', '/en-US/block-content-editor/full-editor', /GameGuild Lexical Editor|Project/i],
  ['Static viewer', '/en-US/block-content-editor/static-viewer', /GameGuild Lexical Editor|Static|Viewer/i],
  ['Publish', '/en-US/block-content-editor/publish', /GameGuild Lexical Editor|Publish/i],
];

const apiChecks = [
  ['Static project API', '/api/static-viewer/folder/projeto-17792247804366bs8q7l9t', 200],
  ['Static project traversal defense', '/api/static-viewer/folder/..%2Fsecret', 400],
  ['Static file missing defense', '/api/static-viewer/file/not-real/data.block-content-editor', 404],
  ['Web health API', '/api/health', 200],
];

function urlFor(route) {
  return `${baseUrl}${route}`;
}

async function bodyText(page) {
  return page.locator('body').innerText({ timeout: 15_000 });
}

async function waitForBodyText(page, pattern, label) {
  await page.waitForFunction(
    (source, flags) => {
      const pattern = new RegExp(source, flags);
      return pattern.test(document.body.innerText);
    },
    pattern.source,
    pattern.flags,
    { timeout: 30_000 },
  ).catch(async () => {
    const text = await bodyText(page);
    throw new Error(`${label}: expected page content matching ${pattern}, received "${text.slice(0, 200)}"`);
  });
}

async function assertNoErrorSurface(page, label) {
  await page.waitForLoadState('domcontentloaded', { timeout: 30_000 });
  await page.waitForFunction(() => document.body.innerText.trim().length > 0, undefined, { timeout: 30_000 });

  const text = await bodyText(page);
  if (/This page could not be found|404|500|Application error|Unhandled Runtime Error|Build Error/i.test(text)) {
    throw new Error(`${label}: rendered an error surface`);
  }
}

async function checkRoute(page, [label, route, contentPattern]) {
  const response = await page.goto(urlFor(route), { waitUntil: 'domcontentloaded', timeout: 45_000 });
  if (!response || response.status() !== 200) {
    throw new Error(`${label}: expected 200 but got ${response?.status() ?? 'no response'}`);
  }

  await assertNoErrorSurface(page, label);
  await waitForBodyText(page, contentPattern, label);
}

async function checkApi(page, [label, route, expectedStatus]) {
  const response = await page.goto(urlFor(route), { waitUntil: 'domcontentloaded', timeout: 30_000 });
  if (!response || response.status() !== expectedStatus) {
    throw new Error(`${label}: expected ${expectedStatus} but got ${response?.status() ?? 'no response'}`);
  }
}

async function runProjectCreateFlow(page) {
  await page.goto(urlFor('/en-US/block-content-editor'), { waitUntil: 'domcontentloaded', timeout: 45_000 });
  await assertNoErrorSurface(page, 'Project manager');

  const projectName = `E2E Project ${Date.now()}`;
  await page.getByRole('button', { name: 'New Project' }).click();
  await page.getByRole('dialog', { name: /Create New Project/i }).waitFor({ timeout: 15_000 });
  await page.getByLabel('Project Name *').fill(projectName);
  await page.getByPlaceholder('Search or create tags...').fill('e2e');
  await page.keyboard.press('Enter');
  await page.getByRole('button', { name: 'Create Project' }).click();
  await page.getByText(projectName).waitFor({ timeout: 20_000 });

  const projectCard = page.getByText(projectName);
  await projectCard.click();
  await page.waitForURL(/\/block-content-editor\/studio#/, { timeout: 20_000 });
  await assertNoErrorSurface(page, 'Created project studio');
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 960 } });
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
    for (const check of apiChecks) {
      await checkApi(page, check);
    }
    consoleErrors.length = 0;

    for (const check of routeChecks) {
      await checkRoute(page, check);
    }

    await runProjectCreateFlow(page);

    const relevantErrors = consoleErrors.filter(
      (message) => !/favicon|cloudflareinsights|beacon\.min\.js|Failed to load resource: the server responded with a status of 404/i.test(message),
    );

    if (relevantErrors.length > 0) {
      throw new Error(`Console/page errors detected:\n${relevantErrors.join('\n')}`);
    }

    console.log(`Block content editor browser E2E passed against ${baseUrl}`);
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
