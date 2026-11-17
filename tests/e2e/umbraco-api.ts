// Calls to Umbraco's Management API for things the tests do *while* they run (publishing, preview).
// The people the tests act as are created by the TestSite itself (Migrations/AddDemoPeople.cs).

import { readFileSync } from 'node:fs';
import { request, type APIRequestContext, type APIResponse, type Page } from '@playwright/test';

const apiBase = '/umbraco/management/api/v1';

/** The demo site's Home node (varies by culture: en-US, es, da). */
export const homeKey = 'b0c59e4c-158c-4fd4-9beb-ebe907693f1c';
/** "Page A" under Home (invariant), served at /en/page-a/. */
export const pageAKey = 'c455b06a-7fcc-4959-bacf-dc5e1dbc0c21';
/** The face-savoring-food.svg media item, picked as the home page's Logo. */
export const logoMediaKey = 'c928617e-1f81-4586-aee4-49be07205b90';

/**
 * Authorization header for a saved backoffice session.
 * Umbraco 16 keeps the access token in localStorage; from 17 it is an httpOnly cookie and the
 * backoffice sends the literal "Bearer [redacted]", which the server swaps for the cookie's token.
 */
export function authorizationHeader(storageStateFile: string): string {
  const state = JSON.parse(readFileSync(storageStateFile, 'utf8')) as {
    origins?: { localStorage?: { name: string; value: string }[] }[];
  };
  const stored = state.origins
    ?.flatMap((origin) => origin.localStorage ?? [])
    .find((item) => item.name === 'umb:userAuthTokenResponse');
  return `Bearer ${stored ? JSON.parse(stored.value).access_token : '[redacted]'}`;
}

async function ensureOk(response: APIResponse, what: string): Promise<APIResponse> {
  if (!response.ok()) {
    throw new Error(`${what} failed: ${response.status()} ${await response.text()}`);
  }
  return response;
}

/** The Management API, authenticated as a saved backoffice session. */
export class ManagementApi {
  private constructor(private readonly context: APIRequestContext) {}

  static async create(baseURL: string, storageStateFile: string): Promise<ManagementApi> {
    const context = await request.newContext({
      baseURL,
      storageState: storageStateFile,
      extraHTTPHeaders: { Authorization: authorizationHeader(storageStateFile) },
    });
    return new ManagementApi(context);
  }

  /** Publishes a document for the given cultures (`null` for an invariant document). */
  async publish(documentKey: string, cultures: (string | null)[]): Promise<void> {
    await ensureOk(
      await this.context.put(`${apiBase}/document/${documentKey}/publish`, {
        data: { publishSchedules: cultures.map((culture) => ({ culture })) },
      }),
      `Publishing ${documentKey}`,
    );
  }

  /** Saves a media item unchanged, as clicking Save in the backoffice does, which refreshes the media caches. */
  async saveMedia(mediaKey: string): Promise<void> {
    const media = await (await ensureOk(await this.context.get(`${apiBase}/media/${mediaKey}`), `Reading media ${mediaKey}`)).json();
    await ensureOk(
      await this.context.put(`${apiBase}/media/${mediaKey}`, { data: { values: media.values, variants: media.variants } }),
      `Saving media ${mediaKey}`,
    );
  }

  async dispose(): Promise<void> {
    await this.context.dispose();
  }
}

/**
 * Puts a logged-in backoffice page into preview mode (sets the UMB_PREVIEW cookie in its context).
 * Umbraco 16-17 have POST /preview; 18+ removed it and set the cookie when the preview URL is requested.
 */
export async function enterPreview(page: Page, storageStateFile: string, documentKey: string): Promise<void> {
  const headers = { Authorization: authorizationHeader(storageStateFile) };
  const preview = await page.context().request.post(`${apiBase}/preview`, { headers });
  if (preview.status() !== 404) {
    await ensureOk(preview, 'Entering preview');
    return;
  }
  await ensureOk(
    await page.context().request.get(
      `${apiBase}/document/${documentKey}/preview-url?providerAlias=umbDocumentUrlProvider&culture=en-US`,
      { headers },
    ),
    'Requesting the preview URL',
  );
}
