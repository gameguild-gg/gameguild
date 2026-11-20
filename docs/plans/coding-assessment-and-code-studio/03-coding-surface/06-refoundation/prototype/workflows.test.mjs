import assert from 'node:assert/strict';
import { join } from 'node:path';
import test, { after, before } from 'node:test';
import { entryUrl, openHarness, outputDirectory, selectCases } from './harness.mjs';

let harness;
before(async () => {
  harness = await openHarness();
});
after(async () => {
  await harness?.close();
  if (harness) assert.equal(harness.browser.isConnected(), false);
});

const cases = [
  {
    id: 'UXP-01-entry-lifecycle',
    slice: 'UXP-01',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByRole('tab', { name: 'Problem', exact: true }).click();
      assert.equal(await page.getByLabel('Exercise title', { exact: true }).inputValue(), '');
      assert.equal(await page.getByLabel('Statement', { exact: true }).inputValue(), '');
      assert.match(await page.getByRole('status').first().innerText(), /Unsaved/);
      await page.getByRole('tab', { name: 'Problem', exact: true }).focus();
      await page.keyboard.press('End');
      assert.equal(await page.getByRole('tab', { name: 'Validate exercise', exact: true }).getAttribute('aria-selected'), 'true');
      assert.equal(await page.getByRole('button', { name: 'Run reference', exact: true }).isEnabled(), false);
    },
  },
  {
    id: 'UXP-02-blank-function-typed-cases',
    slice: 'UXP-02',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Exercise title', { exact: true }).fill('Choose the greater value');
      await page.getByLabel('Statement', { exact: true }).fill('Return the greater integer.');
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      await page.getByLabel('Function name', { exact: true }).fill('chooseGreater');
      await page.getByRole('button', { name: 'Add parameter', exact: true }).click();
      await page.getByLabel('Parameter 1 name').fill('left');
      await page.getByRole('button', { name: 'Add parameter', exact: true }).click();
      await page.getByLabel('Parameter 2 name').fill('right');
      await page.getByRole('button', { name: 'Apply signature', exact: true }).click();
      await page.getByRole('button', { name: 'Generate starter', exact: true }).click();
      assert.match(await page.getByLabel('Source code', { exact: true }).inputValue(), /int chooseGreater\(int left, int right\)/);
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      await page.getByRole('button', { name: 'Add case', exact: true }).click();
      await page.getByLabel('Case name', { exact: true }).fill('Right is greater');
      await page.getByLabel('left (integer)', { exact: true }).fill('2');
      await page.getByLabel('right (integer)', { exact: true }).fill('7');
      await page.getByLabel('Expected result (integer)', { exact: true }).fill('7');
      const firstId = await page.locator('[data-case][aria-pressed="true"]').getAttribute('data-case');
      await page.getByRole('button', { name: 'Duplicate case', exact: true }).click();
      assert.notEqual(await page.locator('[data-case][aria-pressed="true"]').getAttribute('data-case'), firstId);
      assert.equal(await page.getByLabel('left (integer)', { exact: true }).inputValue(), '2');
      await page.getByLabel('Case visibility', { exact: true }).selectOption('private');
      await page.getByLabel('Expected result (integer)', { exact: true }).fill('');
      assert.match(await page.locator('#case-detail').innerText(), /Expected result must be an integer/);
      await page.getByLabel('Expected result (integer)', { exact: true }).fill('7');
      await page.getByRole('tab', { name: 'Problem', exact: true }).click();
      assert.equal(await page.getByLabel('Exercise title', { exact: true }).inputValue(), 'Choose the greater value');
      assert.equal(await page.locator('[data-sample]').count(), 1);
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      assert.equal(await page.getByLabel('Case visibility', { exact: true }).inputValue(), 'private');
      await page.getByRole('button', { name: 'Delete case', exact: true }).click();
      assert.equal(await page.locator('[data-case]').count(), 1);
    },
  },
  {
    id: 'UXP-02-io-empty-output-and-environment',
    slice: 'UXP-02',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Exercise type', { exact: true }).selectOption('standard-io');
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      await page.getByRole('button', { name: 'Add case', exact: true }).click();
      await page.getByLabel('Standard input', { exact: true }).fill('first\nsecond');
      await page.getByLabel('Output expectation', { exact: true }).selectOption('none');
      assert.match(await page.locator('#case-detail').innerText(), /No output oracle/);
      await page.getByLabel('Output expectation', { exact: true }).selectOption('value');
      assert.equal(await page.getByLabel('Expected stdout', { exact: true }).inputValue(), '');
      assert.match(await page.locator('#case-detail').innerText(), /Intentional empty output/);
      await page.getByRole('tab', { name: 'Environment', exact: true }).click();
      await page.getByLabel('Language', { exact: true }).selectOption('c');
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      assert.equal(await page.getByLabel('Graphics output', { exact: true }).isEnabled(), false);
      await page.getByLabel('Language', { exact: true }).selectOption('cpp');
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      await page.getByLabel('Execution environment', { exact: true }).selectOption('cpp-sdl3');
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      assert.equal(await page.getByLabel('Graphics output', { exact: true }).isChecked(), true);
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      assert.equal(await page.getByLabel('Standard input', { exact: true }).inputValue(), 'first\nsecond');
      await page.getByRole('tab', { name: 'Problem', exact: true }).click();
      await page.getByLabel('Exercise type', { exact: true }).selectOption('function');
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      await page.getByRole('tab', { name: 'Environment', exact: true }).click();
      assert.equal(await page.getByLabel('Language', { exact: true }).locator('option[value="c"]').isDisabled(), true);
    },
  },
  {
    id: 'UXP-03-reference-wrong-and-stale',
    slice: 'UXP-03',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
      assert.match(await page.getByLabel('Reference solution', { exact: true }).inputValue(), /a > b/);
      await page.getByRole('button', { name: 'Run reference', exact: true }).click();
      await page.locator('#reference-result').getByText('All cases passed', { exact: true }).waitFor();
      await page.getByRole('button', { name: 'Run incorrect solution', exact: true }).click();
      await page.locator('#incorrect-result').getByText('Cases detected the defect', { exact: true }).waitFor();
      assert.equal(await page.locator('#incorrect-result [data-diagnostic="mismatch"]').count(), 3);
      const candidate = await page.locator('#reference-result [data-candidate]').getAttribute('data-candidate');
      await page.getByRole('tab', { name: 'Problem', exact: true }).click();
      await page.getByLabel('Statement', { exact: true }).fill('Return the greater value, including negative integers.');
      await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
      assert.match(await page.locator('#reference-result').innerText(), /Out of date/);
      assert.equal(await page.locator('#reference-result [data-candidate]').getAttribute('data-candidate'), candidate);
      assert.equal(await page.getByRole('tab', { name: 'Validate', exact: true }).count(), 0);
    },
  },
  {
    id: 'UXP-03-faithful-redacted-independent-preview',
    slice: 'UXP-03',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByLabel('Statement', { exact: true }).fill('New maximum problem <script>window.unwanted = true</script>');
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      const source = 'int maximum(int a, int b) {\n  // New authored starter\n  return a;\n}';
      await page.getByLabel('Source code', { exact: true }).fill(source);
      await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
      await page.getByRole('button', { name: 'Open learner preview', exact: true }).click();
      assert.match(await page.getByRole('region', { name: 'Learner problem', exact: true }).innerText(), /New maximum problem <script>/);
      assert.equal(await page.getByLabel('Your code', { exact: true }).inputValue(), source);
      const projection = await page.evaluate(() => window.codingPrototypeApp.state.preview.delivery);
      assert.equal(projection.samples.length, 2);
      assert.equal('protected' in projection, false);
      assert.doesNotMatch(JSON.stringify(projection), /PRIVATE-CANARY|a > b \? a : b/);
      assert.doesNotMatch(await page.locator('body').innerText(), /PRIVATE-CANARY/);
      assert.equal(await page.evaluate(() => window.unwanted), undefined);
      await page.getByLabel('Your code', { exact: true }).fill('Preview-only edits');
      await page.getByRole('button', { name: 'Return to authoring', exact: true }).click();
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      assert.equal(await page.getByLabel('Source code', { exact: true }).inputValue(), source);
      assert.doesNotMatch(await page.evaluate(() => JSON.stringify(localStorage)), /PRIVATE-CANARY/);
    },
  },
  {
    id: 'UXP-04-diagnose-correct-submit-frozen',
    slice: 'UXP-04',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      await page.getByLabel('Recorded source', { exact: true }).selectOption('incorrect');
      await page.getByRole('button', { name: 'Run tests', exact: true }).click();
      await page.locator('#learner-results').getByText('Wrong answer', { exact: true }).waitFor();
      assert.match(await page.locator('#learner-results').innerText(), /Expected\s+7/);
      assert.match(await page.locator('#learner-results').innerText(), /Actual\s+2/);
      await page.getByLabel('Recorded source', { exact: true }).selectOption('correct');
      assert.match(await page.locator('#learner-results').innerText(), /Out of date/);
      await page.getByRole('button', { name: 'Run tests', exact: true }).click();
      await page.locator('#learner-results').getByText('All sample cases passed', { exact: true }).waitFor();
      const source = await page.getByLabel('Your code', { exact: true }).inputValue();
      await page.getByRole('button', { name: 'Submit solution', exact: true }).click();
      assert.match(await page.locator('#submission-status').innerText(), /Submission acknowledged/);
      const first = await page.evaluate(() => structuredClone(window.codingPrototypeApp.state.learner.submission));
      await page.getByRole('button', { name: 'Submit solution', exact: true }).click();
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.submission.id), first.id);
      await page.getByLabel('Your code', { exact: true }).fill(`${source}\n// Later work`);
      assert.match(await page.locator('#submission-status').innerText(), /Unsubmitted changes/);
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.submission.files[0].content), source);
      await page.getByLabel('Host feedback example', { exact: true }).selectOption('released');
      assert.match(await page.locator('#submission-status').innerText(), /Released feedback/);
      await page.getByLabel('Simulation mode', { exact: true }).selectOption('executor');
      await page.getByRole('button', { name: 'Run tests', exact: true }).click();
      await page.locator('#learner-results').getByText('Execution service unavailable. No correctness verdict.', { exact: true }).waitFor();
      assert.match(await page.locator('#submission-status').innerText(), /Released feedback/);
    },
  },
  {
    id: 'UXP-04-custom-compile-cancel-stale',
    slice: 'UXP-04',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('echo');
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      await page.getByLabel('Recorded source', { exact: true }).selectOption('correct');
      await page.getByRole('button', { name: 'Cases', exact: true }).click();
      await page.getByLabel('Custom standard input', { exact: true }).fill('hello');
      await page.getByRole('button', { name: 'Run custom case', exact: true }).click();
      await page.locator('#learner-results').getByText('Output observed', { exact: true }).waitFor();
      assert.equal(await page.locator('#learner-results').getByText('Expected', { exact: true }).count(), 0);
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.delivery.samples.length), 2);
      await page.getByLabel('Simulation mode', { exact: true }).selectOption('compile');
      await page.getByRole('button', { name: 'Run tests', exact: true }).click();
      await page
        .locator('#learner-results')
        .getByText(/Compilation failed:/)
        .waitFor();
      await page.getByLabel('Simulation mode', { exact: true }).selectOption('slow');
      await page.getByRole('button', { name: 'Run tests', exact: true }).click();
      await page.getByRole('button', { name: 'Cancel run', exact: true }).click();
      assert.match(await page.locator('#learner-results').innerText(), /Cancelled/);
      await page.getByRole('button', { name: 'Run tests', exact: true }).click();
      await page.getByLabel('Your code', { exact: true }).fill('Edited while running');
      await page.waitForFunction(() => window.codingPrototypeApp.state.learner.run.status === 'stale');
      await page.waitForTimeout(1600);
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.run.status), 'stale');
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.submission), null);
    },
  },
  {
    id: 'UXP-05-save-conflict-recovery-and-lossless-cancel',
    slice: 'UXP-05',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      const before = await page.evaluate(() => structuredClone(window.codingPrototypeApp.state.draft));
      await page.getByLabel('Exercise type', { exact: true }).selectOption('standard-io');
      await page.getByRole('button', { name: 'Cancel change', exact: true }).click();
      assert.deepEqual(await page.evaluate(() => window.codingPrototypeApp.state.draft), before);
      await page.getByLabel('Save response', { exact: true }).selectOption('failed');
      await page.getByRole('button', { name: 'Save draft', exact: true }).click();
      await page.getByRole('status').filter({ hasText: 'Save failed' }).waitFor();
      assert.doesNotMatch(await page.getByRole('status').first().innerText(), /Saved/);
      await page.getByLabel('Save response', { exact: true }).selectOption('conflict');
      await page.getByRole('button', { name: 'Save draft', exact: true }).click();
      await page.getByRole('status').filter({ hasText: 'Version conflict' }).waitFor();
      await page.getByLabel('Statement', { exact: true }).fill('Recover this edited statement.');
      await page.getByRole('button', { name: 'Simulate reload', exact: true }).click();
      await page.getByRole('button', { name: 'Recover local draft', exact: true }).click();
      assert.equal(await page.getByLabel('Statement', { exact: true }).inputValue(), 'Recover this edited statement.');
      assert.deepEqual(await page.evaluate(() => window.codingPrototypeApp.state.draft.cases), before.cases);
      await page.getByLabel('Save response', { exact: true }).selectOption('normal');
      await page.getByRole('button', { name: 'Save draft', exact: true }).click();
      await page.getByRole('status').filter({ hasText: 'Saved (simulation)' }).waitFor();
      await page.getByRole('button', { name: 'Reopen saved draft', exact: true }).click();
      assert.equal(await page.getByLabel('Statement', { exact: true }).inputValue(), 'Recover this edited statement.');
      assert.doesNotMatch(await page.evaluate(() => JSON.stringify(localStorage)), /PRIVATE-CANARY/);
    },
  },
  {
    id: 'UXP-05-host-layout-keyboard-and-mobile',
    slice: 'UXP-05',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('stress');
      assert.equal(await page.getByLabel('Design example', { exact: true }).inputValue(), 'stress');
      await page.getByLabel('Curriculum panel', { exact: true }).check();
      await page.getByLabel('Settings panel', { exact: true }).check();
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      assert.equal(await page.locator('[data-case]').count(), 20);
      await page.locator('[data-case]').last().click();
      const list = await page.locator('.case-list').boundingBox();
      const active = await page.locator('[data-case][aria-pressed="true"]').boundingBox();
      assert.ok(list && active && active.x >= list.x && active.x + active.width <= list.x + list.width, 'The selected case stays visible in the navigation.');
      await page.getByLabel('Case name', { exact: true }).fill('Keep selected case');
      await page.getByLabel('Settings panel', { exact: true }).uncheck();
      await page.getByLabel('Settings panel', { exact: true }).check();
      assert.equal(await page.getByLabel('Case name', { exact: true }).inputValue(), 'Keep selected case');
      const width = await page.locator('main.content').evaluate((element) => element.getBoundingClientRect().width);
      assert.equal(width, 704);
      await page.getByRole('tab', { name: 'Problem', exact: true }).focus();
      await page.keyboard.press('End');
      assert.equal(await page.getByRole('tab', { name: 'Validate exercise', exact: true }).evaluate((element) => element === document.activeElement), true);
      await page.getByRole('button', { name: 'Open learner preview', exact: true }).click();
      await page.getByRole('button', { name: 'Results', exact: true }).click();
      await assertInViewport(page.getByRole('button', { name: 'Run tests', exact: true }), 1280, 720);
      await page.setViewportSize({ width: 390, height: 844 });
      await page.getByRole('button', { name: 'Code', exact: true }).click();
      await page.getByLabel('Your code', { exact: true }).fill('Narrow-layout edit');
      await page.getByRole('button', { name: 'Results', exact: true }).click();
      await page.getByRole('button', { name: 'Code', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).inputValue(), 'Narrow-layout edit');
      await assertInViewport(page.getByRole('button', { name: 'Run tests', exact: true }), 390, 844);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
      await page.getByRole('button', { name: 'Return to authoring', exact: true }).click();
      for (const [width, height] of [
        [1600, 1000],
        [1280, 720],
        [390, 844],
      ]) {
        await page.setViewportSize({ width, height });
        await page.getByRole('tab', { name: 'Tests', exact: true }).click();
        if (width >= 1000) {
          const list = await page.locator('.case-list').boundingBox();
          const active = await page.locator('[data-case][aria-pressed="true"]').boundingBox();
          assert.ok(
            list && active && active.y >= list.y && active.y + active.height <= list.y + list.height,
            'Selection remains visible after viewport/task changes.',
          );
        }
        await page.screenshot({ path: join(outputDirectory, `uxp05-teacher-${width}.png`) });
        await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
        await page.getByRole('button', { name: 'Open learner preview', exact: true }).click();
        await page.getByRole('button', { name: 'Code', exact: true }).click();
        await page.screenshot({ path: join(outputDirectory, `uxp05-learner-${width}.png`) });
        await page.getByRole('button', { name: 'Return to authoring', exact: true }).click();
      }
    },
  },
  {
    id: 'UXP-06-scalar-types-and-capability-guards',
    slice: 'UXP-06',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      await page.getByLabel('Function name', { exact: true }).fill('isPositive');
      await page.getByLabel('Return type', { exact: true }).selectOption('boolean');
      await page.getByRole('button', { name: 'Add parameter', exact: true }).click();
      await page.getByLabel('Parameter 1 name').fill('value');
      await page.getByLabel('Parameter 1 type').selectOption('float');
      await page.getByRole('button', { name: 'Apply signature', exact: true }).click();
      await page.getByRole('button', { name: 'Generate starter', exact: true }).click();
      assert.match(await page.getByLabel('Source code', { exact: true }).inputValue(), /bool isPositive\(double value\)/);
      await page.getByLabel('Function name', { exact: true }).fill('pendingName');
      assert.equal(await page.getByRole('button', { name: 'Generate starter', exact: true }).isEnabled(), false);
      await page.getByLabel('Function name', { exact: true }).fill('isPositive');
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      await page.getByRole('button', { name: 'Add case', exact: true }).click();
      await page.getByLabel('value (float)', { exact: true }).fill('-0.25');
      await page.getByLabel('Expected result (boolean)', { exact: true }).selectOption('false');
      assert.equal(await page.locator('#case-detail .errors li').count(), 0);
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      const error = await page.evaluate(() => {
        try {
          window.codingPrototypeApp.dispatch({ type: 'select-region', region: 'graphics' });
          return null;
        } catch (error) {
          return String(error);
        }
      });
      assert.match(error ?? '', /unavailable/i);
    },
  },
  {
    id: 'UXP-06-modal-reset-and-save-in-flight',
    slice: 'UXP-06',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByLabel('Exercise type', { exact: true }).selectOption('standard-io');
      await page.keyboard.press('Tab');
      assert.equal(await page.getByRole('dialog').evaluate((element) => element.contains(document.activeElement)), true);
      await page.keyboard.press('Escape');
      assert.equal(await page.getByLabel('Exercise type', { exact: true }).evaluate((element) => element === document.activeElement), true);
      await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
      await page.getByRole('button', { name: 'Open learner preview', exact: true }).click();
      assert.equal(await page.getByRole('button', { name: 'Submit solution', exact: true }).isDisabled(), true);
      await page.getByRole('button', { name: 'Return to authoring', exact: true }).click();
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      await page.getByLabel('Recorded source', { exact: true }).selectOption('correct');
      const edited = await page.getByLabel('Your code', { exact: true }).inputValue();
      await page.getByRole('button', { name: 'Submit solution', exact: true }).click();
      const submissionId = await page.evaluate(() => window.codingPrototypeApp.state.learner.submission.id);
      await page.getByRole('button', { name: 'Cases', exact: true }).click();
      await page.getByLabel('Custom a (integer)', { exact: true }).fill('2');
      assert.doesNotMatch(await page.locator('#submission-status').innerText(), /Unsubmitted changes/);
      await page.getByRole('button', { name: 'Submit solution', exact: true }).click();
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.submission.id), submissionId);
      await page.getByRole('button', { name: 'Reset code', exact: true }).click();
      await page.getByRole('button', { name: 'Cancel change', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).inputValue(), edited);
      await page.getByRole('button', { name: 'Reset code', exact: true }).click();
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      assert.notEqual(await page.getByLabel('Your code', { exact: true }).inputValue(), edited);
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.submission.files[0].content), edited);
      await page.getByRole('button', { name: 'Return to authoring', exact: true }).click();
      await page.evaluate(() => {
        window.codingPrototypeApp.dispatch({ type: 'save' });
        const draft = structuredClone(window.codingPrototypeApp.state.draft);
        draft.title = 'New work while saving';
        window.codingPrototypeApp.dispatch({ type: 'edit-draft', draft });
      });
      await page.waitForFunction(() => !document.querySelector('[data-action="reopen"]').disabled);
      assert.match(await page.getByRole('status').first().innerText(), /Unsaved/);
      await page.getByRole('button', { name: 'Reopen saved draft', exact: true }).click();
      await page.getByRole('tab', { name: 'Problem', exact: true }).click();
      assert.equal(await page.getByLabel('Exercise title', { exact: true }).inputValue(), 'Maximum of two integers');
    },
  },
  {
    id: 'UXP-06-capability-specimens-and-captures',
    slice: 'UXP-06',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('echo');
      await page.getByRole('tab', { name: 'Environment', exact: true }).click();
      await page.getByLabel('Execution environment', { exact: true }).selectOption('cpp-sdl3');
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      await page.getByLabel('Interactive terminal', { exact: true }).check();
      await page.getByLabel('Curriculum panel', { exact: true }).check();
      await page.getByLabel('Settings panel', { exact: true }).check();
      await page.getByRole('tab', { name: 'Tests', exact: true }).click();
      await page.screenshot({ path: join(outputDirectory, 'uxp06-teacher-io.png') });
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      await page.getByRole('button', { name: 'Canvas', exact: true }).click();
      const before = await page.locator('#graphics-canvas').evaluate((canvas) => canvas.toDataURL());
      const pixel = await page.locator('#graphics-canvas').evaluate((canvas) => Array.from(canvas.getContext('2d').getImageData(50, 120, 1, 1).data));
      assert.deepEqual(pixel, [103, 198, 164, 255]);
      await page.getByRole('button', { name: 'Move shape', exact: true }).click();
      assert.notEqual(await page.locator('#graphics-canvas').evaluate((canvas) => canvas.toDataURL()), before);
      await page.getByRole('button', { name: 'Stop canvas', exact: true }).click();
      assert.equal(await page.getByRole('button', { name: 'Move shape', exact: true }).isEnabled(), false);
      await page.getByRole('button', { name: 'Reopen canvas', exact: true }).click();
      await page.getByRole('button', { name: 'Terminal', exact: true }).click();
      await page.getByLabel('Terminal input', { exact: true }).fill('separate interactive input');
      await page.getByRole('button', { name: 'Send input', exact: true }).click();
      assert.match(await page.getByLabel('Terminal transcript', { exact: true }).innerText(), /separate interactive input/);
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.scratch), '');
      for (const [width, height] of [
        [1600, 1000],
        [1280, 720],
        [390, 844],
      ]) {
        await page.setViewportSize({ width, height });
        if ((await page.getByRole('button', { name: 'Study controls', exact: true }).getAttribute('aria-expanded')) === 'true')
          await page.getByRole('button', { name: 'Study controls', exact: true }).click();
        await page.getByRole('button', { name: 'Canvas', exact: true }).click();
        await page.getByRole('button', { name: 'Focus canvas', exact: true }).click();
        await assertInViewport(page.getByRole('button', { name: 'Run tests', exact: true }), width, height);
        await page.screenshot({ path: join(outputDirectory, `uxp06-canvas-${width}.png`) });
        const ids = await page.locator('[id]').evaluateAll((elements) => elements.map((element) => element.id));
        assert.equal(new Set(ids).size, ids.length);
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
      }
    },
  },
  {
    id: 'UXP-06-immutable-support-and-command-port',
    slice: 'UXP-06',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      await page.getByRole('button', { name: 'New file', exact: true }).click();
      await page.getByLabel('File path', { exact: true }).fill('support.txt');
      await page.getByRole('button', { name: 'Create file', exact: true }).click();
      await page.getByLabel('Source code', { exact: true }).fill('Supplied immutable support');
      await page.getByLabel('Learner can edit this file', { exact: true }).uncheck();
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      await page.getByRole('button', { name: 'Files', exact: true }).click();
      await page.getByRole('button', { name: 'support.txt', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).evaluate((element) => element.readOnly), true);
      const error = await page.evaluate(() => {
        try {
          window.codingPrototypeApp.dispatch({ type: 'edit-source', source: 'attempted overwrite' });
          return null;
        } catch (error) {
          return String(error);
        }
      });
      assert.match(error ?? '', /read.only/i);
      assert.equal(await page.getByLabel('Your code', { exact: true }).inputValue(), 'Supplied immutable support');
    },
  },
  {
    id: 'UXP-07-author-file-workbench',
    slice: 'UXP-07',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      await page.getByRole('tab', { name: 'solution.cpp', exact: true }).waitFor();
      await page.getByRole('button', { name: 'Files', exact: true }).click();
      await page.getByRole('button', { name: 'New file', exact: true }).click();
      await page.getByLabel('File path', { exact: true }).fill('solution.cpp');
      await page.getByRole('button', { name: 'Create file', exact: true }).click();
      assert.match(await page.getByRole('dialog').innerText(), /already exists/i);
      await page.getByLabel('File path', { exact: true }).fill('../outside.cpp');
      await page.getByRole('button', { name: 'Create file', exact: true }).click();
      assert.match(await page.getByRole('dialog').innerText(), /relative|workspace/i);
      await page.getByLabel('File path', { exact: true }).fill('include/helper.hpp');
      await page.getByRole('button', { name: 'Create file', exact: true }).click();
      const source = '#pragma once\n// Keep my helper\nconstexpr int offset = 2;\n';
      await page.getByLabel('Source code', { exact: true }).fill(source);
      assert.equal(await page.getByRole('tab', { name: 'include/helper.hpp', exact: true }).getAttribute('aria-selected'), 'true');
      assert.equal(await page.locator('.editor-tab.is-modified').count(), 1);
      await page.getByRole('button', { name: 'Rename file', exact: true }).click();
      await page.getByLabel('File path', { exact: true }).fill('include/offset.hpp');
      await page.getByRole('button', { name: 'Rename', exact: true }).click();
      await page.getByRole('button', { name: 'Close include/offset.hpp', exact: true }).click();
      assert.equal(await page.getByRole('tab', { name: 'include/offset.hpp', exact: true }).count(), 0);
      await page.getByRole('button', { name: 'include/offset.hpp', exact: true }).click();
      assert.equal(await page.getByLabel('Source code', { exact: true }).inputValue(), source);
      await page.getByRole('button', { name: 'Save draft', exact: true }).click();
      await page.getByRole('status').filter({ hasText: 'Saved (simulation)' }).waitFor();
      assert.equal(await page.locator('.editor-tab.is-modified').count(), 0);
      await page.getByRole('button', { name: 'Delete file', exact: true }).click();
      await page.getByRole('button', { name: 'Cancel change', exact: true }).click();
      assert.equal(await page.getByLabel('Source code', { exact: true }).inputValue(), source);
      await page.getByRole('button', { name: 'Delete file', exact: true }).click();
      await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
      assert.equal(await page.getByRole('button', { name: 'include/offset.hpp', exact: true }).count(), 0);
      assert.equal(await page.getByRole('tab', { name: 'solution.cpp', exact: true }).getAttribute('aria-selected'), 'true');
    },
  },
  {
    id: 'UXP-07-learner-buffers-and-readonly',
    slice: 'UXP-07',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByRole('button', { name: 'Learner', exact: true }).click();
      await page.getByRole('tab', { name: 'solution.cpp', exact: true }).waitFor();
      const source = Array.from({ length: 40 }, (_, i) => `// line ${i}: ${'code '.repeat(30)}`).join('\n');
      await page.getByLabel('Your code', { exact: true }).fill(source);
      const position = await page.getByLabel('Your code', { exact: true }).evaluate((input) => {
        input.setSelectionRange(45, 45);
        input.scrollTop = 160;
        input.scrollLeft = 90;
        return { top: input.scrollTop, left: input.scrollLeft };
      });
      await page.getByRole('button', { name: 'Files', exact: true }).click();
      await page.getByRole('button', { name: 'include/limits.hpp', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).evaluate((input) => input.readOnly), true);
      assert.match(await page.locator('.editor-status').innerText(), /Read-only/);
      assert.equal(await page.getByRole('button', { name: 'New file', exact: true }).count(), 0);
      await page.getByRole('tab', { name: 'solution.cpp', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).inputValue(), source);
      const restored = await page
        .getByLabel('Your code', { exact: true })
        .evaluate((input) => ({ start: input.selectionStart, top: input.scrollTop, left: input.scrollLeft }));
      assert.equal(restored.start, 45);
      assert.equal(restored.top, position.top);
      assert.equal(restored.left, position.left);
      await page.getByRole('button', { name: 'Close solution.cpp', exact: true }).click();
      if ((await page.getByRole('button', { name: 'Files', exact: true }).getAttribute('aria-pressed')) === 'false')
        await page.getByRole('button', { name: 'Files', exact: true }).click();
      await page.getByRole('button', { name: 'solution.cpp', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).inputValue(), source);
      assert.equal(await page.locator('.editor-tab.is-modified').count(), 1);
      assert.doesNotMatch(await page.locator('.learner-code').innerText(), /PRIVATE-CANARY|Reference solution/);
      const error = await page.evaluate(() => {
        try {
          window.codingPrototypeApp.dispatch({ type: 'open-file', fileId: 'missing' });
          return null;
        } catch (error) {
          return String(error);
        }
      });
      assert.match(error ?? '', /unavailable|unknown/i);
      const count = await page.evaluate(() => window.codingPrototypeApp.state.learner.files.length);
      await page.getByRole('button', { name: 'Close solution.cpp', exact: true }).click();
      await page.getByRole('button', { name: 'Close include/limits.hpp', exact: true }).click();
      assert.equal(await page.getByLabel('Your code', { exact: true }).count(), 0);
      assert.match(await page.locator('#source-buffer').innerText(), /No file open/);
      assert.equal(await page.evaluate(() => window.codingPrototypeApp.state.learner.files.length), count);
      assert.equal(await page.getByRole('button', { name: 'Files', exact: true }).evaluate((button) => button === document.activeElement), true);
    },
  },
  {
    id: 'UXP-07-editor-responsive-and-keyboard',
    slice: 'UXP-07',
    async run(page) {
      await page.goto(entryUrl);
      await page.getByLabel('Design example', { exact: true }).selectOption('maximum');
      await page.getByLabel('Curriculum panel', { exact: true }).check();
      await page.getByLabel('Settings panel', { exact: true }).check();
      await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
      await page.getByRole('button', { name: 'Files', exact: true }).click();
      await page.getByRole('button', { name: 'include/limits.hpp', exact: true }).click();
      await page.getByRole('tab', { name: 'solution.cpp', exact: true }).focus();
      await page.keyboard.press('ArrowRight');
      assert.equal(await page.getByRole('tab', { name: 'include/limits.hpp', exact: true }).getAttribute('aria-selected'), 'true');
      await page.getByRole('tab', { name: 'solution.cpp', exact: true }).click();
      await page.getByLabel('Source code', { exact: true }).fill('int maximum(int a, int b) {\n  return 0;\n}');
      await page.getByLabel('Source code', { exact: true }).evaluate((input) => {
        input.setSelectionRange(29, 29);
      });
      await page.keyboard.press('Tab');
      assert.match(await page.getByLabel('Source code', { exact: true }).inputValue(), /\n {4}return/);
      assert.ok((await page.locator('.syntax-keyword').count()) > 0);
      assert.ok(await page.locator('.line-numbers').innerText());
      await page.emulateMedia({ forcedColors: 'active' });
      assert.notEqual(await page.getByLabel('Source code', { exact: true }).evaluate((input) => getComputedStyle(input).color), 'rgba(0, 0, 0, 0)');
      assert.equal(await page.locator('.syntax-layer').evaluate((layer) => getComputedStyle(layer).visibility), 'hidden');
      await page.emulateMedia({ forcedColors: 'none' });
      await page.getByLabel('Source code', { exact: true }).press('Escape');
      await page.keyboard.press('Tab');
      assert.equal(await page.getByLabel('Source code', { exact: true }).evaluate((input) => input === document.activeElement), false);
      for (const [width, height] of [
        [1600, 1000],
        [1280, 720],
        [390, 844],
      ]) {
        await page.setViewportSize({ width, height });
        await page.getByRole('tab', { name: 'Starter code', exact: true }).click();
        await page.locator('.code-workbench').scrollIntoViewIfNeeded();
        if (width === 1600 && (await page.getByRole('button', { name: 'Files', exact: true }).getAttribute('aria-pressed')) === 'false')
          await page.getByRole('button', { name: 'Files', exact: true }).click();
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        await page.screenshot({ path: join(outputDirectory, `uxp07-author-editor-${width}.png`) });
        await page
          .getByRole('button', { name: 'Learner', exact: true })
          .isVisible()
          .then(async (visible) => {
            if (!visible) await page.getByRole('button', { name: 'Study controls', exact: true }).click();
          });
        await page.getByRole('button', { name: 'Learner', exact: true }).click();
        await page.getByRole('button', { name: 'Code', exact: true }).click();
        if (width === 390) {
          const editor = await page.locator('.code-editor').boundingBox();
          assert.ok(editor && editor.height >= 120, 'Mobile editing retains a usable code area.');
        }
        await assertInViewport(page.getByRole('button', { name: 'Run tests', exact: true }), width, height);
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        await page.screenshot({ path: join(outputDirectory, `uxp07-learner-editor-${width}.png`) });
        await page.getByRole('button', { name: 'Return to authoring', exact: true }).click();
      }
    },
  },
];

async function assertInViewport(locator, width, height) {
  const box = await locator.boundingBox();
  assert.ok(box && box.x >= 0 && box.y >= 0 && box.x + box.width <= width && box.y + box.height <= height, 'Active command must be reachable in the viewport.');
}

for (const entry of selectCases(cases)) {
  test(entry.id, { timeout: 30_000 }, async () => {
    const page = await harness.browser.newPage({ viewport: { width: 1280, height: 720 } });
    page.setDefaultTimeout(2500);
    const errors = [];
    const network = [];
    const workers = [];
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('request', (request) => {
      if (/^https?:/.test(request.url())) network.push(request.url());
    });
    page.on('worker', (worker) => workers.push(worker.url()));
    try {
      await entry.run(page);
      assert.deepEqual(errors, []);
      assert.deepEqual(network, [], 'Prototype must not contact application/runtime services.');
      assert.deepEqual(workers, [], 'The design specimen starts no compiler worker.');
    } catch (error) {
      await page.screenshot({ path: join(outputDirectory, `${entry.id}-failed.png`) });
      throw error;
    } finally {
      await page.close();
      assert.equal(harness.browser.contexts().length, 0, 'Every page/context is disposed before the next case.');
    }
  });
}
