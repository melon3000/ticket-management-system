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
