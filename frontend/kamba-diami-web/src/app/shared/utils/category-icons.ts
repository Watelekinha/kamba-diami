const CATEGORY_ICONS: { match: string[]; paths: string[] }[] = [
  { match: ['bab', 'crian'], paths: ['M12 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6z', 'M6.5 20a5.5 5.5 0 0 1 11 0'] },
  { match: ['cozinha'], paths: ['M4 11h16', 'M5.5 11v5.5A3.5 3.5 0 0 0 9 20h6a3.5 3.5 0 0 0 3.5-3.5V11', 'M9 7.5c0-1.2 1-1.3 1-2.5', 'M14 7.5c0-1.2 1-1.3 1-2.5'] },
  { match: ['idoso'], paths: ['M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10z'] },
  { match: ['engom'], paths: ['M4 17h16', 'M6 17l1.6-6h8.9a3.5 3.5 0 0 1 3.5 3.5V17', 'M9 8h6'] },
  { match: ['jardin'], paths: ['M5 19c0-8 6-13 14-14-1 8-6 14-14 14z', 'M5 19l7-7'] },
  { match: ['lavag', 'roupa'], paths: ['M5 3h14v18H5z', 'M12 17.5a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', 'M8.5 6h.01'] },
  { match: ['motorista'], paths: ['M5.5 15l1.4-4.5A2 2 0 0 1 8.8 9h6.4a2 2 0 0 1 1.9 1.5l1.4 4.5', 'M4 15h16v4H4z', 'M7.5 19v1.5', 'M16.5 19v1.5'] },
  { match: ['outro'], paths: ['M12 5v14', 'M5 12h14'] }
];
const DEFAULT_ICON = ['M12 3l1.8 4.7 4.7 1.8-4.7 1.8L12 16l-1.8-4.7-4.7-1.8 4.7-1.8z', 'M18.5 15l.8 2.2 2.2.8-2.2.8-.8 2.2-.8-2.2-2.2-.8 2.2-.8z'];

/** Caminhos SVG (viewBox 24) do ícone de uma categoria de serviço, escolhido pelo nome. */
export function categoryIcon(name: string): string[] {
  const normalized = name.toLowerCase();
  return CATEGORY_ICONS.find((icon) => icon.match.some((m) => normalized.includes(m)))?.paths ?? DEFAULT_ICON;
}
