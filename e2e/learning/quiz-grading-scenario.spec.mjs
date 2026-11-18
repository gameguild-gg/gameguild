import { chmod, mkdir } from 'node:fs/promises';
import { expect, test } from 'playwright/test';
import { assertSharedAuthCookie, trackAppHttpFailures } from '../../apps/web/scripts/learning-browser-e2e-support.mjs';
import { authenticatePersona, request } from '../../apps/web/src/lib/grading-scenarios/adapters/quiz/api.server.mjs';
import {
  checkpointIndex,
  expectedFinalScore,
  getScenarioDefinition,
  hasAIReview,
  hasInstructorReview,
  hasPeerReview,
  hasSelfReview,
  selectQuizDefinitions,
} from '../../apps/web/src/lib/grading-scenarios/adapters/quiz/definitions.mjs';
import { QUESTION_TYPE_CASES, getValidationCase } from '../../apps/web/src/lib/grading-scenarios/adapters/quiz/capability-catalog.mjs';
import {
  readManifest,
  recomputeManifestCheckpoint,
  storageStatePath,
  writeManifest,
} from '../../apps/web/src/lib/grading-scenarios/adapters/quiz/manifest.server.mjs';
import { validationEvidenceDirectory } from '../../apps/web/src/lib/grading-scenarios/adapters/quiz/report.server.mjs';
import { buildScenarioUrls } from '../../apps/web/src/lib/grading-scenarios/adapters/quiz/urls.mjs';
import { expectQuestionVisible, questionLocator, startActivity } from './quiz-grading/support/browser-support.mjs';
import { answerQuestionTypeCase } from './quiz-grading/support/question-type-drivers.mjs';

test.use({ trace: 'retain-on-failure', screenshot: 'only-on-failure' });
test.skip(!process.env.E2E_RUN, 'Set E2E_RUN=1 to execute the local scenario.');

const scenarioKey = process.env.QUIZ_GRADING_SCENARIO;
const requestedQuizKeys = (process.env.QUIZ_GRADING_QUIZZES ?? '')
  .split(',')
  .map((value) => value.trim())
  .filter(Boolean);

if (!scenarioKey) {
  test('quiz grading scenario requires a scenario key', () => {
    throw new Error('QUIZ_GRADING_SCENARIO is required.');
  });
} else {
  const definition = getScenarioDefinition(scenarioKey);
  const quizzes = selectQuizDefinitions(definition, requestedQuizKeys).filter((quiz) => !quiz.validationCaseId);

  for (const quiz of quizzes) {
    test(`${quiz.key}: ${quiz.reviewMethods.join(' -> ')} across its personas`, async ({ browser }) => {
      test.setTimeout(360_000);
      let manifest = await readManifest(scenarioKey);
      if (!manifest.quizKeys.includes(quiz.key)) {
        throw new Error(`Quiz "${quiz.key}" was not provisioned in scenario "${scenarioKey}". Reset and prepare it with that quiz selected.`);
      }
      const resources = manifest.resources.quizzes[quiz.key];
      if (checkpointIndex(resources.checkpoint) < checkpointIndex('learner-ready')) {
        throw new Error(`Browser execution for "${quiz.key}" requires learner-ready; current checkpoint is ${resources.checkpoint}.`);
      }
      if (quiz.subject === 'collective') {
        test.skip(true, 'The collective scenario remains covered through API verification.');
      }

      const instructor = await createPersonaPage(browser, manifest, 'instructor');
      const learnerA = await createPersonaPage(browser, manifest, 'learnerA');
      const learnerB = hasPeerReview(quiz) ? await createPersonaPage(browser, manifest, 'learnerB') : null;
      const outsider = await createPersonaPage(browser, manifest, 'outsider');
      try {
        await verifyAuthoringAndTestRun(instructor.page, manifest, quiz);
        await verifyOutsiderGate(outsider.page, manifest, quiz);
        await verifyLearnerCourseDiscovery(learnerA.page, manifest);

        if (resources.checkpoint === 'learner-ready') {
          await ensureLearnerSubmission(learnerA.page, manifest, quiz, 'learnerA');
          if (learnerB) {
            await ensureLearnerSubmission(learnerB.page, manifest, quiz, 'learnerB');
            await reviewAsPeer(learnerA.page, manifest, quiz, 'learnerA');
            await reviewAsPeer(learnerB.page, manifest, quiz, 'learnerB');
            await synchronizeSubmission(manifest, quiz, 'learnerA');
            await synchronizeSubmission(manifest, quiz, 'learnerB');
          }
          advanceQuizCheckpoint(manifest, quiz.key, hasInstructorReview(quiz) ? 'review-ready' : 'release-ready');
          await persist(manifest);
        }

        if (hasInstructorReview(quiz) && resources.checkpoint === 'review-ready') {
          for (const personaKey of Object.keys(resources.submissions)) {
            await reviewAsInstructor(instructor.page, manifest, quiz, personaKey);
            await synchronizeSubmission(manifest, quiz, personaKey);
          }
          advanceQuizCheckpoint(manifest, quiz.key, 'release-ready');
          await persist(manifest);
        }

        if (resources.checkpoint === 'release-ready') {
          for (const personaKey of Object.keys(resources.submissions)) {
            await releaseAsInstructor(instructor.page, manifest, quiz, personaKey);
            await synchronizeSubmission(manifest, quiz, personaKey);
          }
          advanceQuizCheckpoint(manifest, quiz.key, 'released');
          await persist(manifest);
        }

        await verifyLearnerResult(learnerA.page, manifest, quiz);
        if (learnerB) {
          await verifyLearnerResult(learnerB.page, manifest, quiz);
        }
        instructor.failures.assertNone(`${quiz.key} instructor scenario`);
        learnerA.failures.assertNone(`${quiz.key} learner A scenario`);
        learnerB?.failures.assertNone(`${quiz.key} learner B scenario`);
        outsider.failures.assertNone(`${quiz.key} outsider scenario`);
      } finally {
        await Promise.allSettled([instructor, learnerA, learnerB, outsider].filter(Boolean).map((persona) => persona.context.close()));
      }
    });
  }
}

