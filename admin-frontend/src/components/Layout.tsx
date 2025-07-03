import React from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useLocalization } from '../contexts/LocalizationContext';
import LanguageSwitcher from './LanguageSwitcher';
import ThemeToggle from './ThemeToggle';
import { 
  LayoutDashboard, 
  Users, 
  Settings, 
  FileText, 
  Briefcase,
  LogOut,
  Menu,
  X,
  Shield,
  Zap,
  Globe
} from 'lucide-react';
import { Button } from '@/components/ui/button';

const Layout: React.FC = () => {
  const { user, logout } = useAuth();
  const { t, isRTL } = useLocalization();
  const location = useLocation();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = React.useState(false);

  const navigation = [
    { name: t('dashboard', 'admin'), href: '/dashboard', icon: LayoutDashboard },
    { name: t('users', 'admin'), href: '/users', icon: Users },
    { name: t('roles', 'admin'), href: '/roles', icon: Shield },
    { name: t('system', 'admin'), href: '/system', icon: Settings },
    { name: t('assessments', 'admin'), href: '/assessments', icon: FileText },
    { name: t('careers', 'admin'), href: '/careers', icon: Briefcase },
    { name: t('reports', 'admin'), href: '/reports', icon: FileText },
    { name: t('api_integration', 'admin'), href: '/api-integration', icon: Zap },
    { name: t('pathways', 'admin'), href: '/pathways', icon: Globe },
  ];

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${isRTL ? 'rtl' : 'ltr'}`} dir={isRTL ? 'rtl' : 'ltr'}>
      <div className="lg:flex">
        <div className={`fixed inset-y-0 z-50 w-64 bg-white dark:bg-gray-800 shadow-lg transform ${
          isRTL ? 'right-0' : 'left-0'
        } ${
          sidebarOpen ? 'translate-x-0' : (isRTL ? 'translate-x-full' : '-translate-x-full')
        } transition-transform duration-300 ease-in-out lg:translate-x-0 lg:static lg:inset-0`}>
          <div className={`flex items-center justify-between h-16 px-6 border-b border-gray-200 dark:border-gray-700 ${isRTL ? 'flex-row-reverse' : ''}`}>
            <h1 className="text-xl font-bold text-gray-900 dark:text-white">
              {t('masark_admin', 'admin')}
            </h1>
            <Button
              variant="ghost"
              size="sm"
              className="lg:hidden"
              onClick={() => setSidebarOpen(false)}
            >
              <X className="h-5 w-5" />
            </Button>
          </div>
          
          <nav className="mt-6">
            <div className="px-3">
              {navigation.map((item) => {
                const isActive = location.pathname === item.href;
                return (
                  <Link
                    key={item.name}
                    to={item.href}
                    className={`group flex items-center px-3 py-2 text-sm font-medium rounded-md mb-1 ${
                      isRTL ? 'flex-row-reverse' : ''
                    } ${
                      isActive
                        ? 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300'
                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white'
                    }`}
                    onClick={() => setSidebarOpen(false)}
                  >
                    <item.icon
                      className={`h-5 w-5 ${isRTL ? 'ml-3' : 'mr-3'} ${
                        isActive ? 'text-blue-500 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500 group-hover:text-gray-500 dark:group-hover:text-gray-400'
                      }`}
                    />
                    {item.name}
                  </Link>
                );
              })}
            </div>
          </nav>
        </div>

        <div className={`flex-1 ${isRTL ? 'lg:pr-64' : 'lg:pl-64'}`}>
          <div className="sticky top-0 z-40 bg-white dark:bg-gray-800 shadow-sm border-b border-gray-200 dark:border-gray-700">
            <div className={`flex items-center justify-between h-16 px-4 sm:px-6 lg:px-8 ${isRTL ? 'flex-row-reverse' : ''}`}>
              <Button
                variant="ghost"
                size="sm"
                className="lg:hidden"
                onClick={() => setSidebarOpen(true)}
              >
                <Menu className="h-5 w-5" />
              </Button>
              
              <div className={`flex items-center space-x-4 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}>
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  {t('welcome', 'admin')}, {user?.firstName || user?.username}
                </span>
                <ThemeToggle />
                <LanguageSwitcher variant="button" />
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleLogout}
                  className={`flex items-center space-x-2 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}
                >
                  <LogOut className="h-4 w-4" />
                  <span>{t('logout', 'auth')}</span>
                </Button>
              </div>
            </div>
          </div>

          <main className="p-6">
            <Outlet />
          </main>
        </div>
      </div>

      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-gray-600 bg-opacity-75 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}
    </div>
  );
};

export default Layout;
