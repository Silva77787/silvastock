// Generates docs/design/tokens.css from docs/design/tokens.json and checks WCAG 2.2 AA contrast
// for the semantic pairs listed in tools/contrast-pairs.json (both themes).
// Usage: node docs/design/tools/build-tokens.mjs   (exit code 1 if any pair fails)
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const dir = join(here, '..');
const t = JSON.parse(readFileSync(join(dir, 'tokens.json'), 'utf8'));
const get = (p) => p.split('.').reduce((o, k) => o[k], t);
const resolve = (v) => (typeof v === 'string' && v.startsWith('{') ? resolve(get(v.slice(1, -1)).$value) : v);
const kebab = (s) => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
const flat = (o, pre = [], out = []) => {
  for (const [k, v] of Object.entries(o)) {
    if (k.startsWith('$')) continue;
    if (v && typeof v === 'object' && '$value' in v) out.push([[...pre, k], v]);
    else if (v && typeof v === 'object') flat(v, [...pre, k], out);
  }
  return out;
};
const themeVars = (th, indent) =>
  flat(t.theme[th]).map(([p, v]) => `${indent}--${p.map(kebab).join('-')}: ${resolve(v.$value)};`).join('\n');

let css = '/* SilvaStock design tokens — GENERATED from docs/design/tokens.json by tools/build-tokens.mjs. Do not edit by hand. */\n';
css += '/* Theme: light by default; dark via prefers-color-scheme (no toggle in the MVP). */\n\n:root {\n  color-scheme: light dark;\n';
const scalar = [];
for (const grp of ['font', 'space', 'radius', 'borderWidth', 'size', 'breakpoint', 'gutter', 'zIndex'])
  for (const [p, v] of flat(t[grp])) scalar.push(`  --${[grp, ...p].map(kebab).join('-')}: ${resolve(v.$value)};`);
for (const [p, v] of flat(t.motion)) {
  const val = Array.isArray(v.$value) ? `cubic-bezier(${v.$value.join(', ')})` : v.$value;
  scalar.push(`  --motion-${p.map(kebab).join('-')}: ${val};`);
}
css += scalar.join('\n') + '\n\n' + themeVars('light', '  ') + '\n}\n\n';
css += '@media (prefers-color-scheme: dark) {\n  :root {\n' + themeVars('dark', '    ') + '\n  }\n}\n\n';
css += '@media (prefers-reduced-motion: reduce) {\n  :root {\n    --motion-duration-fast: 0ms;\n    --motion-duration-base: 0ms;\n    --motion-duration-slow: 0ms;\n  }\n}\n';
css += '\n/* Text styles (mobile-first; larger sizes from 768px). */\n';
const up = [];
for (const [name, v] of Object.entries(t.typography)) {
  if (name.startsWith('$')) continue;
  const s = v.$value, m = v.mobile, base = m ? { ...s, ...m } : s;
  css += `.text-${kebab(name)} { font-family: var(--font-family-sans); font-size: ${base.fontSize}; line-height: ${base.lineHeight}; font-weight: ${s.fontWeight}; letter-spacing: ${s.letterSpacing};${s.fontVariantNumeric ? ` font-variant-numeric: ${s.fontVariantNumeric};` : ''} }\n`;
  if (m) up.push(`  .text-${kebab(name)} { font-size: ${s.fontSize}; line-height: ${s.lineHeight}; }`);
}
css += `@media (min-width: 768px) {\n${up.join('\n')}\n}\n`;
writeFileSync(join(dir, 'tokens.css'), css);

// Contrast check (WCAG 2.x relative luminance)
const lum = (h) => {
  const c = [0, 2, 4].map((i) => parseInt(h.replace('#', '').slice(i, i + 2), 16) / 255)
    .map((x) => (x <= 0.03928 ? x / 12.92 : ((x + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
};
const ratio = (a, b) => { const [x, y] = [lum(a), lum(b)].sort((p, q) => q - p); return (x + 0.05) / (y + 0.05); };
const pairs = JSON.parse(readFileSync(join(here, 'contrast-pairs.json'), 'utf8'));
let failures = 0;
console.log('| Foreground | Background | Use | Min | Light | Dark |\n|---|---|---|---|---|---|');
for (const [fg, bg, use, min] of pairs) {
  const cell = (th) => {
    const r = ratio(resolve(get(`theme.${th}.color.${fg}`).$value), resolve(get(`theme.${th}.color.${bg}`).$value));
    if (r < min) failures++;
    return `${r.toFixed(2)}:1 ${r >= min ? '✅' : '❌'}`;
  };
  console.log(`| \`${fg}\` | \`${bg}\` | ${use} | ${min}:1 | ${cell('light')} | ${cell('dark')} |`);
}
console.log(`\ntokens.css written. Contrast failures: ${failures}`);
process.exit(failures ? 1 : 0);
