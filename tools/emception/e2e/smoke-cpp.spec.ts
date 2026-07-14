import { expect, test } from '@playwright/test';

test('boots the browser toolchain and compiles and runs C++', async ({ page }) => {
    const browserErrors: string[] = [];
    page.on('pageerror', (error) => browserErrors.push(error.message));
    page.on('console', (message) => {
        if (message.type() === 'error') browserErrors.push(message.text());
    });

    await page.goto('/', { waitUntil: 'domcontentloaded' });

    const runButton = page.getByRole('button', { name: 'Compile & run' });
    await expect(runButton).toBeEnabled({ timeout: 120_000 });
    await runButton.click();

    const output = page.locator('pre.output');
    await expect(output).toContainText('[compile]', { timeout: 120_000 });
    await expect(output).toContainText('[link]', { timeout: 120_000 });
    await expect(output).toContainText('[run]', { timeout: 120_000 });
    await expect(output).toContainText('got 42', { timeout: 120_000 });
    await expect(output).toContainText('[exit] phase=run code=0', { timeout: 120_000 });

    expect(browserErrors).toEqual([]);
});
