import { useEffect, useState } from 'react'
import { commentsApi, ticketsApi } from './ticketsApi'
import type { Ticket, TicketComment } from './types'

interface State {
  /** The ticket id and the reload counter that this state belongs to. */
  key: string
  ticket: Ticket | null
  comments: TicketComment[]
  error: unknown
}

export interface TicketResult {
  ticket: Ticket | null
  comments: TicketComment[]
  error: unknown
  loading: boolean
  /** Loads the ticket and its comments again, for example after a change. */
  reload: () => void
}

/** Loads one ticket together with its comments. */
export function useTicket(id: number): TicketResult {
  const [version, setVersion] = useState(0)
  const key = `${id}:${version}`
  const [state, setState] = useState<State>({ key: '', ticket: null, comments: [], error: null })

  useEffect(() => {
    let cancelled = false
    const ticketId = Number(key.split(':')[0])
    Promise.all([ticketsApi.get(ticketId), commentsApi.list(ticketId)])
      .then(([ticket, comments]) => {
        if (!cancelled) setState({ key, ticket, comments, error: null })
      })
      .catch((error: unknown) => {
        if (!cancelled) setState((previous) => ({ ...previous, key, error }))
      })
    return () => {
      cancelled = true
    }
  }, [key])

  // Data of another ticket is not shown while this one is loading.
  const sameTicket = state.ticket?.id === id
  return {
    ticket: sameTicket ? state.ticket : null,
    comments: sameTicket ? state.comments : [],
    error: state.key === key ? state.error : null,
    loading: state.key !== key,
    reload: () => setVersion((v) => v + 1),
  }
}
