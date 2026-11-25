import { useEffect } from 'react'
import { AuthProvider } from './auth/AuthProvider'
import { useAuth } from './auth/AuthContext'
import { Layout } from './components/Layout'
import { LoginPage } from './pages/LoginPage'
import { NewTicketPage } from './pages/NewTicketPage'
import { ProfilePage } from './pages/ProfilePage'
import { RegisterPage } from './pages/RegisterPage'
import { StatisticsPage } from './pages/StatisticsPage'
import { TicketPage } from './pages/TicketPage'
import { TicketsPage } from './pages/TicketsPage'
import { UsersPage } from './pages/UsersPage'
import { RealtimeProvider } from './realtime/RealtimeProvider'
import { matchPath, navigate, usePath } from './router/hashRouter'

function Pages() {
  const { user } = useAuth()
  const path = usePath()
  const onAuthPage = path === '/login' || path === '/register'

  // A signed-in user has no use for the sign-in pages.
  useEffect(() => {
    if (user && onAuthPage) navigate('/')
  }, [user, onAuthPage])

  if (!user) return path === '/register' ? <RegisterPage /> : <LoginPage />

  const ticketRoute = matchPath('/tickets/:id', path)
  let page = <TicketsPage />
  if (path === '/tickets/new') page = <NewTicketPage />
  else if (path === '/profile') page = <ProfilePage />
  else if (path === '/users' && user.role === 'Admin') page = <UsersPage />
  else if (path === '/statistics' && user.role === 'Admin') page = <StatisticsPage />
  else if (ticketRoute && /^\d+$/.test(ticketRoute.id)) page = <TicketPage key={ticketRoute.id} id={Number(ticketRoute.id)} />

  return <Layout>{page}</Layout>
}

function App() {
  return (
    <AuthProvider>
      <RealtimeProvider>
        <Pages />
      </RealtimeProvider>
    </AuthProvider>
  )
}

export default App
