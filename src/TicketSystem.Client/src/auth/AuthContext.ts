import { createContext, useContext } from 'react'
import type { LoginRequest, RegisterRequest, User, UserRole } from '../api/types'

export interface AuthState {
  user: User | null
  token: string | null
  login: (data: LoginRequest) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}

/** Estonian names of the roles, as shown in the interface. */
export const roleLabels: Record<UserRole, string> = {
  User: 'Kasutaja',
  Support: 'Tugiisik',
  Admin: 'Administraator',
  Service: 'Teenusekonto',
}
