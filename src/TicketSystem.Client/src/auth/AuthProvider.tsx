import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../api/authApi'
import { ApiError, setAuthToken, setUnauthorizedHandler } from '../api/http'
import type { LoginRequest, RegisterRequest } from '../api/types'
import { AuthContext, type AuthState } from './AuthContext'
import { clearSession, loadSession, saveSession, toSession, type Session } from './session'

// The token is set before the first render, so requests made by child components already carry it.
const initialSession = loadSession()
setAuthToken(initialSession?.token ?? null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(initialSession)

  const logout = useCallback(() => {
    setAuthToken(null)
    clearSession()
    setSession(null)
  }, [])

  const start = useCallback((next: Session) => {
    setAuthToken(next.token)
    saveSession(next)
    setSession(next)
  }, [])

  // An expired or rejected token ends the session.
  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  const login = useCallback(
    async (data: LoginRequest) => {
      const response = await authApi.login(data)
      if (response.user.role === 'Service') {
        throw new ApiError(403, 'Teenusekontoga ei saa veebiliideses sisse logida.')
      }
      start(toSession(response))
    },
    [start],
  )

  const register = useCallback(
    async (data: RegisterRequest) => {
      start(toSession(await authApi.register(data)))
    },
    [start],
  )

  const value = useMemo<AuthState>(
    () => ({ user: session?.user ?? null, token: session?.token ?? null, login, register, logout }),
    [session, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
