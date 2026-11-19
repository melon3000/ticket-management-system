import { useEffect, useState } from 'react'
import { lookupApi } from './ticketsApi'
import type { Category, Priority, UserSummary } from './types'

export interface Lookups {
  categories: Category[]
  priorities: Priority[]
  supportStaff: UserSummary[]
}

interface State {
  lookups: Lookups | null
  error: unknown
}

/** Loads the data of the drop-down lists once. The support staff list is only available to staff. */
export function useLookups(isStaff: boolean): State {
  const [state, setState] = useState<State>({ lookups: null, error: null })

  useEffect(() => {
    let cancelled = false
    Promise.all([
      lookupApi.categories(),
      lookupApi.priorities(),
      isStaff ? lookupApi.supportStaff() : Promise.resolve<UserSummary[]>([]),
    ])
      .then(([categories, priorities, supportStaff]) => {
        if (!cancelled) setState({ lookups: { categories, priorities, supportStaff }, error: null })
      })
      .catch((error: unknown) => {
        if (!cancelled) setState({ lookups: null, error })
      })
    return () => {
      cancelled = true
    }
  }, [isStaff])

  return state
}
