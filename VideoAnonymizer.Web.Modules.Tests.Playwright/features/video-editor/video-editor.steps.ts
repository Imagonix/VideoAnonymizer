import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

const { Given, Then } = createBdd();

Given('the editor is opened for an analyzed video', async ({ page }) => {
    await page.goto('/videoEditor');
});

Then('I should see a video preview', async ({ page }) => {
    await expect(page.locator('video')).toBeVisible();
});

Then('I should see a list of detected objects for the current frame next to the video preview', async ({ page }) => {
    await expect(page.getByTestId('object-list')).toBeVisible();
});

Then('every detected object in the current frame should have a checkbox', async ({ page }) => {
    const checkboxes = page.locator('[data-testid="object-list"] input[type="checkbox"]');
    await expect(checkboxes.first()).toBeVisible();
});

Then('I should see a timeline below the video preview', async ({ page }) => {
    await expect(page.getByTestId('timeline')).toBeVisible();
});

Then('I should see the detected objects listed on the left side of the timeline', async ({ page }) => {
    await expect(page.getByTestId('timeline-object-list')).toBeVisible();
});

Then('each object on the timeline should have a color', async ({ page }) => {
    const first = page.locator('[data-testid="timeline-object"]').first();
    await expect(first).toBeVisible();

    const color = await first.evaluate(el => getComputedStyle(el).backgroundColor);
    expect(color).not.toBe('');
});

Then('each object on the timeline should have a checkbox', async ({ page }) => {
    const checkboxes = page.locator('[data-testid="timeline-object"] input[type="checkbox"]');
    await expect(checkboxes.first()).toBeVisible();
});