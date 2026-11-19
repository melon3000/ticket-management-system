import { useEffect, useState } from 'react'
import type { TicketQuery, TicketSortField, TicketStatus } from '../api/types'
import { useLookups } from '../api/useLookups'
import { useTickets } from '../api/useTickets'
import { useAuth } from '../auth/AuthContext'
import { PriorityBadge, StatusBadge } from '../components/Badges'
import { FormError } from '../components/FormError'
import { formatDateTime, sortLabels, statusLabels } from '../components/labels'
import { Pager } from '../components/Pager'
import { useNow } from '../components/useNow'

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
  const { data, error, loading } = useTickets(query)
  const now = useNow()

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

  return (
    <section>
      <div className="page-head">
        <h1>{isStaff ? 'Tiketid' : 'Minu tiketid'}</h1>
        <a href="#/tickets/new" className="button">
          Uus tiket
        </a>
      </div>

      <div className="card filters">
        <label className="field grow">
          <span>Otsing</span>
          <input
            type="search"
            maxLength={100}
            placeholder="Pealkiri või kirjeldus"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
          />
        </label>
        <label className="field">
          <span>Staatus</span>
          <select
            value={query.status ?? ''}
            onChange={(e) => setFilter({ status: (e.target.value || undefined) as TicketStatus | undefined })}
          >
            <option value="">Kõik</option>
            {(Object.keys(statusLabels) as TicketStatus[]).map((status) => (
              <option key={status} value={status}>
                {statusLabels[status]}
              </option>
            ))}
          </select>
        </label>
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
          <select
            value={query.sortBy}
            onChange={(e) => setFilter({ sortBy: e.target.value as TicketSortField })}
          >
            {(Object.keys(sortLabels) as TicketSortField[]).map((field) => (
              <option key={field} value={field}>
                {sortLabels[field]}
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
        {filtersActive && (
          <button type="button" className="button secondary" onClick={clearFilters}>
            Tühista filtrid
          </button>
        )}
      </div>

      <FormError error={lookupError} />
      <FormError error={error} />

      <div className={loading ? 'card table-card loading' : 'card table-card'}>
        {data === null ? (
          <p className="muted empty">{loading ? 'Laen…' : 'Tikette ei saanud laadida.'}</p>
        ) : data.items.length === 0 ? (
          <p className="muted empty">{filtersActive ? 'Valitud filtritega tikette ei leitud.' : 'Tikette pole veel.'}</p>
        ) : (
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Pealkiri</th>
                  <th>Staatus</th>
                  <th>Prioriteet</th>
                  <th>Kategooria</th>
                  {isStaff && <th>Looja</th>}
                  <th>Tugiisik</th>
                  <th>Tähtaeg</th>
                  <th>Loodud</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((ticket) => {
                  const late = ticket.status !== 'Resolved' && new Date(ticket.deadline).getTime() < now
                  return (
                    <tr key={ticket.id}>
                      <td className="muted">{ticket.id}</td>
                      <td className="title-cell">
                        <a href={`#/tickets/${ticket.id}`} className="ticket-link">
                          {ticket.title}
                        </a>
                        {ticket.isOverdue && <span className="badge overdue">Eskaleeritud</span>}
                      </td>
                      <td>
                        <StatusBadge status={ticket.status} />
                      </td>
                      <td>
                        <PriorityBadge priority={ticket.priority} />
                      </td>
                      <td>{ticket.category.name}</td>
                      {isStaff && <td>{ticket.createdBy.displayName}</td>}
                      <td>{ticket.assignee?.displayName ?? <span className="muted">—</span>}</td>
                      <td className={late ? 'late' : undefined}>{formatDateTime(ticket.deadline)}</td>
                      <td className="muted">{formatDateTime(ticket.createdAt)}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
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
