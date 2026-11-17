import { useState, type FormEvent } from 'react'
import { FormError } from '../components/FormError'
import { useAuth } from '../auth/AuthContext'
import { navigate } from '../router/hashRouter'

const demoAccounts = [
  { label: 'Kasutaja', email: 'kati.kask@ticketsystem.local' },
  { label: 'Tugiisik', email: 'mari.maasikas@ticketsystem.local' },
  { label: 'Administraator', email: 'admin@ticketsystem.local' },
]

export function LoginPage() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login({ email: email.trim(), password })
      navigate('/')
    } catch (e) {
      setError(e)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit}>
        <h1>Tiketisüsteem</h1>
        <p className="muted">Logi sisse, et näha ja luua tikette.</p>

        <label className="field">
          <span>E-post</span>
          <input
            type="email"
            autoComplete="username"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
        <label className="field">
          <span>Parool</span>
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        <FormError error={error} />

        <button type="submit" className="button" disabled={busy}>
          {busy ? 'Sisselogimine…' : 'Logi sisse'}
        </button>
        <p className="muted center">
          Kontot pole? <a href="#/register">Registreeru</a>
        </p>

        <div className="demo">
          <span className="muted">Katsekontod (parool Password123!):</span>
          <div className="demo-buttons">
            {demoAccounts.map((account) => (
              <button
                key={account.email}
                type="button"
                className="button secondary small"
                onClick={() => {
                  setEmail(account.email)
                  setPassword('Password123!')
                }}
              >
                {account.label}
              </button>
            ))}
          </div>
        </div>
      </form>
    </div>
  )
}
