import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { test, expect, section, siteRoot, type Locator, type Page } from '../fixtures';

// The two original samples; the option samples below are each wrapped in <div data-demo="...">
const originalSvgs = (page: Page) => section(page, 'our-svg').locator(':scope > div:not([data-demo]) > svg');
const demo = (page: Page, name: string) => section(page, 'our-svg').locator(`[data-demo="${name}"] svg`);
// HtmlAgilityPack writes the attribute in lower case
const viewBox = (svg: Locator) => svg.evaluate((el) => el.getAttribute('viewBox') ?? el.getAttribute('viewbox'));

test('inlines both the file and the media item SVG', async ({ page }) => {
  await page.goto('/en/');

  // One from src="/samples/partying-face.svg", one from the Logo media item
  await expect(originalSvgs(page)).toHaveCount(2);
  await expect(section(page, 'our-svg').locator('img')).toHaveCount(0);
});

test('ensure-viewbox adds a viewBox to each inlined SVG', async ({ page }) => {
  await page.goto('/en/');
  const svgs = originalSvgs(page);

  await expect(svgs).toHaveCount(2);
  for (const svg of await svgs.all()) {
    expect(await viewBox(svg)).toMatch(/^0 0 \d+(\.\d+)? \d+(\.\d+)?$/);
  }
});

test('class is added to the inlined svg', async ({ page }) => {
  await page.goto('/en/');
  await expect(demo(page, 'class')).toHaveClass(/demo-svg demo-svg--large/);
});

test('appsettings EnsureViewBox applies without the attribute', async ({ page }) => {
  await page.goto('/en/');
  const svg = demo(page, 'appsettings');

  expect(await viewBox(svg)).toBe('0 0 246 246');
  await expect(svg).not.toHaveAttribute('width');
});

test('ignore-appsettings skips the site settings', async ({ page }) => {
  await page.goto('/en/');
  const svg = demo(page, 'ignore-appsettings');

  await expect(svg).toHaveAttribute('width', '246');
  expect(await viewBox(svg)).toBeNull();
});

test('cache serves the cached svg after the file changes', async ({ page, umbracoMajor }) => {
  // Change the published site's copy of the file, never the source in the repo
  const file = join(siteRoot(umbracoMajor), 'wwwroot', 'samples', 'cache-demo.svg');
  const original = readFileSync(file, 'utf8');

  // Render once so the cached sample has read the original file
  await page.goto('/en/');
  await expect(demo(page, 'cached').locator('title')).toHaveText('original');

  try {
    writeFileSync(file, original.replace('<title>original</title>', '<title>changed</title>'));
    await page.reload();

    await expect(demo(page, 'uncached').locator('title')).toHaveText('changed');
    await expect(demo(page, 'cached').locator('title')).toHaveText('original');
  } finally {
    writeFileSync(file, original);
  }
});

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/58
test('cached copies of the same svg keep their own class', async ({ page }) => {
  await page.goto('/en/');

  // Both read the same cache entry; the class must come from each tag, not from the cache
  await expect(demo(page, 'cached-class-a')).toHaveClass('demo-svg-a');
  await expect(demo(page, 'cached-class-b')).toHaveClass('demo-svg-b');
});

test('removes everything from an svg that could run script', async ({ page }) => {
  await page.goto('/en/');
  const svg = demo(page, 'unsafe');
  await expect(svg.locator('circle')).toHaveCount(1);

  // No event handlers, foreignObject or script links survive, and none of them ran
  const handlers = await svg.evaluate((el) =>
    [el, ...el.querySelectorAll('*')].flatMap((node) => [...node.attributes].map((a) => a.name)).filter((name) => name.startsWith('on')),
  );
  expect(handlers).toEqual([]);
  await expect(svg.locator('foreignObject')).toHaveCount(0);
  expect(await svg.locator('a').getAttribute('href')).toBeNull();
  await svg.locator('circle').dispatchEvent('click'); // would run an onclick handler if one were left
  expect(await page.evaluate(() => (window as unknown as { ourSvgUnsafeRan?: boolean }).ourSvgUnsafeRan)).toBeUndefined();
});
