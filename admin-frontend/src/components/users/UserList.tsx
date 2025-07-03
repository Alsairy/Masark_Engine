import React from 'react';
import { Edit, Trash2, Shield, ShieldOff } from 'lucide-react';
import { User } from '../../types/auth';

interface UserListProps {
  users: User[];
  selectedUsers: string[];
  onSelectUser: (userId: string) => void;
  onSelectAll: (selected: boolean) => void;
  onEditUser: (user: User) => void;
  onDeleteUser: (userId: string) => Promise<boolean>;
  onToggleStatus: (userId: string) => Promise<boolean>;
}

const UserList: React.FC<UserListProps> = ({
  users,
  selectedUsers,
  onSelectUser,
  onSelectAll,
  onEditUser,
  onDeleteUser,
  onToggleStatus,
}) => {
  const allSelected = users.length > 0 && selectedUsers.length === users.length;
  const someSelected = selectedUsers.length > 0 && selectedUsers.length < users.length;

  const handleDeleteClick = async (userId: string, username: string) => {
    if (window.confirm(`Are you sure you want to delete user "${username}"? This action cannot be undone.`)) {
      await onDeleteUser(userId);
    }
  };

  const handleToggleStatusClick = async (userId: string, username: string, isActive: boolean) => {
    const action = isActive ? 'deactivate' : 'activate';
    if (window.confirm(`Are you sure you want to ${action} user "${username}"?`)) {
      await onToggleStatus(userId);
    }
  };

  const getRoleBadgeColor = (roles: string[]) => {
    if (roles.includes('ADMIN')) return 'bg-red-100 text-red-800';
    if (roles.includes('MANAGER')) return 'bg-blue-100 text-blue-800';
    if (roles.includes('USER')) return 'bg-green-100 text-green-800';
    return 'bg-gray-100 text-gray-800';
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString();
  };

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left">
              <input
                type="checkbox"
                checked={allSelected}
                ref={input => {
                  if (input) input.indeterminate = someSelected;
                }}
                onChange={(e) => onSelectAll(e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              User
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Role
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Last Login
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Created
            </th>
            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {users.map((user) => (
            <tr key={user.id} className="hover:bg-gray-50">
              <td className="px-6 py-4 whitespace-nowrap">
                <input
                  type="checkbox"
                  checked={selectedUsers.includes(user.id)}
                  onChange={() => onSelectUser(user.id)}
                  className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="flex items-center">
                  <div className="flex-shrink-0 h-10 w-10">
                    <div className="h-10 w-10 rounded-full bg-gray-300 flex items-center justify-center">
                      <span className="text-sm font-medium text-gray-700">
                        {user.full_name?.charAt(0) || user.username.charAt(0).toUpperCase()}
                      </span>
                    </div>
                  </div>
                  <div className="ml-4">
                    <div className="text-sm font-medium text-gray-900">
                      {user.full_name || user.username}
                    </div>
                    <div className="text-sm text-gray-500">{user.email}</div>
                    <div className="text-xs text-gray-400">@{user.username}</div>
                  </div>
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="flex flex-wrap gap-1">
                  {user.roles?.map((role) => (
                    <span
                      key={role}
                      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getRoleBadgeColor(user.roles)}`}
                    >
                      {role}
                    </span>
                  )) || (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                      {user.role || 'USER'}
                    </span>
                  )}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    user.is_active
                      ? 'bg-green-100 text-green-800'
                      : 'bg-red-100 text-red-800'
                  }`}
                >
                  {user.is_active ? 'Active' : 'Inactive'}
                </span>
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {formatDate(user.lastLoginAt)}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {formatDate(user.createdAt)}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <div className="flex items-center justify-end space-x-2">
                  <button
                    onClick={() => onEditUser(user)}
                    className="text-blue-600 hover:text-blue-900 p-1 rounded"
                    title="Edit user"
                  >
                    <Edit className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => handleToggleStatusClick(user.id, user.username, user.is_active)}
                    className={`p-1 rounded ${
                      user.is_active
                        ? 'text-orange-600 hover:text-orange-900'
                        : 'text-green-600 hover:text-green-900'
                    }`}
                    title={user.is_active ? 'Deactivate user' : 'Activate user'}
                  >
                    {user.is_active ? <ShieldOff className="h-4 w-4" /> : <Shield className="h-4 w-4" />}
                  </button>
                  <button
                    onClick={() => handleDeleteClick(user.id, user.username)}
                    className="text-red-600 hover:text-red-900 p-1 rounded"
                    title="Delete user"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {users.length === 0 && (
        <div className="text-center py-12">
          <div className="text-gray-500">No users found</div>
        </div>
      )}
    </div>
  );
};

export default UserList;
