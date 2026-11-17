import { test, expect } from '../fixtures';

const sampleHeadings = [
  'our-dictionary',
  'our-if',
  'our-text-if',
  'Replacement for BeginUmbracoForm',
  'our-lang-switcher',
  'our-svg',
  'our-fallback',
  'our-version',
  'our-member-include and our-member-exclude',
  'our-user-include and our-user-exclude',
  'our-edit-link',
  'our-active-class',
  'our-link',
  'our-cache',
  'our-img',
  'our-self-host',
];

test('home renders every sample', async ({ page }) => {
  const response = await page.goto('/en/');
  expect(response?.status()).toBe(200);

  for (const heading of sampleHeadings) {
    await expect(page.getByRole('heading', { level: 2, name: heading, exact: true })).toBeVisible();
  }
});

test('no unprocessed our-* tags or attributes', async ({ page }) => {
  await page.goto('/en/');

  const leftovers = await page.evaluate(() => {
    const all = Array.from(document.querySelectorAll('*'));
    return {
      elements: all.filter((el) => el.tagName.startsWith('OUR-')).map((el) => el.tagName.toLowerCase()),
      attributes: all.flatMap((el) =>
        Array.from(el.attributes)
          .filter((a) => a.name.startsWith('our-'))
          .map((a) => `${el.tagName.toLowerCase()}[${a.name}]`),
      ),
    };
  });

  expect(leftovers.elements).toEqual([]);
  expect(leftovers.attributes).toEqual([]);
});

test('culture pages render', async ({ page }) => {
  for (const path of ['/', '/es/', '/da/', '/en/page-a/', '/en/page-a/page-ab/', '/en/page-b/page-ba/', '/en/page-c/page-ca/']) {
    const response = await page.goto(path);
    expect(response?.status(), path).toBe(200);
  }
});
