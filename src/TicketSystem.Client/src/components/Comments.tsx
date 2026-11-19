import { useState, type FormEvent } from 'react'
import { commentsApi } from '../api/ticketsApi'
import type { TicketComment } from '../api/types'
import { FormError } from './FormError'
import { formatDateTime } from './labels'

interface Props {
  ticketId: number
  comments: TicketComment[]
  /** Called after a comment was added, so the list can be loaded again. */
  onAdded: () => void
}

export function Comments({ ticketId, comments, onAdded }: Props) {
  const [text, setText] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await commentsApi.add(ticketId, text.trim())
      setText('')
      onAdded()
    } catch (e) {
      setError(e)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card">
      <h2>Kommentaarid ({comments.length})</h2>
      {comments.length === 0 ? (
        <p className="muted">Kommentaare pole veel.</p>
      ) : (
        <ul className="comments">
          {comments.map((comment) => (
            <li key={comment.id}>
              <div className="comment-head">
                <strong>{comment.author.displayName}</strong>
                <span className="muted">{formatDateTime(comment.createdAt)}</span>
              </div>
              <p>{comment.text}</p>
            </li>
          ))}
        </ul>
      )}

      <form className="form" onSubmit={submit}>
        <label className="field">
          <span>Lisa kommentaar</span>
          <textarea
            required
            minLength={2}
            maxLength={1000}
            rows={3}
            value={text}
            onChange={(e) => setText(e.target.value)}
          />
        </label>
        <FormError error={error} />
        <div className="form-actions">
          <button type="submit" className="button" disabled={busy || text.trim().length < 2}>
            {busy ? 'Saadan…' : 'Saada'}
          </button>
        </div>
      </form>
    </div>
  )
}
