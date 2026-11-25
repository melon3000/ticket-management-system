import { useState } from 'react'
import type { User, UserRole } from '../api/types'
import { usersApi } from '../api/ticketsApi'
import { useUsers } from '../api/useUsers'
import { roleLabels, useAuth } from '../auth/AuthContext'
import { FormError } from '../components/FormError'
import { avatarTone, initials } from '../components/initials'

/** The roles an administrator can give to a person. The service account role is not on the list. */
const assignableRoles: UserRole[] = ['User', 'Support', 'Admin']

export function UsersPage() {
  const { user: me } = useAuth()
  const { data, error, loading, reload } = useUsers()
  const [savingId, setSavingId] = useState<number | null>(null)
  const [saveError, setSaveError] = useState<unknown>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [query, setQuery] = useState('')

  async function changeRole(target: User, role: UserRole) {
    setSavingId(target.id)
    setSaveError(null)
    setMessage(null)
    try {
      await usersApi.changeRole(target.id, role)
      setMessage(`${target.displayName} on nüüd ${roleLabels[role].toLowerCase()}.`)
      reload()
    } catch (e) {
      setSaveError(e)
    } finally {
      setSavingId(null)
    }
  }

  const needle = query.trim().toLowerCase()
  const visible =
    data === null
      ? null
      : data.filter(
          (person) =>
            needle === '' ||
            person.displayName.toLowerCase().includes(needle) ||
            person.email.toLowerCase().includes(needle) ||
            roleLabels[person.role].toLowerCase().includes(needle),
        )

  return (
    <section>
      <div className="page-head">
        <h1>
          Kasutajad
          {data !== null && <span className="count">{data.length}</span>}
        </h1>
      </div>
      <p className="muted">Rolli muudatus jõustub, kui inimene järgmine kord sisse logib.</p>

      <FormError error={error} />
      <FormError error={saveError} />
      {message && <p className="notice">{message}</p>}

      <div className="toolbar">
        <input
          type="search"
          className="search"
          placeholder="Otsi nime, e-posti või rolli järgi"
          aria-label="Otsi kasutajaid"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
      </div>

      <div className={loading ? 'card list-card loading' : 'card list-card'}>
        {visible === null ? (
          <p className="muted empty">{loading ? 'Laen…' : 'Kasutajaid ei saanud laadida.'}</p>
        ) : visible.length === 0 ? (
          <p className="muted empty">Otsingule ei vasta ükski kasutaja.</p>
        ) : (
          <ul className="ticket-list">
            {visible.map((person) => {
              const isMe = person.id === me?.id
              return (
                <li key={person.id} className="user-row">
                  <span className={`avatar ${avatarTone(person.id)}`}>{initials(person.displayName)}</span>
                  <span className="row-main">
                    <span className="row-title">
                      {person.displayName}
                      {person.id === me?.id && <span className="badge">Sina</span>}
                    </span>
                    <span className="row-sub">
                      <span>{person.email}</span>
                    </span>
                  </span>
                  {person.role === 'Service' ? (
                    <span
                      className="role locked-role"
                      title="Teenusekonto on taustateenuse (Worker) jaoks. Ta kontrollib tähtaegu ja tõstab hilinenud tiketite prioriteeti. Selle rolli ei saa muuta."
                    >
                      {roleLabels.Service}
                    </span>
                  ) : (
                    <span
                      className={isMe ? 'role-select locked' : 'role-select'}
                      title={isMe ? 'Sa ei saa enda administraatori rolli ära võtta. Seda saab teha teine administraator.' : undefined}
                    >
                      <select
                        aria-label={`Roll: ${person.displayName}`}
                        value={person.role}
                        disabled={isMe || savingId === person.id}
                        onChange={(e) => void changeRole(person, e.target.value as UserRole)}
                      >
                        {assignableRoles.map((role) => (
                          <option key={role} value={role}>
                            {roleLabels[role]}
                          </option>
                        ))}
                      </select>
                    </span>
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </section>
  )
}
