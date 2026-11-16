import { test, expect, section } from '../fixtures';

const homeKey = 'b0c59e4c-158c-4fd4-9beb-ebe907693f1c';
const otherKey = 'b0c59e4c-158c-4fd4-9beb-ebe907693e4a';
const editUrl = (key: string) => `/umbraco/section/content/workspace/document/edit/${key}`;

test('hidden for anonymous visitors', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-edit-link').locator('a')).toHaveCount(0);
});

test('shown to a backoffice user with access to content', async ({ backofficePage: page }) => {
  await page.goto('/en/');
  const links = section(page, 'our-edit-link').locator('a');

  await expect(links).toHaveCount(4);
  await expect(links.filter({ hasText: 'Edit this Page' }).first()).toHaveAttribute('href', editUrl(homeKey));
  await expect(links.filter({ hasText: 'Edit this Page in new tab' })).toHaveAttribute('target', '_blank');
  await expect(links.filter({ hasText: 'Edit a specific page that not itself' })).toHaveAttribute('href', editUrl(otherKey));

  const styled = links.filter({ hasText: 'Edit down bottom left' });
  await expect(styled).toHaveAttribute('href', editUrl(homeKey));
  // use-default-styles wraps the link in a fixed-position div
  await expect(styled.locator('xpath=..')).toHaveAttribute('style', /position:fixed/);
});

// Cookies on localhost ignore the port, so make sure a login never leaks into another test
test('a fresh visitor is anonymous even after the backoffice login', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-edit-link').locator('a')).toHaveCount(0);
});
