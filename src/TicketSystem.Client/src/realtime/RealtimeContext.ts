import { createContext, useContext } from 'react'
import type { TicketEvent } from '../api/types'

export type TicketEventListener = (event: TicketEvent) => void

export interface RealtimeState {
  /** Registers a listener. Returns a function that removes it. */
  subscribe: (listener: TicketEventListener) => () => void
  /** True while the WebSocket is open. */
  connected: boolean
}

export const RealtimeContext = createContext<RealtimeState | null>(null)

export function useRealtime(): RealtimeState {
  const value = useContext(RealtimeContext)
  if (!value) throw new Error('useRealtime must be used inside RealtimeProvider')
  return value
}
