import { useCallback, useEffect, useState } from 'react'
import { usersApi } from './ticketsApi'
import type { User } from './types'

interface State {
  key: number
  data: User[] | null
  error: unknown
}

/** Loads every user. Only an administrator may ask for it. */
export function useUsers() {
  const [version, setVersion] = useState(0)
  const [state, setState] = useState<State>({ key: -1, data: null, error: null })

  useEffect(() => {
    let cancelled = false
    usersApi
      .list()
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
