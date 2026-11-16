import { test, expect, section } from '../fixtures';

for (const [path, selectedCulture] of [
  ['/en/', 'en-US'],
  ['/da/', 'da'],
] as const) {
  test(`templated switcher lists every language and selects the current one on ${path}`, async ({ page }) => {
    await page.goto(path);
    const links = section(page, 'our-lang-switcher').locator('div.lang-switcher a.lang-switcher__link');

    await expect(links).toHaveCount(3);
    expect((await links.evaluateAll((as) => as.map((a) => a.getAttribute('href')))).sort()).toEqual(['/da', '/en', '/es']);

    const selected = section(page, 'our-lang-switcher').locator('div.lang-switcher a.selected');
    await expect(selected).toHaveCount(1);
    await expect(selected).toHaveAttribute('hreflang', selectedCulture);
  });
}

test('default switcher renders a list of every language', async ({ page }) => {
  await page.goto('/en/');
  const list = section(page, 'our-lang-switcher').locator('ul.lang-switcher');

  await expect(list.locator('li > a')).toHaveCount(3);
  await expect(list.locator('a.selected')).toHaveAttribute('hreflang', 'en-US');
});
