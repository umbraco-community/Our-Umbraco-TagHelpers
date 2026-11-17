import type { Loader } from 'astro/loaders';
import { z } from 'astro/zod';

/**
 * Loads the repository's GitHub releases at build time, with each release's notes rendered from
 * Markdown, so the release notes page is current every time the site is built.
 *
 * Set GITHUB_TOKEN to raise the API rate limit (the docs workflow does). If GitHub can't be reached,
 * the build carries on with no releases and the page links to GitHub instead.
 */
export function githubReleasesLoader({ repo }: { repo: string }): Loader {
	return {
		name: 'github-releases',
		schema: z.object({
			tag: z.string(),
			version: z.string(),
			name: z.string(),
			url: z.url(),
			publishedAt: z.coerce.date(),
			prerelease: z.boolean(),
		}),
		async load({ store, logger, parseData, renderMarkdown, generateDigest }) {
			const headers: Record<string, string> = {
				Accept: 'application/vnd.github+json',
				'X-GitHub-Api-Version': '2022-11-28',
				'User-Agent': 'our-umbraco-taghelpers-docs',
			};
			if (process.env.GITHUB_TOKEN) headers.Authorization = `Bearer ${process.env.GITHUB_TOKEN}`;

			const releases: GitHubRelease[] = [];
			try {
				for (let page = 1; page <= 10; page++) {
					const url = `https://api.github.com/repos/${repo}/releases?per_page=100&page=${page}`;
					let res = await fetch(url, { headers });
					if (res.status === 401 && headers.Authorization) {
						// An expired or unrelated token in the environment; the releases are public, so carry on without it
						logger.warn('GITHUB_TOKEN was rejected, so loading releases without it');
						delete headers.Authorization;
						res = await fetch(url, { headers });
					}
					if (!res.ok) throw new Error(`GitHub API responded ${res.status} ${res.statusText}`);
					const batch = (await res.json()) as GitHubRelease[];
					releases.push(...batch);
					if (batch.length < 100) break;
				}
			} catch (error) {
				// Keep the existing entries (from a previous build in dev) rather than failing the whole site
				logger.warn(`Couldn't load releases from GitHub, so the release notes page will be empty: ${error}`);
				return;
			}

			store.clear();
			for (const release of releases) {
				if (release.draft || !release.published_at) continue;
				const body = release.body ?? '';
				const data = await parseData({
					id: release.tag_name,
					data: {
						tag: release.tag_name,
						version: release.tag_name.replace(/^v/, ''),
						name: release.name || release.tag_name,
						url: release.html_url,
						publishedAt: release.published_at,
						prerelease: release.prerelease,
					},
				});
				const rendered = await renderMarkdown(body);
				rendered.html = nestUnderRelease(rendered.html, release.tag_name.replaceAll('.', '-'));
				store.set({
					id: release.tag_name,
					data,
					body,
					rendered,
					digest: generateDigest(body + JSON.stringify(data)),
				});
			}
			logger.info(`Loaded ${store.keys().length} releases from GitHub`);
		},
	};
}

/**
 * Every release's notes have the same headings ("What's Changed", "New Contributors"), and they sit
 * under the release's own heading on the page. So each id gets the release's prefix, to stay unique,
 * and each heading moves down a level (## becomes h3), to keep the page's outline right.
 */
function nestUnderRelease(html: string, prefix: string): string {
	return html
		.replace(/<(\/?)h([1-5])\b/g, (_, slash: string, level: string) => `<${slash}h${Number(level) + 1}`)
		.replace(/\bid="([^"]+)"/g, (_, id: string) => `id="${prefix}-${id}"`)
		.replace(/\bhref="#([^"]+)"/g, (_, id: string) => `href="#${prefix}-${id}"`);
}

interface GitHubRelease {
	tag_name: string;
	name: string | null;
	body: string | null;
	html_url: string;
	published_at: string | null;
	prerelease: boolean;
	draft: boolean;
}
