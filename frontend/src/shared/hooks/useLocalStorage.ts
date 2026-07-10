import { useCallback, useState } from 'react'

// Key format: f1app__{featureName}__{version} — e.g. f1app__streak__v1.
// The version is embedded in the key itself (not a field inside the stored
// value): bumping the version is how a future breaking schema change is
// "migrated" — old data is simply never read again under the new key,
// rather than needing in-place upgrade logic for a version bump that
// doesn't exist yet.
export function useLocalStorage<T>(key: string, defaultValue: T): [T, (value: T) => void] {
  const [value, setValue] = useState<T>(() => {
    try {
      const raw = window.localStorage.getItem(key)
      return raw ? (JSON.parse(raw) as T) : defaultValue
    } catch {
      return defaultValue
    }
  })

  const setAndPersist = useCallback(
    (next: T) => {
      setValue(next)
      try {
        window.localStorage.setItem(key, JSON.stringify(next))
      } catch {
        // localStorage unavailable (private browsing quota, disabled
        // storage, etc.) — fail silently; in-memory state for this session
        // still works, it just won't persist across reloads.
      }
    },
    [key],
  )

  return [value, setAndPersist]
}