async function verifyLearnerCourseDiscovery(page, manifest) {
  await page.goto(manifest.urls.learnerCourses, {
    waitUntil: 'domcontentloaded',
  });
  await expect(page.getByRole('heading', { name: 'My courses' })).toBeVisible({
    timeout: 60_000,
  });
  await expect(page.getByText(manifest.marker).first()).toBeVisible();
}

async function createPersonaPage(browser, manifest, personaKey) {
  const context = await browser.newContext();
  const page = await context.newPage();
  const failures = trackAppHttpFailures(page, [manifest.environment.webBaseUrl, manifest.environment.apiBaseUrl]);
  const persona = manifest.personas[personaKey];
  await browserSignIn(page, manifest.environment.webBaseUrl, persona.email, persona.password);
  const statePath = storageStatePath(manifest.definitionKey, personaKey);
  await context.storageState({ path: statePath });
  await chmod(statePath, 0o600).catch(() => {});
  return { context, page, failures };
}

async function browserSignIn(page, webBaseUrl, email, password) {
  const request = page.context().request;
  const csrfResponse = await request.get(`${webBaseUrl}/api/auth/csrf`);
  const csrf = await csrfResponse.json().catch(() => null);
  const result =
    csrfResponse.ok() && typeof csrf?.csrfToken === 'string'
      ? await request
          .post(`${webBaseUrl}/api/auth/signin/credentials`, {
            headers: { 'Content-Type': 'application/json' },
            data: {
              email,
              password,
              csrfToken: csrf.csrfToken,
              redirect: false,
              redirectTo: '/',
            },
          })
          .then((response) => ({
            ok: response.ok(),
            stage: 'credentials',
            status: response.status(),
          }))
      : { ok: false, stage: 'csrf', status: csrfResponse.status() };
  if (!result.ok) {
    throw new Error(`Browser sign-in failed at ${result.stage} (HTTP ${result.status}).`);
  }
  assertSharedAuthCookie(await page.context().cookies([webBaseUrl]));
}

