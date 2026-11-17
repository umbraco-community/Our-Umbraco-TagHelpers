import { defineCollection } from 'astro:content';
import { z } from 'astro/zod';
import { docsLoader } from '@astrojs/starlight/loaders';
import { docsSchema } from '@astrojs/starlight/schema';
import { githubReleasesLoader } from './loaders/github-releases';

const version = z.string().regex(/^\d+\.\d+\.\d+$/, 'Use a plain version such as 2.1.0');

export const collections = {
	docs: defineCollection({
		loader: docsLoader(),
		schema: docsSchema({
			extend: z.object({
				/** The package version that first shipped this tag helper. Shows as a badge by the title. */
				addedIn: version.optional(),
				/** The package version that removed it. */
				removedIn: version.optional(),
				/** How it's used in Razor: as its own element, or as an attribute on any element. */
				usage: z.enum(['element', 'attribute']).optional(),
			}),
		}),
	}),
	releases: defineCollection({
		loader: githubReleasesLoader({ repo: 'umbraco-community/Our-Umbraco-TagHelpers' }),
	}),
};
