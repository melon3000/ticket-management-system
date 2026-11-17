import type { UserRole } from '../api/types'
import { roleLabels, useAuth } from '../auth/AuthContext'

const abilities: Record<UserRole, string[]> = {
  User: ['Tiketi loomine', 'Oma tikettide ja nende ajaloo vaatamine', 'Kommenteerimine oma tikettidel'],
  Support: [
    'Kõikide tikettide vaatamine ja otsimine',
    'Tiketi töösse võtmine',
    'Oma tikettide oleku muutmine',
    'Reaalajas teated uutest ja muutunud tikettidest',
  ],
  Admin: [
    'Kõik tugiisiku õigused',
    'Iga tiketi oleku muutmine ja tiketi kustutamine',
    'Lahendamise keskmise aja statistika',
  ],
  Service: [],
}

export function HomePage() {
  const { user } = useAuth()
  if (!user) return null

  return (
    <section className="card">
      <h1>Tere, {user.displayName}!</h1>
      <p className="muted">
        Sisse logitud rollis <strong>{roleLabels[user.role]}</strong> ({user.email}).
      </p>
      <h2>Sinu õigused</h2>
      <ul>
        {abilities[user.role].map((ability) => (
          <li key={ability}>{ability}</li>
        ))}
      </ul>
    </section>
  )
}
