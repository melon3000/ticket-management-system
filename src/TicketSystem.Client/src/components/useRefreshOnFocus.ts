import { useEffect } from 'react'

/** Calls refresh when the user comes back to the tab or the window, so a page does not show old data. */
export function useRefreshOnFocus(refresh: () => void): void {
  useEffect(() => {
    let last = Date.now()
    const run = () => {
      if (document.visibilityState === 'hidden' || Date.now() - last < 1000) return
      last = Date.now()
      refresh()
    }
    document.addEventListener('visibilitychange', run)
    window.addEventListener('focus', run)
    return () => {
      document.removeEventListener('visibilitychange', run)
      window.removeEventListener('focus', run)
    }
  }, [refresh])
}
