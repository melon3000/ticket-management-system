import { useState, type FormEvent } from 'react'
import { FormError } from '../components/FormError'
import { useAuth } from '../auth/AuthContext'
import { navigate } from '../router/hashRouter'

export function RegisterPage() {
  const { register } = useAuth()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await register({ displayName: displayName.trim(), email: email.trim(), password })
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
        <h1>Registreeru</h1>
        <p className="muted">Uus konto saab rolli Kasutaja.</p>

        <label className="field">
          <span>Nimi</span>
          <input
            type="text"
            autoComplete="name"
            required
            minLength={2}
            maxLength={100}
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
          />
        </label>
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
          <span>Parool (vähemalt 8 märki)</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            maxLength={100}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        <FormError error={error} />

        <button type="submit" className="button" disabled={busy}>
          {busy ? 'Registreerimine…' : 'Registreeru'}
        </button>
        <p className="muted center">
          Konto juba olemas? <a href="#/login">Logi sisse</a>
        </p>
      </form>
    </div>
  )
}
