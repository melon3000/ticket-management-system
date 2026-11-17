import type { ReactNode } from 'react'
import { usePath } from '../router/hashRouter'

interface Props {
  to: string
  children: ReactNode
}

/** A link between pages. It gets the class "active" on its own page. */
export function NavLink({ to, children }: Props) {
  const path = usePath()
  const active = to === '/' ? path === '/' : path === to || path.startsWith(`${to}/`)
  return (
    <a href={`#${to}`} className={active ? 'nav-link active' : 'nav-link'}>
      {children}
    </a>
  )
}