async function verifyAuthoringAndTestRun(page, manifest, quiz) {
  const resources = manifest.resources.quizzes[quiz.key];
  const urls = manifest.urls.quizzes[quiz.key];
  const api = await authenticatePersona(manifest, 'instructor', {
    create: false,
  });
  const before = await readGradingQueue(api, resources.assessmentId);
  await refreshPersonaPage(page, manifest, 'instructor');
  if (hasAIReview(quiz)) {
    await verifyAIModelAnswer(page, urls.contentEditor, quiz);
  }
  await page.goto(urls.assessmentEditor, {
    waitUntil: 'domcontentloaded',
  });
  await expect(page.getByRole('heading', { name: new RegExp(quiz.title, 'i') }).first()).toBeVisible({ timeout: 60_000 });
  if (process.env.GRADING_AI_LIVE_TEST === '1' && hasAIReview(quiz)) {
    await screenshot(page, manifest, quiz.key, '01-authoring-live-smoke.png');
    return;
  }
  await openTestRunDialog(page);
  await page.getByRole('button', { name: 'Start test run' }).click();

  if (hasPeerReview(quiz)) {
    const tabs = page.getByRole('tab');
    await expect(tabs).toHaveCount(2);
    for (let index = 0; index < 2; index += 1) {
      await selectSyntheticSubject(tabs, index);
      await answerTestRun(page, quiz);
    }
    for (let index = 1; index >= 0; index -= 1) {
      await selectSyntheticSubject(tabs, index);
      await page.getByRole('button', { name: 'Claim anonymous response' }).click();
      await page.locator('input[id^="peer-score-"]').fill(String(quiz.expected.peerReviewScore / 100));
      await page.locator('textarea[id^="peer-feedback-"]').fill(`Synthetic peer feedback ${index + 1}.`);
      await page.getByRole('button', { name: 'Submit peer review' }).click();
      await expect(page.getByRole('button', { name: 'Submit peer review' })).toHaveCount(0, { timeout: 30_000 });
    }
    if (hasInstructorReview(quiz)) {
      for (let index = 0; index < 2; index += 1) {
        await selectSyntheticSubject(tabs, index);
        await completeTestInstructorReview(page, quiz);
      }
    }
  } else {
    await answerTestRun(page, quiz);
    if (hasSelfReview(quiz)) {
      await completeSelfReview(page, quiz);
    }
    if (hasInstructorReview(quiz)) {
      await completeTestInstructorReview(page, quiz);
    }
  }

  await expect(page.getByText('Diagnostic result').first()).toBeVisible({
    timeout: 30_000,
  });
  await screenshot(page, manifest, quiz.key, '01-test-run.png');
  const after = await readGradingQueue(api, resources.assessmentId);
  expect(after.total ?? 0).toBe(before.total ?? 0);
}

async function verifyAIModelAnswer(page, contentEditorUrl, quiz) {
  const essayIndex = quiz.document.order.findIndex(([itemId]) => quiz.document.blocks[itemId]?.type === 'ESSAY');
  if (essayIndex < 0) return;

  const [essayId] = quiz.document.order[essayIndex];
  const expected = quiz.document.blocks[essayId].correctAnswerPlain;
  if (!expected) {
    throw new Error(`AIReview quiz "${quiz.key}" has no Essay model answer.`);
  }

  await page.goto(contentEditorUrl, { waitUntil: 'domcontentloaded' });
  await expect(page.locator('[data-quiz-collection-editor][data-hydrated="true"]')).toBeVisible({ timeout: 180_000 });
  await page.getByRole('button', { name: `Edit question ${essayIndex + 1}` }).click();
  const editor = page.getByRole('dialog', { name: 'Quiz Builder' });
  await expect(editor).toBeVisible({ timeout: 60_000 });
  await expect(editor.locator('[contenteditable="true"]').filter({ hasText: expected })).toBeVisible({ timeout: 60_000 });
  await editor.getByRole('button', { name: 'Close quiz editor' }).click();
  await expect(editor).toHaveCount(0);
}

async function openTestRunDialog(page) {
  const trigger = page.getByRole('button', { name: 'Test assessment' });
  const heading = page.getByRole('heading', { name: 'Assessment test run' });
  let lastError;

  for (let attempt = 0; attempt < 3; attempt += 1) {
    await trigger.click();
    try {
      await heading.waitFor({ state: 'visible', timeout: 10_000 });
      return;
    } catch (error) {
      lastError = error;
    }
  }

  throw lastError;
}

async function selectSyntheticSubject(tabs, index) {
  const tab = tabs.nth(index);
  await tab.click();
  await expect(tab).toHaveAttribute('aria-selected', 'true');
}

