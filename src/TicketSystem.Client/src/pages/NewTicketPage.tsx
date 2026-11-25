import { ticketsApi } from '../api/ticketsApi'
import { useLookups } from '../api/useLookups'
import { FormError } from '../components/FormError'
import { TicketForm } from '../components/TicketForm'
import { navigate } from '../router/hashRouter'

export function NewTicketPage() {
  const { lookups, error } = useLookups(false)

  return (
    <section className="narrow-page">
      <a href="#/" className="back-link">
        ← Tiketid
      </a>
      <div className="card">
        <h1>Uus tiket</h1>
        <p className="muted">Tähtaeg arvutatakse valitud prioriteedi järgi loomise hetkest.</p>
        <FormError error={error} />
        {lookups && (
          <TicketForm
            categories={lookups.categories}
            priorities={lookups.priorities}
            submitLabel="Loo tiket"
            onSubmit={async (input) => {
              const ticket = await ticketsApi.create(input)
              navigate(`/tickets/${ticket.id}`)
            }}
            onCancel={() => navigate('/')}
          />
        )}
      </div>
    </section>
  )
}
