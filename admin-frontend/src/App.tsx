import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { LocalizationProvider } from './contexts/LocalizationContext';
import { ThemeProvider } from './contexts/ThemeContext';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import UsersPage from './pages/UsersPage';
import RoleManagement from './pages/RoleManagement';
import SystemPage from './pages/SystemPage';
import AssessmentPage from './pages/AssessmentPage';
import CareersPage from './pages/CareersPage';
import ReportsPage from './pages/ReportsPage';
import ApiIntegrationPage from './pages/ApiIntegrationPage';
import Layout from './components/Layout';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <LocalizationProvider>
          <AuthProvider>
            <Router>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <Layout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<Navigate to="/dashboard" replace />} />
                <Route path="dashboard" element={<DashboardPage />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="roles" element={<RoleManagement />} />
                <Route path="system" element={<SystemPage />} />
                <Route path="assessments" element={<AssessmentPage />} />
                <Route path="careers" element={<CareersPage />} />
                <Route path="reports" element={<ReportsPage />} />
                <Route path="api-integration" element={<ApiIntegrationPage />} />
              </Route>
            </Routes>
          </Router>
        </AuthProvider>
      </LocalizationProvider>
    </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
