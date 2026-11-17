// Syntax colours for code blocks, from the Hack Make Do design system's CodeBlock guidance:
// keywords `type-role`, strings `type-community`, comments `ink-muted`, constants and numbers
// `accent-text`. Each reads at 4.5:1 or better on `panel` in its theme.
import { ExpressiveCodeTheme } from '@astrojs/starlight/expressive-code';

/** @param {{ name: string, type: 'dark' | 'light', c: Record<string, string> }} t */
function brandTheme({ name, type, c }) {
	return new ExpressiveCodeTheme({
		name,
		type,
		colors: {
			'editor.background': c.panel,
			'editor.foreground': c.ink,
			'editorGroupHeader.tabsBackground': c.ground,
			'tab.activeBackground': c.panel,
			'tab.activeForeground': c.ink,
			'tab.inactiveForeground': c.muted,
			'terminal.background': c.panel,
			'terminal.foreground': c.ink,
			'editor.selectionBackground': c.selection,
		},
		tokenColors: [
			{ scope: ['comment', 'punctuation.definition.comment'], settings: { foreground: c.muted, fontStyle: 'italic' } },
			{ scope: ['keyword', 'storage', 'storage.type', 'storage.modifier', 'keyword.control', 'keyword.operator.new'], settings: { foreground: c.role } },
			{ scope: ['string', 'string.quoted', 'punctuation.definition.string'], settings: { foreground: c.community } },
			{ scope: ['constant', 'constant.numeric', 'constant.language', 'support.constant', 'constant.character'], settings: { foreground: c.accent } },
			{ scope: ['entity.name.tag', 'meta.tag', 'punctuation.definition.tag'], settings: { foreground: c.role } },
			// Tag helper attributes such as our-if are what these docs are about, so they get the accent
			{ scope: ['entity.other.attribute-name'], settings: { foreground: c.accent } },
			{ scope: ['support.type.property-name', 'meta.object-literal.key'], settings: { foreground: c.accent } },
			{ scope: ['entity.name.type', 'entity.name.class', 'support.class', 'support.type'], settings: { foreground: c.ink } },
			{ scope: ['entity.name.function', 'support.function'], settings: { foreground: c.ink } },
			{ scope: ['keyword.operator', 'punctuation'], settings: { foreground: c.ink } },
			{ scope: ['markup.inserted'], settings: { foreground: c.community } },
			{ scope: ['markup.deleted'], settings: { foreground: c.accent } },
		],
	});
}

export const codeThemes = [
	brandTheme({
		name: 'hmd-night',
		type: 'dark',
		c: {
			ground: '#0b1c2f',
			panel: '#16293d',
			ink: '#eceaf7',
			muted: '#a9abd2',
			role: '#8e99ff',
			community: '#7fe0c3',
			accent: '#ffb1a4',
			selection: '#2a2f66',
		},
	}),
	brandTheme({
		name: 'hmd-paper',
		type: 'light',
		c: {
			ground: '#f6f3fa',
			panel: '#ffffff',
			ink: '#0b1c2f',
			muted: '#4a4f7a',
			role: '#3d4bd1',
			community: '#0b6b57',
			accent: '#a8321f',
			selection: '#d6d2e8',
		},
	}),
];
