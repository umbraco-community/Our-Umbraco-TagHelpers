import type { Loader } from 'astro/loaders';
import { z } from 'astro/zod';
import { createGitHubClient } from '../lib/github';

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
			let releases: GitHubRelease[];
			try {
				releases = await createGitHubClient(logger).getAll<GitHubRelease>(`repos/${repo}/releases`);
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
