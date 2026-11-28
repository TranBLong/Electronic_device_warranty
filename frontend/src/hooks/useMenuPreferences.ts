import { useCallback, useEffect, useMemo, useState } from 'react';

export type MenuPref = { id: string; hidden: boolean };

const STORAGE_KEY = 'ewarranty.menu.prefs.v1';

const DEFAULT_ORDER = [
  '/',
  '/products',
  '/warranty-cards',
  '/repair-requests',
  '/parts',
  '/categories',
  '/service-centers',
  '/users',
  '/profile',
] as const;

const DEFAULT_IDS: ReadonlySet<string> = new Set(DEFAULT_ORDER);

function defaultPrefs(): MenuPref[] {
  return DEFAULT_ORDER.map((id) => ({ id, hidden: false }));
}

function isMenuPref(value: unknown): value is MenuPref {
  if (typeof value !== 'object' || value === null) return false;
  const pref = value as Record<string, unknown>;
  return typeof pref.id === 'string' &&
    DEFAULT_IDS.has(pref.id) &&
    typeof pref.hidden === 'boolean';
}

function loadPrefs(): MenuPref[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return defaultPrefs();

    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return defaultPrefs();

    const prefs: MenuPref[] = [];
    const known = new Set<string>();
    for (const item of parsed) {
      if (isMenuPref(item) && !known.has(item.id)) {
        prefs.push({ id: item.id, hidden: item.hidden });
        known.add(item.id);
      }
    }

    for (const id of DEFAULT_ORDER) {
      if (!known.has(id)) prefs.push({ id, hidden: false });
    }
    return prefs;
  } catch {
    return defaultPrefs();
  }
}

export function useMenuPreferences() {
  const [prefs, setPrefs] = useState<MenuPref[]>(loadPrefs);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
  }, [prefs]);

  const orderMap = useMemo(() => {
    const map = new Map<string, number>();
    prefs.forEach((pref, index) => map.set(pref.id, index));
    return map;
  }, [prefs]);

  const isHidden = useCallback(
    (id: string) => prefs.find((pref) => pref.id === id)?.hidden ?? false,
    [prefs]
  );

  const toggleHidden = useCallback((id: string) => {
    setPrefs((previous) =>
      previous.map((pref) => (pref.id === id ? { ...pref, hidden: !pref.hidden } : pref))
    );
  }, []);

  const move = useCallback((
    id: string,
    direction: 'up' | 'down',
    movableIds: readonly string[] = DEFAULT_ORDER
  ) => {
    setPrefs((previous) => {
      const movable = previous
        .map((pref, index) => ({ pref, index }))
        .filter(({ pref }) => movableIds.includes(pref.id));
      const currentIndex = movable.findIndex(({ pref }) => pref.id === id);
      if (currentIndex < 0) return previous;

      const targetIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
      if (targetIndex < 0 || targetIndex >= movable.length) return previous;

      const next = [...previous];
      const current = movable[currentIndex].index;
      const target = movable[targetIndex].index;
      [next[current], next[target]] = [next[target], next[current]];
      return next;
    });
  }, []);

  const reset = useCallback(() => {
    setPrefs(defaultPrefs());
  }, []);

  return { prefs, orderMap, isHidden, toggleHidden, move, reset };
}
