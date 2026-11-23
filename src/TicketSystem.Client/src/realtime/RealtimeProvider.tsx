import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { API_URL } from '../api/http'
import type { TicketEvent } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { RealtimeContext, type RealtimeState, type TicketEventListener } from './RealtimeContext'

const maxRetryDelayMs = 15_000

function isTicketEvent(value: unknown): value is TicketEvent {
  if (typeof value !== 'object' || value === null) return false
  const message = value as { type?: unknown; ticket?: unknown }
  return (message.type === 'TicketCreated' || message.type === 'TicketUpdated') && typeof message.ticket === 'object'
}

/**
 * Keeps one WebSocket open to /ws/tickets for the support staff and the administrator and passes the
 * messages on to the pages that listen. A lost connection is opened again after a growing pause.
 */
export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { user, token } = useAuth()
  const listeners = useRef(new Set<TicketEventListener>())
  const [connected, setConnected] = useState(false)
  const isStaff = user?.role === 'Support' || user?.role === 'Admin'

  const subscribe = useCallback((listener: TicketEventListener) => {
    listeners.current.add(listener)
    return () => {
      listeners.current.delete(listener)
    }
  }, [])

  useEffect(() => {
    if (!token || !isStaff) return
    const address = `${API_URL.replace(/^http/, 'ws')}/ws/tickets?access_token=${encodeURIComponent(token)}`
    let socket: WebSocket | null = null
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    let attempt = 0
    let stopped = false

    const connect = () => {
      socket = new WebSocket(address)
      socket.onopen = () => {
        attempt = 0
        setConnected(true)
      }
      socket.onmessage = (message: MessageEvent<string>) => {
        try {
          const event: unknown = JSON.parse(message.data)
          if (isTicketEvent(event)) listeners.current.forEach((listener) => listener(event))
        } catch {
          // A message that is not JSON is ignored.
        }
      }
      socket.onclose = () => {
        setConnected(false)
        if (stopped) return
        retryTimer = setTimeout(connect, Math.min(1000 * 2 ** attempt, maxRetryDelayMs))
        attempt += 1
      }
    }

    connect()
    return () => {
      stopped = true
      clearTimeout(retryTimer)
      socket?.close()
    }
  }, [token, isStaff])

  const value = useMemo<RealtimeState>(() => ({ subscribe, connected }), [subscribe, connected])
  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>
}
