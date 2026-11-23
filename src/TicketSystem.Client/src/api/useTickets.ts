import { useCallback, useEffect, useState } from 'react'
import { ticketsApi } from './ticketsApi'
import type { PagedResult, Ticket, TicketQuery } from './types'

interface State {
  /** The query and reload counter that this state belongs to. */
  key: string
  data: PagedResult<Ticket> | null
  error: unknown
}

export interface TicketsResult {
  data: PagedResult<Ticket> | null
  error: unknown
  /** True while the answer to the current query is on its way. The previous page stays in data. */
  loading: boolean
  /** Loads the current page again. */
  reload: () => void
}

/** Loads a page of tickets whenever the query changes. */
export function useTickets(query: TicketQuery): TicketsResult {
  const [version, setVersion] = useState(0)
  const key = JSON.stringify({ query, version })
  const [state, setState] = useState<State>({ key: '', data: null, error: null })

  useEffect(() => {
    let cancelled = false
    const { query: current } = JSON.parse(key) as { query: TicketQuery }
    ticketsApi
      .list(current)
      .then((data) => {
        if (!cancelled) setState({ key, data, error: null })
      })
      .catch((error: unknown) => {
        if (!cancelled) setState((previous) => ({ key, data: previous.data, error }))
      })
    return () => {
      cancelled = true
    }
  }, [key])

  const reload = useCallback(() => setVersion((v) => v + 1), [])
  return { data: state.data, error: state.key === key ? state.error : null, loading: state.key !== key, reload }
}
