import type { UserRole } from '../api/types'
import { roleLabels, useAuth } from '../auth/AuthContext'
import { initials } from '../components/initials'
import { useRealtime } from '../realtime/RealtimeContext'

/** What each role can do, in plain words. */
const roleHelp: Record<UserRole, string> = {
  User: 'Saad luua tikette ning näha, muuta ja kommenteerida enda tikette.',
  Support:
    'Näed kõiki tikette, võtad need tööle, muudad nende olekut ja kommenteerid. Uued ja muudetud tiketid ilmuvad sulle reaalajas.',
  Admin:
    'Näed kõiki tikette, muudad ja kustutad neid, muudad olekut ning näed lahendamisaja statistikat. Uued ja muudetud tiketid ilmuvad sulle reaalajas.',
  Service: 'See on taustateenuse konto. Seda kasutab hilinenud tikettide kontroll.',
}

export function ProfilePage() {
  const { user, logout } = useAuth()
  const { connected } = useRealtime()
  if (!user) return null
  const isStaff = user.role === 'Support' || user.role === 'Admin'

  return (
    <div className="profile-page">
      <div className="page-head">
        <h1>Profiil</h1>
      </div>

      <section className="card profile-card">
        <span className="avatar large">{initials(user.displayName)}</span>
        <div className="profile-main">
          <h2>{user.displayName}</h2>
          <p className="muted">{user.email}</p>
          <span className={`role role-${user.role.toLowerCase()}`}>{roleLabels[user.role]}</span>
        </div>
      </section>

      <section className="card">
        <h2>Mida sa teha saad</h2>
        <p>{roleHelp[user.role]}</p>
        {isStaff && (
          <p className="muted">
            Reaalajas uuendused: <strong>{connected ? 'ühendatud' : 'pole ühendatud'}</strong>
          </p>
        )}
      </section>

      <section className="card">
        <h2>Seanss</h2>
        <p className="muted">Seanss kehtib ainult sellel brauseri vahelehel ning lõpeb väljalogimisel.</p>
        <button type="button" className="button secondary" onClick={logout}>
          Logi välja
        </button>
      </section>
    </div>
  )
}
