import type { AuthResponse, User } from '../api/types'

const STORAGE_KEY = 'ticketsystem.session'

export interface Session {
  token: string
  expiresAt: string
  user: User
}

export function toSession(response: AuthResponse): Session {
  return { token: response.token, expiresAt: response.expiresAt, user: response.user }
}

// The session is kept in sessionStorage, so every browser tab has its own signed-in user.

/** Reads the saved session. Returns null when there is none or when the token has expired. */
export function loadSession(): Session | null {
  try {
    const text = sessionStorage.getItem(STORAGE_KEY)
    if (!text) return null
    const session = JSON.parse(text) as Session
    if (!session.token || !session.user || new Date(session.expiresAt).getTime() <= Date.now()) {
      sessionStorage.removeItem(STORAGE_KEY)
      return null
    }
    return session
  } catch {
    return null
  }
}

export function saveSession(session: Session): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    // Storage can be blocked. The user then has to sign in again after a reload.
  }
}

export function clearSession(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // Nothing to clear.
  }
}
