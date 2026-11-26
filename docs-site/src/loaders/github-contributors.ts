import type { Loader } from 'astro/loaders';
import { z } from 'astro/zod';
import { createGitHubClient } from '../lib/github';

/**
 * Loads the people who've contributed commits to the repository, from GitHub, each time the site is
 * built. Bots (Dependabot and the like) are left out. Display names come from each person's GitHub
 * profile when it can be read; otherwise their username is used.
 */
export function githubContributorsLoader({ repo }: { repo: string }): Loader {
	return {
		name: 'github-contributors',
		schema: z.object({
			login: z.string(),
			name: z.string(),
			avatar: z.url(),
			profile: z.url(),
		}),
		async load({ store, logger, parseData, generateDigest }) {
			const github = createGitHubClient(logger);

			let contributors: GitHubContributor[];
			try {
				contributors = (await github.getAll<GitHubContributor>(`repos/${repo}/contributors`)).filter(
					(c) => c.type === 'User' && !c.login.endsWith('[bot]'),
				);
			} catch (error) {
				logger.warn(`Couldn't load contributors from GitHub, so the contributors list will be empty: ${error}`);
				return;
			}

			// Names are nice to have: if profiles can't be read (rate limits, a restricted token), use usernames
			let namesFailed = false;
			const names = await Promise.all(
				contributors.map(async (c) => {
					if (namesFailed) return null;
					try {
						return (await github.get<{ name: string | null }>(`users/${c.login}`)).name;
					} catch {
						namesFailed = true;
						return null;
					}
				}),
			);
			if (namesFailed) logger.warn("Couldn't read every GitHub profile, so some contributors are shown by username");

			store.clear();
			for (const [i, c] of contributors.entries()) {
				const data = await parseData({
					id: c.login,
					data: {
						login: c.login,
						name: names[i]?.trim() || c.login,
						// A square avatar at twice the largest size it's shown at
						avatar: `${c.avatar_url}${c.avatar_url.includes('?') ? '&' : '?'}s=192`,
						profile: c.html_url,
					},
				});
				store.set({ id: c.login, data, digest: generateDigest(data) });
			}
			logger.info(`Loaded ${store.keys().length} contributors from GitHub`);
		},
	};
}

interface GitHubContributor {
	login: string;
	avatar_url: string;
	html_url: string;
	type: string;
}
