import { useState, type FormEvent } from 'react'
import type { Category, Priority, TicketInput } from '../api/types'
import { FormError } from './FormError'

interface Props {
  categories: Category[]
  priorities: Priority[]
  initial?: TicketInput
  submitLabel: string
  onSubmit: (input: TicketInput) => Promise<void>
  onCancel: () => void
}

/** The fields of a ticket, used for creating and for editing. */
export function TicketForm({ categories, priorities, initial, submitLabel, onSubmit, onCancel }: Props) {
  const [title, setTitle] = useState(initial?.title ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [categoryId, setCategoryId] = useState(initial?.categoryId ?? 0)
  const [priorityId, setPriorityId] = useState(initial?.priorityId ?? 0)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await onSubmit({ title: title.trim(), description: description.trim(), categoryId, priorityId })
    } catch (e) {
      setError(e)
      setBusy(false)
    }
  }

  return (
    <form className="form" onSubmit={submit}>
      <label className="field">
        <span>Pealkiri</span>
        <input
          type="text"
          required
          minLength={5}
          maxLength={100}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
      </label>
      <label className="field">
        <span>Kirjeldus</span>
        <textarea
          required
          minLength={10}
          maxLength={2000}
          rows={6}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </label>
      <div className="form-row">
        <label className="field">
          <span>Kategooria</span>
          <select required value={categoryId} onChange={(e) => setCategoryId(Number(e.target.value))}>
            <option value={0} disabled>
              Vali kategooria
            </option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Prioriteet (määrab tähtaja)</span>
          <select required value={priorityId} onChange={(e) => setPriorityId(Number(e.target.value))}>
            <option value={0} disabled>
              Vali prioriteet
            </option>
            {priorities.map((priority) => (
              <option key={priority.id} value={priority.id}>
                {priority.name} ({priority.deadlineHours} h)
              </option>
            ))}
          </select>
        </label>
      </div>

      <FormError error={error} />

      <div className="form-actions">
        <button type="submit" className="button" disabled={busy}>
          {busy ? 'Salvestan…' : submitLabel}
        </button>
        <button type="button" className="button secondary" onClick={onCancel} disabled={busy}>
          Tühista
        </button>
      </div>
    </form>
  )
}
