import { useState } from 'react'
import { ticketsApi } from '../api/ticketsApi'
import type { TicketStatus } from '../api/types'
import { useLookups } from '../api/useLookups'
import { useTicket } from '../api/useTicket'
import { useAuth } from '../auth/AuthContext'
import { PriorityBadge, StatusBadge } from '../components/Badges'
import { ChangeLog } from '../components/ChangeLog'
import { Comments } from '../components/Comments'
import { FormError } from '../components/FormError'
import { TicketForm } from '../components/TicketForm'
import { formatDateTime } from '../components/labels'
import { useNow } from '../components/useNow'
import { useRefreshOnFocus } from '../components/useRefreshOnFocus'
import { useTicketEvents } from '../realtime/useTicketEvents'
import { navigate } from '../router/hashRouter'

export function TicketPage({ id }: { id: number }) {
  const { user } = useAuth()
  const { ticket, comments, log, error, loading, reload } = useTicket(id)
  const { lookups } = useLookups(false)
  const [editing, setEditing] = useState(false)
  const [actionError, setActionError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)
  const now = useNow()

  // A message about this ticket or a return to the page loads the newest state.
  useTicketEvents((event) => {
    if (event.ticket.id === id) reload()
  })
  useRefreshOnFocus(reload)

  const back = (
    <a href="#/" className="back-link">
      ← Tiketid
    </a>
  )

  if (!user) return null
  if (ticket === null) {
    return (
      <section>
        {back}
        <FormError error={error} />
        {error === null && <p className="muted">{loading ? 'Laen…' : ''}</p>}
      </section>
    )
  }

  const isAdmin = user.role === 'Admin'
  const isSupport = user.role === 'Support'
  const isAssignee = ticket.assignee?.id === user.id
  const canTake = isSupport && ticket.status === 'New' && ticket.assignee === null
  const canChangeStatus = (isAdmin || (isSupport && isAssignee)) && ticket.status !== 'New'
  const canEdit =
    ticket.status !== 'Resolved' && ((ticket.status === 'New' && ticket.createdBy.id === user.id) || isAdmin)
  const nextStatus: TicketStatus = ticket.status === 'Resolved' ? 'InProgress' : 'Resolved'

  /** Runs a change on the ticket and shows the new state. */
  async function act(action: () => Promise<unknown>) {
    setBusy(true)
    setActionError(null)
    try {
      await action()
      reload()
    } catch (e) {
      setActionError(e)
    } finally {
      setBusy(false)
    }
  }

  async function remove() {
    if (!window.confirm(`Kas kustutada tiket #${id}? Seda ei saa tagasi võtta.`)) return
    setBusy(true)
    setActionError(null)
    try {
      await ticketsApi.remove(id)
      navigate('/')
    } catch (e) {
      setActionError(e)
      setBusy(false)
    }
  }

  const late = ticket.status !== 'Resolved' && new Date(ticket.deadline).getTime() < now

  return (
    <section className="ticket-page">
      {back}

      <div className="ticket-layout">
        <div className="ticket-main">
          <div className="card">
            {editing && lookups ? (
              <>
                <h1>Muuda tiketit #{ticket.id}</h1>
                <TicketForm
                  categories={lookups.categories}
                  priorities={lookups.priorities}
                  initial={{
                    title: ticket.title,
                    description: ticket.description,
                    categoryId: ticket.category.id,
                    priorityId: ticket.priority.id,
                  }}
                  submitLabel="Salvesta"
                  onSubmit={async (input) => {
                    await ticketsApi.update(id, input)
                    setEditing(false)
                    reload()
                  }}
                  onCancel={() => setEditing(false)}
                />
              </>
            ) : (
              <>
                <div className="ticket-head">
                  <h1>
                    #{ticket.id} {ticket.title}
                  </h1>
                  <div className="ticket-badges">
                    <StatusBadge status={ticket.status} />
                    <PriorityBadge priority={ticket.priority} />
                    {ticket.isOverdue && <span className="badge overdue">Eskaleeritud</span>}
                  </div>
                </div>


                <h2>Kirjeldus</h2>
                <p className="description">{ticket.description}</p>

                <FormError error={actionError} />
                <FormError error={error} />

                <div className="form-actions">
                  {canTake && (
                    <button type="button" className="button" disabled={busy} onClick={() => act(() => ticketsApi.assign(id))}>
                      Võta töösse
                    </button>
                  )}
                  {canChangeStatus && (
                    <button
                      type="button"
                      className="button"
                      disabled={busy}
                      onClick={() => act(() => ticketsApi.changeStatus(id, nextStatus))}
                    >
                      {nextStatus === 'Resolved' ? 'Märgi lahendatuks' : 'Ava uuesti'}
                    </button>
                  )}
                  {canEdit && (
                    <button type="button" className="button secondary" disabled={busy} onClick={() => setEditing(true)}>
                      Muuda
                    </button>
                  )}
                  {isAdmin && (
                    <button type="button" className="button danger" disabled={busy} onClick={remove}>
                      Kustuta
                    </button>
                  )}
                </div>
              </>
            )}
          </div>

          <Comments ticketId={id} comments={comments} onAdded={reload} />
          <ChangeLog entries={log} />
        </div>

        <aside className="card ticket-aside">
          <h2>Üksikasjad</h2>
          <dl className="meta">
            <div>
              <dt>Kategooria</dt>
              <dd>{ticket.category.name}</dd>
            </div>
            <div>
              <dt>Looja</dt>
              <dd>{ticket.createdBy.displayName}</dd>
            </div>
            <div>
              <dt>Tugiisik</dt>
              <dd>{ticket.assignee?.displayName ?? '—'}</dd>
            </div>
            <div>
              <dt>Loodud</dt>
              <dd>{formatDateTime(ticket.createdAt)}</dd>
            </div>
            <div>
              <dt>Tähtaeg</dt>
              <dd className={late ? 'late' : undefined}>{formatDateTime(ticket.deadline)}</dd>
            </div>
            <div>
              <dt>Lahendatud</dt>
              <dd>{ticket.resolvedAt ? formatDateTime(ticket.resolvedAt) : '—'}</dd>
            </div>
          </dl>
        </aside>
      </div>
    </section>
  )
}