async function answerTestRun(page, quiz) {
  for (const validationCase of quizBrowserCases(quiz)) {
    await expectQuestionVisible(page, validationCase);
    await answerQuestionTypeCase(page, validationCase, 'correct');
    const question = questionLocator(page, validationCase);
    const questionPanel = question.locator("xpath=ancestor::*[.//button[normalize-space()='Submit answer']][1]");
    await questionPanel.getByRole('button', { name: 'Submit answer', exact: true }).click();
  }
  await page.getByRole('button', { name: 'Run grading workflow' }).click();
  await expect(page.getByRole('button', { name: 'Run grading workflow' })).toHaveCount(0, { timeout: 30_000 });
}

async function completeSelfReview(page, quiz) {
  for (const [itemId, score] of reviewItemScores(quiz, quiz.expected.selfReviewScore)) {
    await page.locator(`#self-score-${itemId}`).fill(String(score / 100));
  }
  await page.getByLabel('Overall self-review feedback').fill('Scenario self review.');
  await page.getByRole('button', { name: 'Submit self review' }).click();
  await expect(page.getByText('Self review submitted.')).toBeVisible({
    timeout: 30_000,
  });
}

async function completeTestInstructorReview(page, quiz) {
  for (const [itemId, score] of reviewItemScores(quiz, quiz.expected.instructorScore)) {
    await page.locator(`#test-score-${itemId}`).fill(String(score / 100));
  }
  await page.getByRole('button', { name: 'Complete instructor review' }).click();
  await expect(page.getByRole('button', { name: 'Complete instructor review' })).toHaveCount(0, { timeout: 30_000 });
}

async function verifyOutsiderGate(page, manifest, quiz) {
  await page.goto(manifest.urls.quizzes[quiz.key].learnerActivity, {
    waitUntil: 'domcontentloaded',
  });
  await expect(page.getByRole('heading', { name: 'Join this course' })).toBeVisible({ timeout: 60_000 });
  await expect(page.getByRole('button', { name: 'Start Activity' })).toHaveCount(0);
  await screenshot(page, manifest, quiz.key, '02-outsider-denied.png');
}

async function submitAsLearner(page, manifest, quiz, personaKey) {
  await page.goto(manifest.urls.quizzes[quiz.key].learnerActivity, {
    waitUntil: 'domcontentloaded',
  });
  const validationCases = quizBrowserCases(quiz);
  await startActivity(page, questionLocator(page, validationCases[0]));
  for (const [index, validationCase] of validationCases.entries()) {
    await expectQuestionVisible(page, validationCase);
    await answerQuestionTypeCase(page, validationCase, 'correct');
    await page.getByRole('button', { name: 'Submit answer', exact: true }).click();
    if (index < validationCases.length - 1) {
      await page.getByRole('button', { name: 'Next', exact: true }).click();
    }
  }
  await page.getByRole('button', { name: 'Submit Quiz' }).click();
  if (hasSelfReview(quiz)) {
    await completeSelfReview(page, quiz);
  }
  await expect(page.getByText(/Submitted|result has not been released|instructor review|Self review submitted/i).first()).toBeVisible({ timeout: 60_000 });
  await screenshot(page, manifest, quiz.key, `03-${personaKey}-submitted.png`);
}

async function ensureLearnerSubmission(page, manifest, quiz, personaKey) {
  const alreadySubmitted = await synchronizeSubmission(manifest, quiz, personaKey, { wait: false });
  if (!alreadySubmitted) {
    await submitAsLearner(page, manifest, quiz, personaKey);
    await synchronizeSubmission(manifest, quiz, personaKey);
  }
  await persist(manifest);
}

