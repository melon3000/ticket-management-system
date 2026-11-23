import { useEffect, useRef } from 'react'
import { useRealtime, type TicketEventListener } from './RealtimeContext'

/** Calls the listener for every ticket message from the server. Only the support staff receive messages. */
export function useTicketEvents(listener: TicketEventListener): void {
  const { subscribe } = useRealtime()
  const latest = useRef(listener)

  useEffect(() => {
    latest.current = listener
  })

  useEffect(() => subscribe((event) => latest.current(event)), [subscribe])
}
