import { useEffect } from 'react'
import { AuthProvider } from './auth/AuthProvider'
import { useAuth } from './auth/AuthContext'
import { Layout } from './components/Layout'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { navigate, usePath } from './router/hashRouter'

function Pages() {
  const { user } = useAuth()
  const path = usePath()
  const onAuthPage = path === '/login' || path === '/register'

  // A signed-in user has no use for the sign-in pages.
  useEffect(() => {
    if (user && onAuthPage) navigate('/')
  }, [user, onAuthPage])

  if (!user) return path === '/register' ? <RegisterPage /> : <LoginPage />

  return (
    <Layout>
      <HomePage />
    </Layout>
  )
}

function App() {
  return (
    <AuthProvider>
      <Pages />
    </AuthProvider>
  )
}

export default App
