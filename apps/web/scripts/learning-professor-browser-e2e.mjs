#!/usr/bin/env node

import { chromium } from 'playwright';

const apiBaseUrl = (process.env.API_BASE_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5295').replace(/\/$/, '');
const webBaseUrl = (process.env.PROFESSOR_E2E_BASE_URL ?? process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3011').replace(/\/$/, '');
const adminEmail = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? 'admin@game-guild.com';
const adminPassword = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? 'Admin123!';
const headless = !['0', 'false', 'no'].includes((process.env.PROFESSOR_E2E_HEADLESS ?? 'true').toLowerCase());

function unique() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

async function apiRequest(path, init = {}, accessToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
      ...init.headers,
    },
  });
  const body = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(`${init.method ?? 'GET'} ${path} failed with ${response.status}: ${JSON.stringify(body)}`);
  }
  return body;
}

async function apiStatus(path, init = {}, accessToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
      ...init.headers,
    },
  });
  return response.status;
}

async function deleteFixture(path, accessToken) {
  const deleteStatus = await apiStatus(path, { method: 'DELETE' }, accessToken);
  if (![204, 404].includes(deleteStatus)) {
    throw new Error(`DELETE ${path} failed with ${deleteStatus}`);
  }
}

function flattenCourseContent(value) {
  const roots = Array.isArray(value) ? value : Array.isArray(value?.items) ? value.items : [];
  const flattened = [];
  const visit = (item) => {
    if (!item || typeof item !== 'object') return;
    flattened.push(item);
    for (const child of Array.isArray(item.children) ? item.children : []) visit(child);
  };
  for (const root of roots) visit(root);
  return flattened;
}

async function bootstrap() {
  const signIn = await apiRequest('/v1/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ email: adminEmail, password: adminPassword }),
  });
  const tag = unique();
  const studentEmail = `professor-browser-student-${tag}@example.test`;
  await apiRequest('/v1/auth/sign-up', {
    method: 'POST',
    body: JSON.stringify({
      username: `professor_browser_student_${tag.replace(/[^a-z0-9]/gi, '_')}`,
      email: studentEmail,
      password: 'Str0ng!Passw0rd123!',
      tenantId: signIn.tenantId,
    }),
  });
  const lookup = await apiRequest(`/v1/users?email=${encodeURIComponent(studentEmail)}&limit=2`, {}, signIn.accessToken);
  const student = lookup.items?.find((candidate) => candidate.email?.toLowerCase() === studentEmail.toLowerCase());
  if (!student?.id) throw new Error(`Could not resolve temporary professor E2E student ${studentEmail}.`);

  return { accessToken: signIn.accessToken, studentEmail, studentId: student.id, tag };
}

async function assertNoErrorSurface(page, label) {
  await page.waitForLoadState('domcontentloaded');
  await page.locator('body').waitFor({ state: 'visible' });
  const body = await page.locator('body').innerText();
  if (/This page could not be found|Course not found|Unhandled Runtime Error|Build Error|Application error|Internal server error/i.test(body)) {
    throw new Error(`${label} rendered an error surface at ${page.url()}:\n${body.slice(0, 1200)}`);
  }
}

async function visit(page, courseRoute, suffix, _expectedText) {
  const path = `/dashboard/learning/courses/${courseRoute}${suffix ? `/${suffix}` : ''}`;
  await page.goto(`${webBaseUrl}${path}`, { waitUntil: 'domcontentloaded' });
  await assertNoErrorSurface(page, suffix || 'course root');
  await waitForClientHydration(page);
}

async function waitForText(page, value) {
  await page.getByText(value, { exact: false }).filter({ visible: true }).first().waitFor();
}

async function waitForClientHydration(page) {
  await page.waitForFunction(() => document.readyState !== 'loading');
  await page.waitForTimeout(250);
}

async function waitForReactControl(page, locator) {
  const element = await locator.elementHandle();
  if (!element) throw new Error('Could not resolve the expected React control.');

  await page.waitForFunction((control) => Object.keys(control).some((key) => key.startsWith('__reactProps$')), element);
}

