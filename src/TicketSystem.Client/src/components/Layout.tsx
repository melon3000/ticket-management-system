import type { ReactNode } from 'react'
import type { UserRole } from '../api/types'
import { roleLabels, useAuth } from '../auth/AuthContext'
import { useRealtime } from '../realtime/RealtimeContext'
import { NavLink } from './NavLink'

interface NavItem {
  to: string
  label: string
  /** Roles that see the link. */
  roles: UserRole[]
}

const navItems: NavItem[] = [
  { to: '/', label: 'Tiketid', roles: ['User', 'Support', 'Admin'] },
  { to: '/statistics', label: 'Statistika', roles: ['Admin'] },
]

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()
  const { connected } = useRealtime()
  if (!user) return null
  const isStaff = user.role === 'Support' || user.role === 'Admin'

  return (
    <div className="app">
      <header className="topbar">
        <span className="brand">Tiketisüsteem</span>
        <nav className="nav">
          {navItems
            .filter((item) => item.roles.includes(user.role))
            .map((item) => (
              <NavLink key={item.to} to={item.to}>
                {item.label}
              </NavLink>
            ))}
        </nav>
        <div className="userbox">
          {isStaff && (
            <span
              className={connected ? 'live on' : 'live'}
              title={connected ? 'Reaalajas uuendused on ühendatud' : 'Reaalajas uuendused pole ühendatud'}
            >
              Reaalajas
            </span>
          )}
          <span className="username">{user.displayName}</span>
          <span className={`role role-${user.role.toLowerCase()}`}>{roleLabels[user.role]}</span>
          <button type="button" className="button secondary" onClick={logout}>
            Logi välja
          </button>
        </div>
      </header>
      <main className="content">{children}</main>
    </div>
  )
}
