import { useEffect, useState } from 'react'
import type { TicketQuery, TicketSortField, TicketStatus } from '../api/types'
import { useLookups } from '../api/useLookups'
import { useTickets } from '../api/useTickets'
import { useAuth } from '../auth/AuthContext'
import { PriorityBadge, StatusBadge } from '../components/Badges'
import { FormError } from '../components/FormError'
import { formatDateTime, sortLabels, statusLabels } from '../components/labels'
import { Pager } from '../components/Pager'
import { avatarTone, initials } from '../components/initials'
import { useNow } from '../components/useNow'
import { useRefreshOnFocus } from '../components/useRefreshOnFocus'
import { useTicketEvents } from '../realtime/useTicketEvents'

const defaultQuery: TicketQuery = { sortBy: 'CreatedAt', descending: true, page: 1, pageSize: 20 }
const searchDelayMs = 350

/** Turns the text of a drop-down list into a number, or undefined for "all". */
function toId(value: string): number | undefined {
  return value === '' ? undefined : Number(value)
}

export function TicketsPage() {
  const { user } = useAuth()
  const isStaff = user?.role === 'Support' || user?.role === 'Admin'
  const [query, setQuery] = useState<TicketQuery>(defaultQuery)
  const [searchText, setSearchText] = useState('')
  const { lookups, error: lookupError } = useLookups(isStaff)
  const { data, error, loading, reload } = useTickets(query)
  const now = useNow()

  // The support staff get a message when a ticket is created or changed. Others refresh when they return.
  useTicketEvents(reload)
  useRefreshOnFocus(reload)

  // The search starts a moment after the last key press, not on every letter.
  useEffect(() => {
    const timer = setTimeout(() => {
      const search = searchText.trim()
      setQuery((current) =>
        (current.search ?? '') === search ? current : { ...current, search: search || undefined, page: 1 },
      )
    }, searchDelayMs)
    return () => clearTimeout(timer)
  }, [searchText])

  /** Changes a filter and goes back to the first page. */
  function setFilter(changes: Partial<TicketQuery>) {
    setQuery((current) => ({ ...current, ...changes, page: 1 }))
  }

  function clearFilters() {
    setSearchText('')
    setQuery((current) => ({ ...defaultQuery, pageSize: current.pageSize }))
  }

  const filtersActive =
    query.status !== undefined ||
    query.priority !== undefined ||
    query.assignee !== undefined ||
    query.categoryId !== undefined ||
    searchText !== ''

  /** Filters that sit inside the "Filtrid" panel. */
  const moreActive = query.priority !== undefined || query.assignee !== undefined || query.categoryId !== undefined

  return (
    <section>
      <div className="page-head">
        <h1>
          {isStaff ? 'Tiketid' : 'Minu tiketid'}
          {data !== null && <span className="count">{data.totalCount}</span>}
        </h1>
        <div className="head-actions">
          <button type="button" className="button secondary" onClick={reload} disabled={loading}>
            Värskenda
          </button>
          <a href="#/tickets/new" className="button">
            Uus tiket
          </a>
        </div>
      </div>

      <div className="toolbar">
        <input
          type="search"
          className="search"
          aria-label="Otsing"
          maxLength={100}
          placeholder="Otsi pealkirja või kirjeldust"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />

        <div className="tabs" role="group" aria-label="Staatus">
          <button
            type="button"
            className={query.status === undefined ? 'tab active' : 'tab'}
            onClick={() => setFilter({ status: undefined })}
          >
            Kõik
          </button>
          {(Object.keys(statusLabels) as TicketStatus[]).map((status) => (
            <button
              key={status}
              type="button"
              className={query.status === status ? 'tab active' : 'tab'}
              onClick={() => setFilter({ status })}
            >
              {statusLabels[status]}
            </button>
          ))}
        </div>

        <details className="more-filters">
          <summary className={moreActive ? 'button secondary small is-on' : 'button secondary small'}>Filtrid</summary>
          <div className="card more-panel">
            <label className="field">
              <span>Prioriteet</span>
              <select value={query.priority ?? ''} onChange={(e) => setFilter({ priority: toId(e.target.value) })}>
                <option value="">Kõik</option>
                {lookups?.priorities.map((priority) => (
                  <option key={priority.id} value={priority.id}>
                    {priority.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>Kategooria</span>
              <select value={query.categoryId ?? ''} onChange={(e) => setFilter({ categoryId: toId(e.target.value) })}>
                <option value="">Kõik</option>
                {lookups?.categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
            {isStaff && (
              <label className="field">
                <span>Tugiisik</span>
                <select value={query.assignee ?? ''} onChange={(e) => setFilter({ assignee: toId(e.target.value) })}>
                  <option value="">Kõik</option>
                  {lookups?.supportStaff.map((person) => (
                    <option key={person.id} value={person.id}>
                      {person.displayName}
                    </option>
                  ))}
                </select>
              </label>
            )}
            <label className="field">
              <span>Sorteeri</span>
              <select value={query.sortBy} onChange={(e) => setFilter({ sortBy: e.target.value as TicketSortField })}>
                {(Object.keys(sortLabels) as TicketSortField[]).map((field) => (
                  <option key={field} value={field}>
                    {sortLabels[field]}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>Ridu lehel</span>
              <select value={query.pageSize} onChange={(e) => setFilter({ pageSize: Number(e.target.value) })}>
                {[10, 20, 50].map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="button"
              className="button secondary"
              title="Muuda sorteerimise suunda"
              onClick={() => setFilter({ descending: !query.descending })}
            >
              {query.descending ? 'Uuemad enne ↓' : 'Vanemad enne ↑'}
            </button>
            {filtersActive && (
              <button type="button" className="button secondary" onClick={clearFilters}>
                Tühista filtrid
              </button>
            )}
          </div>
        </details>
      </div>

      <FormError error={lookupError} />
      <FormError error={error} />

      <div className={loading ? 'card list-card loading' : 'card list-card'}>
        {data === null ? (
          <p className="muted empty">{loading ? 'Laen…' : 'Tikette ei saanud laadida.'}</p>
        ) : data.items.length === 0 ? (
          <p className="muted empty">{filtersActive ? 'Valitud filtritega tikette ei leitud.' : 'Tikette pole veel.'}</p>
        ) : (
          <ul className="ticket-list">
            {data.items.map((ticket) => {
              const late = ticket.status !== 'Resolved' && new Date(ticket.deadline).getTime() < now
              return (
                <li key={ticket.id}>
                  <a href={`#/tickets/${ticket.id}`} className="ticket-row">
                    <span className="row-main">
                      <span className="row-title">
                        {ticket.title}
                        {ticket.isOverdue && <span className="badge overdue">Eskaleeritud</span>}
                      </span>
                      <span className="row-sub">
                        <span>#{ticket.id}</span>
                        <span>{ticket.category.name}</span>
                        {isStaff && <span>{ticket.createdBy.displayName}</span>}
                      </span>
                    </span>
                    <span className="row-priority">
                      <PriorityBadge priority={ticket.priority} />
                    </span>
                    <span className="row-status">
                      <StatusBadge status={ticket.status} />
                    </span>
                    <span className={late ? 'row-deadline late' : 'row-deadline'} title="Tähtaeg">
                      {formatDateTime(ticket.deadline)}
                    </span>
                    {ticket.assignee ? (
                      <span
                        className={`avatar small ${avatarTone(ticket.assignee.id)}`}
                        title={`Tugiisik: ${ticket.assignee.displayName}`}
                      >
                        {initials(ticket.assignee.displayName)}
                      </span>
                    ) : (
                      <span className="avatar small empty-avatar" title="Tugiisikut pole veel" />
                    )}
                  </a>
                </li>
              )
            })}
          </ul>
        )}
      </div>

      {data !== null && data.totalCount > 0 && (
        <Pager
          page={data.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          onChange={(page) => setQuery((current) => ({ ...current, page }))}
        />
      )}
    </section>
  )
}
