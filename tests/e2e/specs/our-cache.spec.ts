import { test, expect, section, type Page } from '../fixtures';

// The sample renders DateTime.Now in an uncached block (expires-on a date in the past) and in a
// block cached for 120 seconds. Read both from one page render.
async function timestamps(page: Page) {
  const text = await section(page, 'our-cache').innerText();
  return {
    uncached: text.match(/^(.*) - A set Date in time/m)?.[1],
    cached: text.match(/^(.*) - Every 120 seconds/m)?.[1],
  };
}

test('serves the cached content on the next request', async ({ page }) => {
  await page.goto('/en/');
  const first = await timestamps(page);
  expect(first.cached).toBeTruthy();

  await page.waitForTimeout(1_500);
  await page.reload();
  let second = await timestamps(page);

  // The page itself was rendered again...
  expect(second.uncached).not.toBe(first.uncached);

  // ...but the cached block was not. If the 120s entry happened to expire between the two
  // requests, the refreshed entry must then be served on the following request.
  if (second.cached !== first.cached) {
    const refreshed = second.cached;
    // Timestamps have one-second resolution: wait so an uncached value could not match by chance
    await page.waitForTimeout(1_500);
    await page.reload();
    second = await timestamps(page);
    expect(second.cached).toBe(refreshed);
  } else {
    expect(second.cached).toBe(first.cached);
  }
});
