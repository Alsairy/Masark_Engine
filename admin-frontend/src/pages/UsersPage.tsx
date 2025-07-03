import React, { useState, useEffect } from 'react';
import { Plus, Search, Filter, Download } from 'lucide-react';
import { userApi } from '../services/api';
import { User } from '../types/auth';
import UserList from '../components/users/UserList';
import UserForm from '../components/users/UserForm';
import UserEditModal from '../components/users/UserEditModal';
import BulkActions from '../components/users/BulkActions';

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [usersPerPage] = useState(10);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const userData = await userApi.getUsers();
      setUsers(userData);
      setError(null);
    } catch (err) {
      setError('Failed to load users');
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = async (userData: any) => {
    try {
      const newUser = await userApi.createUser(userData);
      setUsers([...users, newUser]);
      setShowCreateForm(false);
      return true;
    } catch (err) {
      console.error('Error creating user:', err);
      return false;
    }
  };

  const handleUpdateUser = async (userData: any) => {
    try {
      const updatedUser = await userApi.updateUser(userData);
      setUsers(users.map(user => user.id === updatedUser.id ? updatedUser : user));
      setEditingUser(null);
      return true;
    } catch (err) {
      console.error('Error updating user:', err);
      return false;
    }
  };

  const handleDeleteUser = async (userId: string) => {
    try {
      await userApi.deleteUser(userId);
      setUsers(users.filter(user => user.id !== userId));
      return true;
    } catch (err) {
      console.error('Error deleting user:', err);
      return false;
    }
  };

  const handleToggleUserStatus = async (userId: string) => {
    try {
      await userApi.deactivateUser(userId);
      await loadUsers();
      return true;
    } catch (err) {
      console.error('Error toggling user status:', err);
      return false;
    }
  };

  const filteredUsers = users.filter(user =>
    user.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
    user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
    user.full_name?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const indexOfLastUser = currentPage * usersPerPage;
  const indexOfFirstUser = indexOfLastUser - usersPerPage;
  const currentUsers = filteredUsers.slice(indexOfFirstUser, indexOfLastUser);
  const totalPages = Math.ceil(filteredUsers.length / usersPerPage);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
          <p className="text-gray-600">Manage system users, roles, and permissions</p>
        </div>
        <button
          onClick={() => setShowCreateForm(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg flex items-center space-x-2"
        >
          <Plus className="h-4 w-4" />
          <span>Add User</span>
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {error}
        </div>
      )}

      <div className="bg-white rounded-lg shadow">
        <div className="p-6 border-b border-gray-200">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <input
                type="text"
                placeholder="Search users..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            <div className="flex space-x-2">
              <button className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center space-x-2">
                <Filter className="h-4 w-4" />
                <span>Filter</span>
              </button>
              <button className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center space-x-2">
                <Download className="h-4 w-4" />
                <span>Export</span>
              </button>
            </div>
          </div>
        </div>

        {selectedUsers.length > 0 && (
          <BulkActions
            selectedUsers={selectedUsers}
            onBulkAction={async (action) => {
              console.log('Bulk action:', action, selectedUsers);
              setSelectedUsers([]);
            }}
          />
        )}

        <UserList
          users={currentUsers}
          selectedUsers={selectedUsers}
          onSelectUser={(userId) => {
            setSelectedUsers(prev =>
              prev.includes(userId)
                ? prev.filter(id => id !== userId)
                : [...prev, userId]
            );
          }}
          onSelectAll={(selected) => {
            setSelectedUsers(selected ? currentUsers.map(u => u.id) : []);
          }}
          onEditUser={setEditingUser}
          onDeleteUser={handleDeleteUser}
          onToggleStatus={handleToggleUserStatus}
        />

        {totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-200">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700">
                Showing {indexOfFirstUser + 1} to {Math.min(indexOfLastUser, filteredUsers.length)} of {filteredUsers.length} users
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                  disabled={currentPage === 1}
                  className="px-3 py-1 border border-gray-300 rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  Previous
                </button>
                {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
                  <button
                    key={page}
                    onClick={() => setCurrentPage(page)}
                    className={`px-3 py-1 border rounded ${
                      currentPage === page
                        ? 'bg-blue-600 text-white border-blue-600'
                        : 'border-gray-300 hover:bg-gray-50'
                    }`}
                  >
                    {page}
                  </button>
                ))}
                <button
                  onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                  disabled={currentPage === totalPages}
                  className="px-3 py-1 border border-gray-300 rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {showCreateForm && (
        <UserForm
          onSubmit={handleCreateUser}
          onCancel={() => setShowCreateForm(false)}
        />
      )}

      {editingUser && (
        <UserEditModal
          user={editingUser}
          onSubmit={handleUpdateUser}
          onCancel={() => setEditingUser(null)}
        />
      )}
    </div>
  );
};

export default UsersPage;
