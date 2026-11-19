import { useSyncExternalStore } from 'react'

function subscribe(onChange: () => void): () => void {
  window.addEventListener('hashchange', onChange)
  return () => window.removeEventListener('hashchange', onChange)
}

function currentPath(): string {
  const hash = window.location.hash.replace(/^#/, '')
  return hash === '' ? '/' : hash
}

/** The path after the # in the address, for example "/login". */
export function usePath(): string {
  return useSyncExternalStore(subscribe, currentPath)
}

export function navigate(path: string): void {
  window.location.hash = path
}

/**
 * Compares a path with a pattern such as "/tickets/:id".
 * Returns the values of the :parts, or null when the path does not match.
 */
export function matchPath(pattern: string, path: string): Record<string, string> | null {
  const patternParts = pattern.split('/')
  const pathParts = path.split('/')
  if (patternParts.length !== pathParts.length) return null
  const values: Record<string, string> = {}
  for (let i = 0; i < patternParts.length; i++) {
    if (patternParts[i].startsWith(':')) values[patternParts[i].slice(1)] = decodeURIComponent(pathParts[i])
    else if (patternParts[i] !== pathParts[i]) return null
  }
  return values
}