function routeFromUrl(url) {
  const match = new URL(url).pathname.match(/\/courses\/([^/]+)/);
  if (!match) throw new Error(`Could not derive course route from ${url}`);
  return decodeURIComponent(match[1]);
}

async function waitForLocation(page, predicate, timeout = 45_000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const current = new URL(page.url());
    if (predicate(current)) return current;
    await page.waitForTimeout(100);
  }

  throw new Error(`Timed out waiting for the expected location. Current URL: ${page.url()}`);
}

async function waitForApiState(readState, predicate, timeout = 45_000) {
  const deadline = Date.now() + timeout;
  let lastState = null;

  while (Date.now() < deadline) {
    lastState = await readState();
    if (predicate(lastState)) return lastState;
    await new Promise((resolve) => setTimeout(resolve, 150));
  }

  throw new Error(`Timed out waiting for persisted API state. Last state: ${JSON.stringify(lastState)}`);
}

function readCourseMetadata(course) {
  if (!course?.metadata) return {};
  if (typeof course.metadata === 'object') return course.metadata;

  try {
    return JSON.parse(course.metadata);
  } catch {
    return {};
  }
}

async function run() {
  const fixture = await bootstrap();
  const browser = await chromium.launch({ headless });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const browserErrors = [];
  const failedResponses = [];
  let courseId = null;
  let deletedCourseId = null;
  let courseSlug = null;

  page.setDefaultTimeout(45_000);
  page.on('pageerror', (error) => browserErrors.push(error.message));
  page.on('console', (message) => {
    if (message.type() === 'error' && !/favicon|cloudflareinsights/i.test(message.text())) {
      browserErrors.push(message.text());
    }
  });
  page.on('response', (response) => {
    const url = new URL(response.url());
    if (url.origin === webBaseUrl && response.status() >= 400) {
      failedResponses.push(`${response.status()} ${url.pathname}${url.search}`);
    }
  });

  try {
    console.log('[professor-e2e] authentication');
    await page.goto(`${webBaseUrl}/sign-in`, { waitUntil: 'domcontentloaded' });
    await waitForClientHydration(page);
    await page.getByLabel('Email').fill(adminEmail);
    await page.getByLabel('Password').fill(adminPassword);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await waitForLocation(page, (url) => url.pathname.includes('/dashboard'));

    await page.goto(`${webBaseUrl}/dashboard/learning/courses/new`, { waitUntil: 'domcontentloaded' });
    console.log('[professor-e2e] create course');
    await waitForClientHydration(page);
    await waitForReactControl(page, page.getByLabel('Title *'));
    courseSlug = `professor-browser-${fixture.tag}`;
    await page.getByLabel('Title *').fill(`Professor Browser ${fixture.tag}`);
    await page.getByLabel('URL Slug').fill(courseSlug);
    await page.getByLabel('Description *').fill('A complete professor course used to validate every management subsection through the browser.');
    await page.getByRole('button', { name: 'Next', exact: true }).click();
    await page.getByLabel('Estimated Hours').fill('24');
    await page.getByLabel('Thumbnail URL').fill('https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop');
    await page.getByRole('button', { name: 'Next', exact: true }).click();
    await page.getByLabel('Max Enrollments').fill('20');
    await page.getByLabel('Skills Required').fill('Basic game development');
    await page.getByLabel('Skills Provided').fill('Production planning, playtesting, launch readiness');
    await page.getByRole('button', { name: 'Create Course', exact: true }).click();
    await waitForLocation(page, (url) => url.pathname.includes('/dashboard/learning/courses/') && !url.pathname.endsWith('/new'), 60_000);
    let courseRoute = routeFromUrl(page.url());
    await visit(page, courseRoute, 'overview', 'Course Readiness');
    await waitForText(page, 'Course Readiness');

    const courseLookup = await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken);
    courseId = courseLookup.id;

    await visit(page, courseRoute, 'listing/info', 'Course Identity');
    console.log('[professor-e2e] listing identity, media, launch, pricing');
    const updatedSlug = `${courseSlug}-updated`;
    await page.getByLabel('Course Title').fill(`Complete Professor Course ${fixture.tag}`);
    await page.getByLabel('URL Slug').fill(updatedSlug);
    await page.getByLabel('Skills Students Will Learn').fill('Course design, assessment planning, cohort delivery');
    await page.getByLabel('Prerequisites').fill('Basic game development');
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await waitForLocation(page, (url) => url.pathname.includes(`${updatedSlug}-by-`));
    courseSlug = updatedSlug;
    courseRoute = routeFromUrl(page.url());
    await page.getByLabel('Course Title').waitFor();
    if ((await page.getByLabel('Course Title').inputValue()) !== `Complete Professor Course ${fixture.tag}`) {
      throw new Error('Course identity changes were not persisted after the canonical route update.');
    }

    await visit(page, courseRoute, 'listing/media', 'Course Media');
    await page.getByLabel('Thumbnail URL').fill('');
    await page.getByLabel('Video URL').fill('');
    await page.getByRole('button', { name: 'Save Media' }).click();
    await waitForText(page, 'Media updated successfully');
    await page.getByLabel('Thumbnail URL').fill('https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop');
    await page.getByLabel('Video URL').fill('https://www.youtube.com/watch?v=dQw4w9WgXcQ');
    await page.getByRole('button', { name: 'Save Media' }).click();
    await waitForText(page, 'Media updated successfully');

    await visit(page, courseRoute, 'listing', 'Launch Controls');
    await page.getByLabel('Enrollment cap').fill('12');
    await page.getByRole('button', { name: 'Save launch controls' }).click();
    await waitForText(page, 'Listing controls updated successfully');
    await visit(page, courseRoute, 'listing', 'Launch Controls');
    await page.getByLabel('Enrollment cap').fill('0');
    await page.getByRole('button', { name: 'Save launch controls' }).click();
    await waitForText(page, 'Listing controls updated successfully');

    await visit(page, courseRoute, 'listing/pricing', 'Pricing');
    const monetization = page.getByLabel('Enable monetization');
    if ((await monetization.getAttribute('data-state')) !== 'checked') await monetization.click();
    await page.getByLabel('Price').fill('79');
    await page.getByLabel('Currency').fill('USD');
    await page.getByRole('button', { name: 'Save pricing' }).click();
    await waitForText(page, 'Pricing updated successfully');
    await visit(page, courseRoute, 'listing/pricing', 'Pricing');
    if ((await page.getByLabel('Price').inputValue()) !== '79') {
      throw new Error('The saved course offer was not restored from the API.');
    }
    await page.getByLabel('Price').fill('99');
    await page.getByRole('button', { name: 'Save pricing' }).click();
    await waitForText(page, 'Pricing updated successfully');

    await visit(page, courseRoute, 'listing/faq', 'Frequently Asked Questions');
    console.log('[professor-e2e] listing FAQ, projects, testimonials');
    await page.getByRole('button', { name: 'Add question' }).click();
    await page.getByLabel('Question', { exact: true }).last().fill('Who is this course for?');
    await page.getByLabel('Answer', { exact: true }).last().fill('Game developers preparing a production-ready portfolio project.');
    await page.getByRole('button', { name: 'Save FAQ' }).click();
    await waitForText(page, 'FAQ updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) => metadata.landingFaq?.some((item) => item.question === 'Who is this course for?'),
    );
    await page.getByLabel('Question', { exact: true }).first().fill('Who should take this production course?');
    await page.getByRole('button', { name: 'Save FAQ' }).click();
    await waitForText(page, 'FAQ updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) => metadata.landingFaq?.[0]?.question === 'Who should take this production course?',
    );
    await page.getByRole('button', { name: 'Add question' }).click();
    await page.getByLabel('Question', { exact: true }).last().fill('Temporary FAQ entry');
    await page.getByLabel('Answer', { exact: true }).last().fill('This entry proves FAQ removal.');
    await page.getByRole('button', { name: 'Save FAQ' }).click();
    await waitForText(page, 'FAQ updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) => metadata.landingFaq?.some((item) => item.question === 'Temporary FAQ entry'),
    );
    const temporaryFaqQuestion = page.getByLabel('Question', { exact: true }).last();
    if ((await temporaryFaqQuestion.inputValue()) !== 'Temporary FAQ entry') throw new Error('Temporary FAQ should be the last authored entry.');
    const temporaryFaqCard = temporaryFaqQuestion.locator('xpath=ancestor::div[contains(@class, "rounded-lg")][1]');
    await temporaryFaqCard.getByRole('button', { name: /Remove question/ }).click();
    await page.getByRole('button', { name: 'Save FAQ' }).click();
    await waitForText(page, 'FAQ updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) =>
        metadata.landingFaq?.[0]?.question === 'Who should take this production course?' &&
        !metadata.landingFaq.some((item) => item.question === 'Temporary FAQ entry'),
    );

    await visit(page, courseRoute, 'listing/projects', 'Project Carousel');
    await page.getByRole('button', { name: 'Add project' }).click();
    await page
      .getByLabel(/Project title/)
      .last()
      .fill('Playable vertical slice');
    await page
      .getByLabel(/Summary/)
      .last()
      .fill('Build and present a focused production milestone.');
    await page
      .getByLabel(/Deliverable/)
      .last()
      .fill('A playable build and retrospective.');
    await page.getByRole('button', { name: 'Save project carousel' }).click();
    await waitForText(page, 'Project carousel updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) => metadata.landingProjects?.some((item) => item.title === 'Playable vertical slice'),
    );
    await page
      .getByLabel(/Project title/)
      .last()
      .fill('Playable vertical slice showcase');
    await page.getByRole('button', { name: 'Save project carousel' }).click();
    await waitForText(page, 'Project carousel updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) => metadata.landingProjects?.[0]?.title === 'Playable vertical slice showcase',
    );
    await page.getByRole('button', { name: 'Add project' }).click();
    await page
      .getByLabel(/Project title/)
      .last()
      .fill('Temporary project');
    await page
      .getByLabel(/Summary/)
      .last()
      .fill('Temporary entry for removal coverage.');
    await page
      .getByLabel(/Deliverable/)
      .last()
      .fill('Temporary deliverable for removal coverage.');
    await page.getByRole('button', { name: 'Save project carousel' }).click();
    await waitForText(page, 'Project carousel updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) => metadata.landingProjects?.some((item) => item.title === 'Temporary project'),
    );
    const temporaryProjectTitle = page.getByLabel(/Project title/).last();
    if ((await temporaryProjectTitle.inputValue()) !== 'Temporary project') throw new Error('Temporary project should be the last authored slide.');
    const temporaryProjectCard = temporaryProjectTitle.locator('xpath=ancestor::div[contains(@class, "rounded-lg")][1]');
    await temporaryProjectCard.getByRole('button', { name: /Remove project/ }).click();
    await page.getByRole('button', { name: 'Save project carousel' }).click();
    await waitForText(page, 'Project carousel updated successfully');
    await waitForApiState(
      async () => readCourseMetadata(await apiRequest(`/v1/courses/slug/${encodeURIComponent(courseSlug)}`, {}, fixture.accessToken)),
      (metadata) =>
        metadata.landingProjects?.[0]?.title === 'Playable vertical slice showcase' &&
        !metadata.landingProjects.some((item) => item.title === 'Temporary project'),
    );
    await visit(page, courseRoute, 'listing/testimonials', 'Testimonials');

    await visit(page, courseRoute, 'content', 'Course Content');
    console.log('[professor-e2e] content module and lesson');
    await page.getByRole('button', { name: 'Add Module', exact: true }).last().click();
    await page.getByLabel('Title').fill('Production Foundations');
    await page.getByLabel('Description (optional)').fill('Prepare the project, scope, and delivery plan.');
    await page.getByRole('button', { name: 'Add Module', exact: true }).last().click();
    await waitForText(page, 'Production Foundations');
    const moduleState = await waitForApiState(
      () => apiRequest(`/v1/courses/${courseId}/content`, {}, fixture.accessToken),
      (content) => flattenCourseContent(content).some((item) => item.title === 'Production Foundations'),
    );
    const createdModule = flattenCourseContent(moduleState).find((item) => item.title === 'Production Foundations');
    if (!createdModule?.id) throw new Error('The content API did not return the newly created module id.');
    const moduleCard = page.getByText('Production Foundations', { exact: true }).locator('xpath=ancestor::*[@data-slot="card"][1]');
    await moduleCard.getByRole('button', { name: /Add lesson/i }).click();
    await page.getByLabel('Title').fill('Define the playable promise');
    await page.getByRole('button', { name: 'Add Lesson', exact: true }).click();
    await waitForApiState(
      () => apiRequest(`/v1/courses/${courseId}/content`, {}, fixture.accessToken),
      (content) => flattenCourseContent(content).some((item) =>
        item.title === 'Define the playable promise' && String(item.parentId).toLowerCase() === String(createdModule.id).toLowerCase()),
    );
    await waitForText(page, 'Define the playable promise');
    const lessonRow = page.getByText('Define the playable promise', { exact: true }).locator('xpath=ancestor::div[contains(@class, "group")][1]');
    await lessonRow.getByRole('button', { name: 'Edit Lesson' }).click();
    await page.getByLabel('Description').fill('Define the smallest experience that proves the product promise.');
    await page.getByLabel('Body').fill('# Playable promise\n\nDescribe the player, the outcome, and the evidence required.');
    await page.getByLabel(/Estimated minutes/).fill('35');
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await waitForText(page, 'Saved successfully');
    await page.getByRole('button', { name: 'Cancel' }).click();
    await page.getByRole('button', { name: 'Edit module' }).click();
    await page.getByLabel('Title').fill('Production Delivery');
    await page.getByLabel('Description (optional)').fill('Updated module description for the complete professor flow.');
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await waitForText(page, 'Production Delivery');

    await visit(page, courseRoute, 'assessments', 'Assessments');
    console.log('[professor-e2e] assessment group and assessment');
    await page.getByRole('button', { name: 'Add Group', exact: true }).first().click();
    await page.getByLabel('Group name').fill('Final Project');
    await page.getByLabel('Weight percent').fill('100');
    await page.getByRole('button', { name: 'Create Group' }).click();
    await waitForText(page, 'Final Project');
    await page.getByRole('button', { name: 'Add Assessment', exact: true }).first().click();
    await page.getByLabel('Title').fill('Vertical Slice Review');
    await page.getByLabel('Max Score').fill('100');
    await page.getByLabel('Passing Score').fill('70');
    await page.getByLabel('Grade group').click();
    await page.getByRole('option', { name: /Final Project/ }).click();
    await page.getByRole('button', { name: 'Create', exact: true }).click();
    await waitForApiState(
      () => apiRequest(`/v1/assessments/course/${courseId}`, {}, fixture.accessToken),
      (assessments) => Array.isArray(assessments) && assessments.some((assessment) => assessment.title === 'Vertical Slice Review'),
    );
    await visit(page, courseRoute, 'assessments', 'Assessments');
    await waitForText(page, 'Vertical Slice Review');

    await page.getByRole('link', { name: /Vertical Slice Review/ }).click();
    await waitForText(page, 'Assessment Editor');
    await page.getByLabel('Title').fill('Vertical Slice Final Review');
    await page.getByLabel('Description').fill('Updated assessment instructions for the final production review.');
    await page.getByLabel('Max Score').fill('120');
    await page.getByLabel('Passing Score').fill('84');
    await page.getByLabel('Time Limit (minutes)').fill('45');
    await page.getByLabel('Max Attempts').fill('2');
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await waitForText(page, 'Saved successfully');
    await page.getByRole('button', { name: 'Back', exact: true }).click();
    await waitForText(page, 'Vertical Slice Final Review');
    await page.getByRole('button', { name: 'Edit group Final Project' }).click();
    await page.getByLabel('Group name').fill('Capstone Delivery');
    await page.getByLabel('Description').fill('Weighted capstone assessment block.');
    await page.getByLabel('Weight percent').fill('100');
    await page.getByRole('button', { name: 'Save Group' }).click();
    await waitForText(page, 'Capstone Delivery');

    await visit(page, courseRoute, 'content', 'Course Content');
    await page.getByRole('button', { name: 'Attach assessment' }).click();
    await page.getByRole('button', { name: /Vertical Slice Final Review/ }).click();
    await waitForText(page, 'Vertical Slice Final Review');

    await visit(page, courseRoute, 'classes', 'Classes');
    console.log('[professor-e2e] class, certificate, enrollment');
    const start = new Date(Date.now() + 86_400_000);
    const end = new Date(start.getTime() + 7_200_000);
    const localInput = (date) => new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
    await page.getByLabel('Name').fill('July Production Cohort');
    await page.getByLabel('Description').fill('Live review and production support cohort.');
    await page.getByLabel('Start').fill(localInput(start));
    await page.getByLabel('End').fill(localInput(end));
    await page.getByLabel('Capacity').fill('20');
    await page.getByLabel('Meeting URL or room').fill('https://meet.example.test/gameguild');
    await page.getByRole('button', { name: 'Schedule class' }).click();
    await waitForText(page, 'Class scheduled.');
    await waitForText(page, 'July Production Cohort');
    await page.getByRole('link', { name: /July Production Cohort/ }).click();
    await page.getByLabel('Name').fill('August Production Cohort');
    await page.getByLabel('Description').fill('Updated cohort schedule and production support.');
    await page.getByLabel('Capacity').fill('24');
    await page.getByRole('button', { name: 'Save class' }).click();
    await waitForText(page, 'Class updated.');
    const openEnrollmentButton = page.getByRole('button', { name: 'Open enrollment' });
    await page.waitForFunction((button) => !button.disabled, await openEnrollmentButton.elementHandle());
    await openEnrollmentButton.click();
    await waitForText(page, 'Class status updated.');
    await visit(page, courseRoute, 'classes', 'Classes');
    await waitForText(page, 'August Production Cohort');

    await visit(page, courseRoute, 'certificates', 'Certificates');
    await page.getByLabel('Name').fill('Production Course Completion');
    await page.getByRole('button', { name: 'Create template' }).click();
    await waitForText(page, 'Certificate template created.');
    await waitForText(page, 'Production Course Completion');
    await page.getByRole('link', { name: /Production Course Completion/ }).click();
    await page.getByLabel('Template name').fill('Game Production Certificate');
    await page.getByLabel('Description').fill('Updated completion credential for production students.');
    await page.getByRole('button', { name: 'Save certificate template' }).click();
    await waitForText(page, 'Certificate template saved.');
    await visit(page, courseRoute, 'certificates', 'Certificates');
    await waitForText(page, 'Game Production Certificate');

    await visit(page, courseRoute, 'students', 'Students');
    await page.getByRole('button', { name: 'Enroll student', exact: true }).first().click();
    await page.getByRole('textbox', { name: 'Student' }).fill(fixture.studentEmail);
    await page.getByRole('button', { name: 'Enroll student', exact: true }).last().click();
    await waitForText(page, 'Student enrolled successfully');
    await waitForText(page, fixture.studentEmail);
    const studentRow = page.getByRole('row').filter({ hasText: fixture.studentEmail });
    await studentRow.getByRole('checkbox').click();
    await page.getByRole('button', { name: 'Send Message' }).click();
    await page.getByLabel('Subject').fill('Production milestone reminder');
    await page.getByRole('textbox', { name: 'Message' }).fill('Bring your playable build and retrospective to the next review.');
    await page.getByRole('button', { name: 'Send message' }).click();
    await waitForText(page, 'Message sent to 1 student');

    await visit(page, courseRoute, 'analytics', 'Analytics');
    await waitForLocation(page, (url) => url.pathname.endsWith('/analytics/engagement'));
    for (const suffix of ['analytics/completion', 'analytics/engagement', 'analytics/revenue']) {
      await visit(page, courseRoute, suffix, 'Analytics');
    }
    console.log('[professor-e2e] analytics, support, settings, preview');

    await visit(page, courseRoute, 'support/discussions', 'Discussions');
    await page.getByLabel('Title').fill('Milestone review expectations');
    await page.getByLabel('Content').fill('What evidence should students bring to the milestone review?');
    await page.getByRole('button', { name: 'Create discussion' }).click();
    await waitForText(page, 'Milestone review expectations');
    await visit(page, courseRoute, 'support/tickets', 'Support Tickets');

    await visit(page, courseRoute, 'settings/access', 'Access');
    await page.getByLabel('Maximum Enrollments').fill('0');
    await page.getByLabel('Enrollment deadline').fill('');
    await page.getByRole('button', { name: 'Save Access Settings' }).click();
    await waitForText(page, 'Access settings saved successfully');

    await visit(page, courseRoute, 'settings/notifications', 'Notifications');
    await page.getByLabel('Class reminder minutes').fill('1440, 60, 10');
    await page.getByRole('button', { name: 'Save notification settings' }).click();
    await waitForText(page, 'Notification settings saved');

    await visit(page, courseRoute, 'settings/integrations', 'Integrations');
    await page.getByRole('button', { name: 'Add webhook' }).click();
    await page.getByLabel('Webhook URL').fill('https://hooks.example.test/gameguild');
    await page.getByLabel('Events').fill('enrollment.created,course.completed');
    await page.getByRole('button', { name: 'Add to course' }).click();
    await page.getByRole('button', { name: 'Save integration settings' }).click();
    await waitForText(page, 'Integration settings saved');
    await page.getByRole('button', { name: 'Remove webhook https://hooks.example.test/gameguild' }).click();
    await page.getByRole('button', { name: 'Save integration settings' }).click();
    await waitForText(page, 'Integration settings saved');

    await visit(page, courseRoute, 'preview', 'Course Preview');
    await visit(page, courseRoute, 'overview', 'Course Readiness');
    await page.getByRole('button', { name: 'Publish', exact: true }).first().click();
    await waitForText(page, 'Published');

    console.log('[professor-e2e] public storefront synchronization');
    await page.goto(`${webBaseUrl}/courses/${courseSlug}`, { waitUntil: 'domcontentloaded' });
    await assertNoErrorSurface(page, 'public course storefront');
    await waitForText(page, `Complete Professor Course ${fixture.tag}`);
    await waitForText(page, 'Who should take this production course?');
    await waitForText(page, 'Playable vertical slice showcase');
    await visit(page, courseRoute, 'overview', 'Course Readiness');

    console.log('[professor-e2e] lifecycle and subsection cleanup');
    await page.getByRole('button', { name: 'Unpublish' }).click();
    await waitForText(page, 'Draft');
    await page.getByRole('button', { name: 'Publish', exact: true }).first().click();
    await waitForText(page, 'Published');
    await page.getByRole('button', { name: 'Archive' }).click();
    await waitForText(page, 'Archived');
    await page.getByRole('button', { name: 'Re-publish' }).click();
    await waitForText(page, 'Published');

    await visit(page, courseRoute, 'students', 'Students');
    const enrolledStudentRow = page.getByRole('row').filter({ hasText: fixture.studentEmail });
    await enrolledStudentRow.getByRole('checkbox').click();
    await page.getByRole('button', { name: 'Remove', exact: true }).click();
    await page.getByRole('button', { name: 'Confirm removal' }).click();
    await waitForText(page, '1 student removed');

    await visit(page, courseRoute, 'certificates', 'Certificates');
    await page.getByRole('button', { name: 'Delete Game Production Certificate' }).click();
    await waitForText(page, 'Certificate template deleted.');

    await visit(page, courseRoute, 'classes', 'Classes');
    await page.getByRole('button', { name: 'Delete August Production Cohort' }).click();
    await waitForText(page, 'Class deleted.');

    await visit(page, courseRoute, 'content', 'Course Content');
    const updatedLessonRow = page.getByText('Define the playable promise', { exact: true }).locator('xpath=ancestor::div[contains(@class, "group")][1]');
    await updatedLessonRow.locator('button[aria-label="Detach assessment"]').click();
    await updatedLessonRow.getByRole('button', { name: 'Delete', exact: true }).click();
    await page.getByRole('button', { name: 'Delete', exact: true }).last().click();
    await waitForText(page, 'Production Delivery');
    await page.getByRole('button', { name: 'Delete module' }).click();
    await page.getByRole('button', { name: 'Delete', exact: true }).last().click();

    await visit(page, courseRoute, 'assessments', 'Assessments');
    await page.getByRole('link', { name: /Vertical Slice Final Review/ }).click();
    page.once('dialog', (dialog) => dialog.accept());
    await page.getByRole('button', { name: 'Delete Assessment' }).click();
    await waitForLocation(page, (url) => url.pathname.endsWith('/assessments'));
    await page.getByRole('button', { name: 'Delete group Capstone Delivery' }).click();
    await page.getByRole('button', { name: 'Delete Group' }).click();

    await visit(page, courseRoute, 'support/discussions', 'Discussions');
    await page.getByRole('button', { name: 'Delete Milestone review expectations' }).click();

    await visit(page, courseRoute, 'listing/faq', 'Frequently Asked Questions');
    await page.getByRole('button', { name: 'Remove question 1' }).click();
    await page.getByRole('button', { name: 'Save FAQ' }).click();
    await waitForText(page, 'FAQ updated successfully');

    await visit(page, courseRoute, 'listing/projects', 'Project Carousel');
    const projectCountBeforeCleanup = await page.getByLabel(/Project title/).count();
    await page.getByRole('button', { name: `Remove project ${projectCountBeforeCleanup}` }).click();
    await page.getByRole('button', { name: 'Save project carousel' }).click();
    await waitForText(page, 'Project carousel updated successfully');

    await visit(page, courseRoute, 'settings/danger', 'Danger Zone');
    await page.getByRole('button', { name: 'Delete Course' }).click();
    await page.getByLabel(/type.*confirm/i).fill(`Complete Professor Course ${fixture.tag}`);
    await page.getByRole('button', { name: 'Permanently Delete' }).click();
    await waitForLocation(page, (url) => url.pathname.endsWith('/dashboard/learning/courses'));
    deletedCourseId = courseId;
    courseId = null;

    const meaningfulFailures = [...new Set(failedResponses)].filter((value) => !/favicon|manifest\.webmanifest/.test(value));
    if (meaningfulFailures.length > 0) {
      throw new Error(`HTTP failures detected during professor journey:\n${meaningfulFailures.join('\n')}`);
    }
    if (browserErrors.length > 0) {
      throw new Error(`Browser errors detected during professor journey:\n${[...new Set(browserErrors)].join('\n')}`);
    }

    console.log(`Professor learning browser E2E passed for ${courseSlug} (${deletedCourseId ?? courseId}).`);
  } catch (error) {
    const pageText = await page
      .locator('body')
      .innerText()
      .catch(() => 'Unable to read page body.');
    console.error(`[professor-e2e] failed at ${page.url()}`);
    console.error(`[professor-e2e] HTTP failures: ${[...new Set(failedResponses)].join(', ') || 'none'}`);
    console.error(`[professor-e2e] browser errors: ${[...new Set(browserErrors)].join(' | ') || 'none'}`);
    console.error(`[professor-e2e] page excerpt:\n${pageText.slice(0, 2400)}`);
    throw error;
  } finally {
    if (courseId) {
      await deleteFixture(`/v1/courses/${courseId}`, fixture.accessToken);
    }
    await deleteFixture(`/v1/users/${fixture.studentId}`, fixture.accessToken);
    await browser.close();
  }
}

run().catch((error) => {
  console.error(error instanceof Error ? (error.stack ?? error.message) : error);
  process.exit(1);
});