async function reviewAsPeer(page, manifest, quiz, personaKey) {
  const resources = manifest.resources.quizzes[quiz.key];
  const assessmentId = resources.assessmentId;
  const claimedReviewId = resources.peerReviewIds[personaKey];
  if (claimedReviewId) {
    await page.goto(`${manifest.urls.learnerReviews}/${claimedReviewId}`, {
      waitUntil: 'domcontentloaded',
    });
  } else {
    await page.goto(manifest.urls.learnerReviews, {
      waitUntil: 'domcontentloaded',
    });
    const reviewPanel = page.getByRole('tabpanel', {
      name: 'To review',
      exact: true,
    });
    await expect(reviewPanel).toBeVisible({ timeout: 60_000 });
    const task = reviewPanel.getByTestId(`review-task-${assessmentId}`);
    await expect(task).toBeVisible({ timeout: 60_000 });
    await Promise.all([
      page.waitForURL((url) => /\/learn\/reviews\/[0-9a-f-]+$/i.test(url.pathname), { timeout: 60_000 }),
      task.getByRole('button', { name: 'Review a peer' }).click(),
    ]);
    resources.peerReviewIds[personaKey] = new URL(page.url()).pathname.split('/').at(-1);
    await persist(manifest);
  }
  await expect(page.getByTestId('anonymous-header')).toBeVisible({
    timeout: 60_000,
  });
  if (await page.getByText('Review already submitted.', { exact: true }).isVisible()) {
    return;
  }
  await page.getByTestId('peer-score-input').fill(String(quiz.expected.peerReviewScore / 100));
  await page.getByTestId('peer-feedback').fill(`Peer feedback from ${personaKey}.`);
  await page.getByTestId('submit-review').click();
  await expect(page.getByRole('heading', { name: 'Peer reviews' })).toBeVisible({
    timeout: 60_000,
  });
  await persist(manifest);
  await screenshot(page, manifest, quiz.key, `04-${personaKey}-peer-review.png`);
}

async function reviewAsInstructor(page, manifest, quiz, personaKey) {
  const urls = manifest.urls.quizzes[quiz.key];
  await refreshPersonaPage(page, manifest, 'instructor');
  await page.goto(urls.speedGraders[personaKey], {
    waitUntil: 'domcontentloaded',
  });
  await expect(page.getByTestId('grading-panel')).toBeVisible({
    timeout: 60_000,
  });
  for (const [itemId, score] of reviewItemScores(quiz, quiz.expected.instructorScore)) {
    await page.getByTestId(`item-score-${itemId}`).fill(String(score / 100));
  }
  await page.getByTestId('resolve-instructor-review').click();
  await expect(page.getByText('Ready to release')).toBeVisible({
    timeout: 30_000,
  });
  await screenshot(page, manifest, quiz.key, `05-${personaKey}-instructor-reviewed.png`);
}

async function releaseAsInstructor(page, manifest, quiz, personaKey) {
  const urls = manifest.urls.quizzes[quiz.key];
  await refreshPersonaPage(page, manifest, 'instructor');
  await page.goto(urls.speedGraders[personaKey], {
    waitUntil: 'domcontentloaded',
  });
  await expect(page.getByTestId('release-result')).toBeVisible({
    timeout: 60_000,
  });
  await page.getByTestId('release-result').click();
  await expect(page.getByText('Released to learner')).toBeVisible({
    timeout: 30_000,
  });
  await screenshot(page, manifest, quiz.key, `06-${personaKey}-released.png`);
}

