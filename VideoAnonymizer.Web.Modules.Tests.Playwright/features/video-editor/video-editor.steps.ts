import { expect, Locator, Page } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

const { Given, When, Then } = createBdd();

Given('the editor is opened for an analyzed video', async ({ page }) => {
    await page.goto('/videoEditor');
    await setVideoCurrentTime(page, 0.25);
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

// --------------------
// helpers
// --------------------

function videoLocator(page: Page): Locator {
    return page.locator('video');
}

function overlayBoxes(page: Page): Locator {
    return page.locator('[data-testid="bounding-box"]');
}

function playbackIndicator(page: Page): Locator {
    return page.getByTestId('playback-indicator');
}

function timestampIndicator(page: Page): Locator {
    return page.getByTestId('playback-timestamp');
}

function currentFrameObjectCheckboxes(page: Page): Locator {
    return page.locator('[data-testid="object-list"] input[type="checkbox"]');
}

function timelineObjectCheckboxes(page: Page): Locator {
    return page.locator('[data-testid="timeline-object"] input[type="checkbox"]');
}

function timelineColoredSegments(page: Page): Locator {
    return page.locator('[data-testid="timeline-segment-colored"]');
}

function timelineGraySegments(page: Page): Locator {
    return page.locator('[data-testid="timeline-segment-gray"]');
}

async function getVideoCurrentTime(page: Page): Promise<number> {
    return await videoLocator(page).evaluate((el: HTMLVideoElement) => el.currentTime);
}

async function setVideoCurrentTime(page: Page, seconds: number): Promise<void> {
    await videoLocator(page).evaluate(
        (el: HTMLVideoElement, time: number) => {
            el.currentTime = time;
            el.dispatchEvent(new Event('timeupdate'));
            el.dispatchEvent(new Event('seeked'));
        },
        seconds
    );
}

async function pauseVideo(page: Page): Promise<void> {
    await videoLocator(page).evaluate((el: HTMLVideoElement) => el.pause());
}

async function playVideo(page: Page): Promise<void> {
    await videoLocator(page).evaluate(async (el: HTMLVideoElement) => {
        try {
            await el.play();
        } catch {
            // autoplay may be blocked in some environments
        }
    });
}

async function clickTimelineAtTime(page: Page, seconds: number): Promise<void> {
    const timeline = page.getByTestId('timeline');
    const box = await timeline.boundingBox();
    expect(box).not.toBeNull();

    const duration = await videoLocator(page).evaluate((el: HTMLVideoElement) => el.duration || 0);
    expect(duration).toBeGreaterThan(0);

    const ratio = Math.max(0, Math.min(1, seconds / duration));
    const x = box!.x + box!.width * ratio;
    const y = box!.y + box!.height / 2;

    await page.mouse.click(x, y);
}

async function getLocatorBackgroundColor(locator: Locator): Promise<string> {
    return await locator.evaluate(el => getComputedStyle(el).backgroundColor);
}

async function getLocatorBorderColor(locator: Locator): Promise<string> {
    return await locator.evaluate(el => getComputedStyle(el).borderColor);
}

async function getLocatorX(locator: Locator): Promise<number> {
    const box = await locator.boundingBox();
    expect(box).not.toBeNull();
    return box!.x;
}

// --------------------
// Timeline highlight
// --------------------

When('an object is detected in one or more analyzed frames', async ({ page }) => {
    await expect(page.locator('[data-testid="timeline-object"]').first()).toBeVisible();
});

Then('the timeline should highlight that object in its assigned color at the corresponding times', async ({ page }) => {
    const colored = timelineColoredSegments(page).first();
    await expect(colored).toBeVisible();

    const color = await getLocatorBackgroundColor(colored);
    expect(color).not.toBe('');
    expect(color).not.toBe('rgba(0, 0, 0, 0)');
});

// --------------------
// Continuous line
// --------------------

Given('an object is detected in neighboring analyzed frames', async ({ page }) => {
    await expect(page.locator('[data-testid="timeline-object"]').first()).toBeVisible();
});

When('the timeline is rendered', async ({ page }) => {
    await expect(page.getByTestId('timeline')).toBeVisible();
});

Then('the object should be shown as one continuous colored line across the corresponding time range', async ({ page }) => {
    const continuousLine = page.locator('[data-testid="timeline-segment-continuous"]').first();
    await expect(continuousLine).toBeVisible();
});

// --------------------
// Bounding box color consistency
// --------------------

When('an object is shown as a bounding box in the video preview', async ({ page }) => {
    await expect(overlayBoxes(page).first()).toBeVisible();
});

Then('the border of the bounding box should use the same color as the object on the timeline', async ({ page }) => {
    const box = overlayBoxes(page).first();
    const timelineObject = page.locator('[data-testid="timeline-object"]').first();

    await expect(box).toBeVisible();
    await expect(timelineObject).toBeVisible();

    const boxBorderColor = await getLocatorBorderColor(box);
    const timelineColor = await getLocatorBackgroundColor(timelineObject);

    expect(boxBorderColor).toBe(timelineColor);
});

// --------------------
// Current-frame object disable / enable
// --------------------

Given('the current frame contains a detected object', async ({ page }) => {
    await expect(currentFrameObjectCheckboxes(page).first()).toBeVisible();
});

When('I uncheck that object in the current-frame object list', async ({ page }) => {
    const checkbox = currentFrameObjectCheckboxes(page).first();
    await checkbox.uncheck();
});

Then('the object should no longer be active in this frame', async ({ page }) => {
    await expect(currentFrameObjectCheckboxes(page).first()).not.toBeChecked();
});

Then('the object should not be displayed in the overlay on this frame', async ({ page }) => {
    await expect(overlayBoxes(page).first()).toHaveCount(0);
});

Then('the point for the object in this frame should turn gray in the timeline', async ({ page }) => {
    const grayPoint = timelineGraySegments(page).first();
    await expect(grayPoint).toBeVisible();
});

Then('the other points for the object on the timeline should not be effected', async ({ page }) => {
    const coloredCount = await timelineColoredSegments(page).count();
    expect(coloredCount).toBeGreaterThan(0);
});

Given('one object is disabled in the object list', async ({ page }) => {
    const checkbox = currentFrameObjectCheckboxes(page).first();
    await checkbox.uncheck();
    await expect(checkbox).not.toBeChecked();
});

When('I check that object in the current-frame object list', async ({ page }) => {
    const checkbox = currentFrameObjectCheckboxes(page).first();
    await checkbox.check();
});

Then('the object should be active in this frame', async ({ page }) => {
    await expect(currentFrameObjectCheckboxes(page).first()).toBeChecked();
});

Then('the object should be displayed in the overlay on this frame', async ({ page }) => {
    await expect(overlayBoxes(page).first()).toBeVisible();
});

Then('the point for the object in this frame should turn to its color in the timeline', async ({ page }) => {
    const coloredPoint = timelineColoredSegments(page).first();
    await expect(coloredPoint).toBeVisible();

    const color = await getLocatorBackgroundColor(coloredPoint);
    expect(color).not.toBe('');
    expect(color).not.toContain('128, 128, 128');
});

// --------------------
// Timeline object disable / enable
// --------------------

Given('the timeline contains a detected object', async ({ page }) => {
    await expect(timelineObjectCheckboxes(page).first()).toBeVisible();
});

When('I uncheck that object in the timeline list', async ({ page }) => {
    await timelineObjectCheckboxes(page).first().uncheck();
});

Then('the object should no longer be selected in any frame', async ({ page }) => {
    await expect(timelineObjectCheckboxes(page).first()).not.toBeChecked();
});

Then('the line or points in the timeline for the object should turn gray', async ({ page }) => {
    await expect(timelineGraySegments(page).first()).toBeVisible();
});

Given('one object has been disabled on the timeline', async ({ page }) => {
    const checkbox = timelineObjectCheckboxes(page).first();
    await checkbox.uncheck();
    await expect(checkbox).not.toBeChecked();
});

When('I check that object in the timeline list', async ({ page }) => {
    await timelineObjectCheckboxes(page).first().check();
});

Then('the object should be selected in all frames', async ({ page }) => {
    await expect(timelineObjectCheckboxes(page).first()).toBeChecked();
});

Then('the line or points in the timeline for the object should turn to its color', async ({ page }) => {
    const colored = timelineColoredSegments(page).first();
    await expect(colored).toBeVisible();

    const color = await getLocatorBackgroundColor(colored);
    expect(color).not.toBe('');
    expect(color).not.toContain('128, 128, 128');
});

// --------------------
// Playback indicator
// --------------------

When('the video is playing', async ({ page }) => {
    await playVideo(page);
});

Then('a vertical playback indicator should move on the timeline with the current video time', async ({ page }) => {
    const indicator = playbackIndicator(page);
    await expect(indicator).toBeVisible();

    const beforeX = await getLocatorX(indicator);
    await page.waitForTimeout(300);
    const afterX = await getLocatorX(indicator);

    expect(afterX).toBeGreaterThan(beforeX);
});

Then('the current timestamp should be shown at the indicator', async ({ page }) => {
    await expect(timestampIndicator(page)).toBeVisible();
    await expect(timestampIndicator(page)).not.toHaveText('');
});

// --------------------
// Timeline click seek
// --------------------

Given('the video is paused', async ({ page }) => {
    await pauseVideo(page);
});

When('I click on a position on the timeline at {int} seconds', async ({ page }, seconds: number) => {
    await clickTimelineAtTime(page, seconds);
});

Then('the video current time should be set to {int} seconds', async ({ page }, seconds: number) => {
    const currentTime = await getVideoCurrentTime(page);
    expect(Math.abs(currentTime - seconds)).toBeLessThanOrEqual(0.15);
});

Then('the video frame displayed should correspond to {int} seconds', async ({ page }, seconds: number) => {
    const currentTime = await getVideoCurrentTime(page);
    expect(Math.abs(currentTime - seconds)).toBeLessThanOrEqual(0.15);
});

Then('the playback indicator should move to the clicked position on the timeline', async ({ page }) => {
    await expect(playbackIndicator(page)).toBeVisible();
});