import { useEffect, useState } from 'react'
import { ticketsApi } from './ticketsApi'
import type { PagedResult, Ticket, TicketQuery } from './types'

interface State {
  /** The query that this state belongs to. */
  key: string
  data: PagedResult<Ticket> | null
  error: unknown
}

export interface TicketsResult {
  data: PagedResult<Ticket> | null
  error: unknown
  /** True while the answer to the current query is on its way. The previous page stays in data. */
  loading: boolean
}

/** Loads a page of tickets whenever the query changes. */
export function useTickets(query: TicketQuery): TicketsResult {
  const key = JSON.stringify(query)
  const [state, setState] = useState<State>({ key: '', data: null, error: null })

  useEffect(() => {
    let cancelled = false
    ticketsApi
      .list(JSON.parse(key) as TicketQuery)
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

  return { data: state.data, error: state.key === key ? state.error : null, loading: state.key !== key }
}
