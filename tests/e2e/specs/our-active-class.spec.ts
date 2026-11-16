import { test, expect, section, type Page } from '../fixtures';

// The sample has two navigations of the root's children (Page A, B, C), see our-active-class.cshtml:
// - links with our-active-class="nav-link--active" (direct children of the sample section),
// - list items with our-active-href + our-active-class="selected", each with its own children nested below.
// A link or list item is active when it points to the current page or one of its ancestors.
const sample = (page: Page) => section(page, 'our-active-class');
const navLink = (page: Page, name: string) => sample(page).locator(`:scope > a.nav-link:text-is("${name}")`);
const navItem = (page: Page, name: string) => sample(page).locator(`li:has(> a:text-is("${name}"))`);

test('renders the navigation for every child of the root', async ({ page }) => {
  await page.goto('/en/page-a/');

  await expect(navLink(page, 'Page A')).toHaveAttribute('href', '/en/page-a/');
  await expect(navLink(page, 'Page B')).toHaveAttribute('href', '/en/page-b/');
  await expect(navLink(page, 'Page C')).toHaveAttribute('href', '/en/page-c/');
});

// Every page of the demo site sits under a culture domain (/en, /es, /da): https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/97
test('marks the current page as active, but not other pages or its children', async ({ page }) => {
  await page.goto('/en/page-a/');

  await expect(navLink(page, 'Page A')).toContainClass('nav-link--active'); // current page
  await expect(navLink(page, 'Page B')).not.toContainClass('nav-link--active');
  await expect(navLink(page, 'Page C')).not.toContainClass('nav-link--active');

  await expect(navItem(page, 'Page A')).toContainClass('selected'); // current page
  await expect(navItem(page, 'Fallback Demo')).not.toContainClass('selected'); // a child of the current page
  await expect(navItem(page, 'Page B')).not.toContainClass('selected');
  await expect(navItem(page, 'Page C')).not.toContainClass('selected');
});

test('marks the ancestors of the current page as active', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');

  await expect(navLink(page, 'Page A')).toContainClass('nav-link--active'); // parent
  await expect(navLink(page, 'Page B')).not.toContainClass('nav-link--active');
  await expect(navLink(page, 'Page C')).not.toContainClass('nav-link--active');

  await expect(navItem(page, 'Page A')).toContainClass('selected'); // parent
  await expect(navItem(page, 'Fallback Demo')).toContainClass('selected'); // current page
  await expect(navItem(page, 'Page B')).not.toContainClass('selected');
  await expect(navItem(page, 'Page C')).not.toContainClass('selected');
});

test('marks the active page in another culture', async ({ page }) => {
  // Follow the Danish navigation rather than assume its URL segments
  await page.goto('/da/');
  const links = sample(page).locator(':scope > a.nav-link');
  const href = await links.first().getAttribute('href');
  expect(href).toMatch(/^\/da\//);

  await page.goto(href!);
  for (const link of await links.all()) {
    if ((await link.getAttribute('href')) === href) {
      await expect(link).toContainClass('nav-link--active');
    } else {
      await expect(link).not.toContainClass('nav-link--active');
    }
  }
});
