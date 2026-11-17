import { defineRouteMiddleware, type StarlightRouteData } from '@astrojs/starlight/route-data';
import { getCollection } from 'astro:content';
import { isNew, isUnreleased } from './lib/versions';

type SidebarEntry = StarlightRouteData['sidebar'][number];

let badgesByPath: Promise<Map<string, { text: string; variant: 'tip' | 'caution' }>> | undefined;

/** Works out each page's sidebar badge once per build, from its `addedIn` and `removedIn` frontmatter. */
function getBadges() {
	badgesByPath ??= (async () => {
		const map = new Map<string, { text: string; variant: 'tip' | 'caution' }>();
		for (const entry of await getCollection('docs')) {
			const { addedIn, removedIn, sidebar } = entry.data;
			if (sidebar.badge) continue; // A badge written in the page's frontmatter wins
			if (removedIn) map.set(entry.id, { text: 'Removed', variant: 'caution' });
			else if (addedIn && (await isUnreleased(addedIn))) map.set(entry.id, { text: 'Coming soon', variant: 'tip' });
			else if (addedIn && (await isNew(addedIn))) map.set(entry.id, { text: 'New', variant: 'tip' });
		}
		return map;
	})();
	return badgesByPath;
}

const base = import.meta.env.BASE_URL.replace(/\/$/, '');

function pageId(href: string) {
	return href.slice(base.length).replace(/^\/|\/$/g, '');
}

export const onRequest = defineRouteMiddleware(async (context) => {
	const badges = await getBadges();
	if (badges.size === 0) return;

	const apply = (entries: SidebarEntry[]) => {
		for (const entry of entries) {
			if (entry.type === 'group') apply(entry.entries);
			else if (!entry.badge) {
				const badge = badges.get(pageId(entry.href));
				if (badge) entry.badge = badge;
			}
		}
	};
	apply(context.locals.starlightRoute.sidebar);
});