async function verifyLearnerResult(page, manifest, quiz) {
  await page.goto(manifest.urls.learnerGrades, {
    waitUntil: 'domcontentloaded',
  });
  const gradeHeading = page.getByRole('heading', {
    name: `[${manifest.marker}] ${quiz.title}`,
    exact: true,
  });
  await expect(gradeHeading).toBeVisible({ timeout: 60_000 });
  const gradeRow = gradeHeading.locator('xpath=ancestor::article');
  await expect(gradeRow.getByText(`${expectedFinalScore(quiz) / 100} / ${quiz.expected.maxScore / 100}`, { exact: true })).toBeVisible();
  await screenshot(page, manifest, quiz.key, '07-learner-gradebook.png');

  await page.goto(manifest.urls.quizzes[quiz.key].learnerActivity, {
    waitUntil: 'domcontentloaded',
  });
  const validationCases = quizBrowserCases(quiz);
  for (const [index, validationCase] of validationCases.entries()) {
    await expectQuestionVisible(page, validationCase);
    if (index < validationCases.length - 1) {
      await page.getByRole('button', { name: 'Next', exact: true }).click();
    }
  }
  await expect(page.getByRole('button', { name: 'Start Activity' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Submit Quiz' })).toBeDisabled();
  await screenshot(page, manifest, quiz.key, '08-learner-restored-result.png');
}

async function synchronizeSubmission(manifest, quiz, personaKey, { wait = true } = {}) {
  const resources = manifest.resources.quizzes[quiz.key];
  const instructor = await authenticatePersona(manifest, 'instructor', {
    create: false,
  });
  const deadline = Date.now() + 30_000;
  let submissionId = resources.submissions[personaKey].submissionId;
  while (!submissionId) {
    const queue = await readGradingQueue(instructor, resources.assessmentId);
    submissionId = queue.items?.find((item) => item.userId === manifest.personas[personaKey].userId || item.isGroup)?.submissionId;
    if (submissionId) break;
    if (!wait) return false;
    if (Date.now() >= deadline) {
      throw new Error(`The ${personaKey} attempt for "${quiz.key}" did not reach the grading queue.`);
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  let submission = await request(instructor.client, 'Synchronize runtime submission', {
    method: 'GET',
    path: `/v1/assessments/runtime-submissions/${submissionId}`,
  });
  while (wait && hasAIReview(quiz) && !hasSettledAIReview(submission, hasInstructorReview(quiz))) {
    if (Date.now() >= deadline) {
      throw new Error(`The AI review for "${quiz.key}" did not settle within 30 seconds.`);
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
    submission = await request(instructor.client, 'Wait for AI review submission', {
      method: 'GET',
      path: `/v1/assessments/runtime-submissions/${submissionId}`,
    });
  }
  const target = resources.submissions[personaKey];
  target.submissionId = submission.submissionId;
  target.gradingExecutionId = submission.execution?.executionId ?? null;
  target.gradeResultId = submission.execution?.activeRoundId ?? null;
  manifest.urls = buildScenarioUrls(manifest);
  return true;
}

function hasSettledAIReview(submission, requiresInstructor) {
  const execution = submission.execution;
  if (!execution) return false;
  return (requiresInstructor && execution.requiresInstructorReview === true) || execution.instructorVisibleResult?.state === 'final';
}

async function refreshPersonaPage(page, manifest, personaKey) {
  const persona = manifest.personas[personaKey];
  await browserSignIn(page, manifest.environment.webBaseUrl, persona.email, persona.password);
}

function advanceQuizCheckpoint(manifest, quizKey, checkpoint) {
  const resources = manifest.resources.quizzes[quizKey];
  if (checkpointIndex(checkpoint) > checkpointIndex(resources.checkpoint)) {
    resources.checkpoint = checkpoint;
  }
  recomputeManifestCheckpoint(manifest);
}

async function persist(manifest) {
  manifest.urls = buildScenarioUrls(manifest);
  await writeManifest(manifest);
}

async function screenshot(page, manifest, quizKey, name) {
  const caseId = getValidationCase(quizKey).id;
  const directory = `${validationEvidenceDirectory(manifest.runId, caseId)}/screenshots`;
  await mkdir(directory, { recursive: true, mode: 0o700 });
  await page.screenshot({ path: `${directory}/${name}`, fullPage: true });
}

function readGradingQueue(session, assessmentId) {
  return request(session.client, 'Read grading queue', {
    method: 'GET',
    path: `/v1/assessments/${assessmentId}/grading-queue`,
  });
}

function quizBrowserCases(quiz) {
  return quiz.document.order.map(([itemId]) => {
    const entry = quiz.document.blocks[itemId];
    const fillBlankInputType = entry?.blanks?.[0]?.input?.type ?? null;
    const source = QUESTION_TYPE_CASES.find(
      (candidate) =>
        candidate.fixture.quizEntryType === entry?.type &&
        (candidate.fixture.fillBlankInputType == null || candidate.fixture.fillBlankInputType === fillBlankInputType),
    );
    const answer = quiz.learnerResponse.payload.answers[itemId];
    if (!source || !entry || !answer) {
      throw new Error(`Quiz "${quiz.key}" item "${itemId}" has no browser driver fixture or representative answer.`);
    }
    return {
      ...source,
      id: `${getValidationCase(quiz.key).id}-${itemId}`,
      fixture: {
        ...source.fixture,
        entry,
        answers: { ...source.fixture.answers, correct: answer },
      },
    };
  });
}

function reviewItemScores(quiz, totalScore) {
  let remaining = totalScore;
  const scores = quiz.document.order.map(([itemId]) => {
    const maximum = quiz.document.blocks[itemId]?.points ?? 100;
    const score = Math.max(0, Math.min(maximum, remaining));
    remaining -= score;
    return [itemId, score];
  });
  if (remaining !== 0) {
    throw new Error(`Review score ${totalScore} cannot be distributed across quiz "${quiz.key}".`);
  }
  return scores;
}
