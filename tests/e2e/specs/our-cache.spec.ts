import { test, expect, section, authFile, type Page } from '../fixtures';
import { ManagementApi, enterPreview, homeKey, pageAKey } from '../umbraco-api';

// These tests publish content and depend on what is (or isn't) in the cache, so they run one at a time.
test.describe.configure({ mode: 'serial' });

// Timestamps have one-second resolution: wait between renders so an uncached value can't match by chance
const pause = (page: Page) => page.waitForTimeout(1_500);

// Each sample block renders DateTime.Now next to a label. Read them all from one page render.
// (The time contains U+202F before AM/PM, so match everything up to the label.)
async function timestamps(page: Page) {
  const text = await section(page, 'our-cache').innerText();
  return {
    uncached: text.match(/^(.*) - A set Date in time/m)?.[1],
    cached: text.match(/^(.*) - Every 120 seconds/m)?.[1],
    optOut: text.match(/^(.*) - Not cleared by publishing other pages/m)?.[1],
  };
}

async function publish(baseURL: string | undefined, umbracoMajor: number, documentKey: string, cultures: (string | null)[]) {
  const api = await ManagementApi.create(baseURL!, authFile(umbracoMajor, 'backoffice'));
  try {
    await api.publish(documentKey, cultures);
  } finally {
    await api.dispose();
  }
}

test('serves the cached content on the next request', async ({ page }) => {
  await page.goto('/en/');
  const first = await timestamps(page);
  expect(first.cached).toBeTruthy();

  await pause(page);
  await page.reload();
  let second = await timestamps(page);

  // The page itself was rendered again...
  expect(second.uncached).not.toBe(first.uncached);

  // ...but the cached block was not. If the 120s entry happened to expire between the two
  // requests, the refreshed entry must then be served on the following request.
  if (second.cached !== first.cached) {
    const refreshed = second.cached;
    await pause(page);
    await page.reload();
    second = await timestamps(page);
    expect(second.cached).toBe(refreshed);
  } else {
    expect(second.cached).toBe(first.cached);
  }
});

test('publishing a page clears the cached content', async ({ page, baseURL, umbracoMajor }) => {
  await page.goto('/en/');
  const before = await timestamps(page);
  expect(before.cached).toBeTruthy();

  await pause(page);
  await publish(baseURL, umbracoMajor, pageAKey, [null]);
  await page.reload();

  expect((await timestamps(page)).cached).not.toBe(before.cached);
});

test('update-cache-on-publish="false" keeps its content when another page is published', async ({ page, baseURL, umbracoMajor }) => {
  await page.goto('/en/');
  const before = await timestamps(page);
  expect(before.optOut).toBeTruthy();

  await pause(page);
  await publish(baseURL, umbracoMajor, pageAKey, [null]);
  await page.reload();

  expect((await timestamps(page)).optOut).toBe(before.optOut);
});

test('vary-by renders new content when its value changes', async ({ page, baseURL, umbracoMajor }) => {
  await page.goto('/en/');
  const before = await timestamps(page);
  expect(before.optOut).toBeTruthy();

  // The opt-out block varies by the page's UpdateDate, which publishing Home changes
  await pause(page);
  await publish(baseURL, umbracoMajor, homeKey, ['en-US', 'es', 'da']);
  await page.reload();

  expect((await timestamps(page)).optOut).not.toBe(before.optOut);
});

test('is skipped in debug mode', async ({ page }) => {
  await page.goto('/en/?umbdebug=true');
  const first = await timestamps(page);
  expect(first.cached).toBeTruthy();

  await pause(page);
  await page.reload();

  expect((await timestamps(page)).cached).not.toBe(first.cached);
});

test('is skipped in preview', async ({ backofficePage: page, umbracoMajor }) => {
  await enterPreview(page, authFile(umbracoMajor, 'backoffice'), homeKey);

  await page.goto('/en/');
  const first = await timestamps(page);
  expect(first.cached).toBeTruthy();

  await pause(page);
  await page.reload();

  expect((await timestamps(page)).cached).not.toBe(first.cached);
});
