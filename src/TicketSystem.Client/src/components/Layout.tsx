import type { ReactNode } from 'react'
import type { UserRole } from '../api/types'
import { roleLabels, useAuth } from '../auth/AuthContext'
import { useRealtime } from '../realtime/RealtimeContext'
import { avatarTone, initials } from './initials'
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
  { to: '/users', label: 'Kasutajad', roles: ['Admin'] },
  { to: '/profile', label: 'Profiil', roles: ['User', 'Support', 'Admin'] },
]

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()
  const { connected } = useRealtime()
  if (!user) return null
  const isStaff = user.role === 'Support' || user.role === 'Admin'

  return (
    <div className="shell">
      <header className="topbar">
        <a href="#/" className="brand">
          <span className="brand-mark">T</span>
          <span className="brand-name">Tiketisüsteem</span>
        </a>

        <nav className="nav" aria-label="Peamenüü">
          {navItems
            .filter((item) => item.roles.includes(user.role))
            .map((item) => (
              <NavLink key={item.to} to={item.to}>
                {item.label}
              </NavLink>
            ))}
        </nav>

        <div className="topbar-right">
          {isStaff && (
            <span
              className={connected ? 'live on' : 'live'}
              title={connected ? 'Reaalajas uuendused on ühendatud' : 'Reaalajas uuendused pole ühendatud'}
            >
              Reaalajas
            </span>
          )}
          <a href="#/profile" className="userbox" title="Ava profiil">
            <span className="userbox-text">
              <span className="username">{user.displayName}</span>
              <span className={`role role-${user.role.toLowerCase()}`}>{roleLabels[user.role]}</span>
            </span>
            <span className={`avatar ${avatarTone(user.id)}`}>{initials(user.displayName)}</span>
          </a>
          <button type="button" className="button secondary small" onClick={logout}>
            Logi välja
          </button>
        </div>
      </header>
      <main className="content">{children}</main>
    </div>
  )
}
