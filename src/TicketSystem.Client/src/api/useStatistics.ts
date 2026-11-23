import { useCallback, useEffect, useState } from 'react'
import { statisticsApi } from './ticketsApi'
import type { ResolutionStatistics } from './types'

interface State {
  key: number
  data: ResolutionStatistics | null
  error: unknown
}

/** Loads the average resolution times. */
export function useStatistics() {
  const [version, setVersion] = useState(0)
  const [state, setState] = useState<State>({ key: -1, data: null, error: null })

  useEffect(() => {
    let cancelled = false
    statisticsApi
      .resolutionTimes()
      .then((data) => {
        if (!cancelled) setState({ key: version, data, error: null })
      })
      .catch((error: unknown) => {
        if (!cancelled) setState((previous) => ({ key: version, data: previous.data, error }))
      })
    return () => {
      cancelled = true
    }
  }, [version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])
  return {
    data: state.data,
    error: state.key === version ? state.error : null,
    loading: state.key !== version,
    reload,
  }
}
