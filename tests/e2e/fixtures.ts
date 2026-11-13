import { test as base, expect, type Browser, type Locator, type Page } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

export { expect, type Page };

const e2eDir = fileURLToPath(new URL('.', import.meta.url));

/** The people a test can act as. Each one's saved session is written by specs/backoffice.setup.ts. */
export type Identity = 'backoffice' | 'editor' | 'no-content-user' | 'staff-member';

/** Saved session for one identity on one Umbraco major. */
export function authFile(umbracoMajor: number, identity: Identity): string {
  return join(e2eDir, '.data', `v${umbracoMajor}`, `${identity}-auth.json`);
}

/** Saved backoffice session for the unattended admin user. */
export function backofficeAuthFile(umbracoMajor: number): string {
  return authFile(umbracoMajor, 'backoffice');
}

type Credentials = { email: string; password: string };

/**
 * Logins for the demo people the TestSite creates on first boot (Migrations/AddDemoPeople.cs).
 * Read from the same appsettings.Development.json the site uses, so there is one source of truth.
 */
export const demoPeople = (() => {
  const file = join(e2eDir, '..', '..', 'Our.Umbraco.TagHelpers.TestSite', 'appsettings.Development.json');
  const settings = JSON.parse(readFileSync(file, 'utf8').replace(/^﻿/, ''));
  const people = settings.TestSite.DemoPeople as {
    EditorEmail: string;
    NoContentUserEmail: string;
    StaffMemberEmail: string;
    Password: string;
  };
  return {
    editor: { email: people.EditorEmail, password: people.Password },
    noContentUser: { email: people.NoContentUserEmail, password: people.Password },
    staffMember: { email: people.StaffMemberEmail, password: people.Password },
  } satisfies Record<string, Credentials>;
})();

type Fixtures = {
  /** Umbraco major of the current project (v16..v19). */
  umbracoMajor: number;
  /** A page in its own browser context, logged into the backoffice as the unattended admin user. */
  backofficePage: Page;
  /** A page logged into the backoffice as the demo Editor user. */
  editorPage: Page;
  /** A page logged into the backoffice as the demo user without Content section access. */
  noContentUserPage: Page;
  /** A page logged in as the demo member in the Staff member group. */
  staffMemberPage: Page;
};

async function pageAs(browser: Browser, baseURL: string | undefined, file: string, use: (page: Page) => Promise<void>) {
  const context = await browser.newContext({ baseURL, storageState: file });
  const page = await context.newPage();
  await use(page);
  await context.close();
}

export const test = base.extend<Fixtures>({
  umbracoMajor: async ({}, use, testInfo) => {
    await use(Number(testInfo.project.metadata.umbracoMajor));
  },
  backofficePage: async ({ browser, umbracoMajor, baseURL }, use) => {
    await pageAs(browser, baseURL, authFile(umbracoMajor, 'backoffice'), use);
  },
  editorPage: async ({ browser, umbracoMajor, baseURL }, use) => {
    await pageAs(browser, baseURL, authFile(umbracoMajor, 'editor'), use);
  },
  noContentUserPage: async ({ browser, umbracoMajor, baseURL }, use) => {
    await pageAs(browser, baseURL, authFile(umbracoMajor, 'no-content-user'), use);
  },
  staffMemberPage: async ({ browser, umbracoMajor, baseURL }, use) => {
    await pageAs(browser, baseURL, authFile(umbracoMajor, 'staff-member'), use);
  },
});

/** A tag helper sample on the demo site, wrapped in `<section data-sample="name">` (see Views/Home.cshtml). */
export function section(page: Page, name: string): Locator {
  return page.locator(`section[data-sample="${name}"]`);
}

/**
 * Logs into the backoffice, by default as the TestSite's unattended install user (overridable via env).
 * Waits for any section: users without Content access land in another one.
 */
export async function loginToBackoffice(page: Page, credentials?: Credentials): Promise<void> {
  const email = credentials?.email ?? process.env.E2E_BACKOFFICE_EMAIL ?? 'warren@hackmakedo.com';
  const password = credentials?.password ?? process.env.E2E_BACKOFFICE_PASSWORD ?? 'password1234';

  await page.goto('/umbraco');
  // The login page is a web component; CSS locators pierce its shadow DOM.
  const emailInput = page.locator('input[type="email"], input[name="username"]').first();
  const passwordInput = page.locator('input[type="password"]').first();
  await emailInput.waitFor({ timeout: 60_000 });

  // While the backoffice is still loading (several logins at once on a freshly booted site), the
  // login form can re-render and drop what was typed, so the submit does nothing. Retry until it lands.
  await expect(async () => {
    await emailInput.fill(email);
    await passwordInput.fill(password);
    await expect(emailInput).toHaveValue(email);
    await page.getByRole('button', { name: 'Login' }).click();
    await page.waitForURL('**/umbraco/section/**', { timeout: 15_000 });
  }).toPass({ timeout: 75_000 });
}

/** Logs a member in with the demo site's member login form. */
export async function loginMember(page: Page, credentials: Credentials): Promise<void> {
  await page.goto('/en/');
  const form = page.locator('form', { has: page.getByRole('button', { name: 'Log in' }) });
  await form.locator('input[name$="Username"]').fill(credentials.email);
  await form.locator('input[name$="Password"]').fill(credentials.password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByText('Welcome back')).toBeVisible();
}

/** Registers (and is thereby logged in as) a brand new member using the demo register form. */
export async function registerMember(page: Page): Promise<{ name: string; email: string }> {
  const id = `${Date.now()}-${Math.floor(Math.random() * 1e6)}`;
  const name = `E2E Member ${id}`;
  const email = `e2e-${id}@example.com`;
  const password = `E2e-${id}-Passw0rd!`;

  await page.goto('/en/');
  const form = page.locator('form', { has: page.getByRole('button', { name: 'Register' }) });
  await form.locator('input[name$="Name"]').first().fill(name);
  await form.locator('input[autocomplete="username"]').fill(email);
  await form.locator('input[autocomplete="new-password"]').nth(0).fill(password);
  await form.locator('input[autocomplete="new-password"]').nth(1).fill(password);
  await form.getByRole('button', { name: 'Register' }).click();
  await expect(page.getByText('Welcome back')).toBeVisible();

  return { name, email };
}
