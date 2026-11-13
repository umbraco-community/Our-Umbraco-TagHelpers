import { test, expect, section } from '../fixtures';

test('prints assembly versions, including the running Umbraco', async ({ page, umbracoMajor }) => {
  await page.goto('/en/');
  const items = section(page, 'our-version').locator('li');

  await expect(items.filter({ hasText: 'Umbraco.Core:' })).toHaveText(new RegExp(`^Umbraco\\.Core: ${umbracoMajor}\\.\\d+\\.\\d+`));
  await expect(items.filter({ hasText: 'Our.Umbraco.Taghelpers:' })).toHaveText(/^Our\.Umbraco\.Taghelpers: \d+\.\d+\.\d+/);
  await expect(items.filter({ hasText: 'Website Project Assembly Version:' })).toHaveText(/: \d+\.\d+\.\d+/);
});

test('prints nothing for an assembly that is not loaded', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-version').locator('li').filter({ hasText: 'Non existent:' })).toHaveText(/^Non existent:\s*$/);
});
