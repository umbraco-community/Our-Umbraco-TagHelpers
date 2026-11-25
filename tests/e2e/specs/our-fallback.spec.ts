import { test, expect, section, type Page } from '../fixtures';

test('uses the culture variant when it exists, otherwise the tag content', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-fallback').locator('h3')).toHaveText([
    'DA Header',
    'I do NOT have a da-DK culture variant of this property',
  ]);
});

// Fallback Demo (/en/page-a/fallback-demo/) is a child of Page A with an empty header
const demo = (page: Page, name: string) => section(page, 'our-fallback').locator(`[data-demo="${name}"]`);

test('ancestors mode uses the nearest ancestor value', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');
  await expect(demo(page, 'ancestors')).toHaveText('Page A');
});

test("uses the page's own value when it has one", async ({ page }) => {
  await page.goto('/en/page-a/');
  await expect(demo(page, 'ancestors')).toHaveText('Page A');
  await expect(demo(page, 'content')).toHaveText('Page A');
});

test('falls back to the tag content when there is no value', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');
  await expect(demo(page, 'content')).toHaveText('This page has no header, so the tag content is shown');
});

test('a plain-text property is shown as text, not rendered as HTML', async ({ page }) => {
  // The Markup Demo page's Header (a Textstring) is "<em>Shown as text, not emphasised</em>"
  await page.goto('/en/page-a/markup-demo/');
  const header = section(page, 'our-fallback').locator('[data-demo="ancestors"]');

  await expect(header).toHaveText('<em>Shown as text, not emphasised</em>');
  await expect(header.locator('em')).toHaveCount(0);
});

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/121
test('a Rich Text property is rendered as HTML', async ({ page }) => {
  await page.goto('/en/page-a/markup-demo/');
  const richText = section(page, 'our-fallback').locator('[data-demo="rich-text"]');

  await expect(richText.locator('p > strong')).toHaveText('Rich text');
  await expect(richText).toHaveText('Rich text from the Rich Text Editor');
});

test('a Markdown property is rendered as HTML', async ({ page }) => {
  await page.goto('/en/page-a/markup-demo/');
  const markdown = section(page, 'our-fallback').locator('[data-demo="markdown"]');

  await expect(markdown.locator('strong')).toHaveText('Bold');
  await expect(markdown).not.toContainText('**');
});

test('empty Rich Text and Markdown properties show the content inside the tag', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');
  const sample = section(page, 'our-fallback');

  await expect(sample.locator('[data-demo="rich-text"]')).toHaveText('This page has no rich text');
  await expect(sample.locator('[data-demo="markdown"]')).toHaveText('This page has no Markdown');
});
