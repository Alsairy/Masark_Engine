import React, { useState } from 'react';
import { Shield, Users, Settings, Check, X } from 'lucide-react';
import { User } from '../../types/auth';

interface RoleAssignmentProps {
  user: User;
  onRoleChange: (userId: string, newRole: string) => Promise<boolean>;
  onClose: () => void;
}

const RoleAssignment: React.FC<RoleAssignmentProps> = ({ user, onRoleChange, onClose }) => {
  const [selectedRole, setSelectedRole] = useState(user.role || user.roles?.[0] || 'USER');
  const [loading, setLoading] = useState(false);

  const roles = [
    {
      value: 'USER',
      label: 'User',
      description: 'Basic user access with assessment capabilities',
      icon: Users,
      permissions: [
        'Take personality assessments',
        'View personal results',
        'Access career recommendations',
        'Update profile information'
      ],
      color: 'bg-green-100 text-green-800 border-green-200'
    },
    {
      value: 'MANAGER',
      label: 'Manager',
      description: 'Manage users and view organizational reports',
      icon: Settings,
      permissions: [
        'All User permissions',
        'View team assessments',
        'Generate team reports',
        'Manage department users',
        'Access analytics dashboard'
      ],
      color: 'bg-blue-100 text-blue-800 border-blue-200'
    },
    {
      value: 'ADMIN',
      label: 'Administrator',
      description: 'Full system access and configuration',
      icon: Shield,
      permissions: [
        'All Manager permissions',
        'Manage all users and roles',
        'System configuration',
        'API integration management',
        'Security and audit logs',
        'Database management'
      ],
      color: 'bg-red-100 text-red-800 border-red-200'
    }
  ];

  const handleSave = async () => {
    if (selectedRole === (user.role || user.roles?.[0])) {
      onClose();
      return;
    }

    setLoading(true);
    try {
      const success = await onRoleChange(user.id, selectedRole);
      if (success) {
        onClose();
      }
    } catch (error) {
      console.error('Error updating role:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">Assign Role</h2>
            <p className="text-sm text-gray-600 mt-1">
              Change role for {user.full_name || user.username}
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            <X className="h-6 w-6" />
          </button>
        </div>

        <div className="p-6">
          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Select Role</h3>
            <div className="space-y-4">
              {roles.map((role) => {
                const Icon = role.icon;
                const isSelected = selectedRole === role.value;
                
                return (
                  <div
                    key={role.value}
                    className={`border-2 rounded-lg p-4 cursor-pointer transition-all ${
                      isSelected
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-gray-300'
                    }`}
                    onClick={() => setSelectedRole(role.value)}
                  >
                    <div className="flex items-start space-x-3">
                      <div className="flex-shrink-0">
                        <div className={`p-2 rounded-lg ${role.color}`}>
                          <Icon className="h-5 w-5" />
                        </div>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center space-x-2">
                          <h4 className="text-sm font-medium text-gray-900">
                            {role.label}
                          </h4>
                          {isSelected && (
                            <Check className="h-4 w-4 text-blue-600" />
                          )}
                        </div>
                        <p className="text-sm text-gray-600 mt-1">
                          {role.description}
                        </p>
                        <div className="mt-3">
                          <h5 className="text-xs font-medium text-gray-700 mb-2">
                            Permissions:
                          </h5>
                          <ul className="text-xs text-gray-600 space-y-1">
                            {role.permissions.map((permission, index) => (
                              <li key={index} className="flex items-center space-x-2">
                                <div className="w-1 h-1 bg-gray-400 rounded-full"></div>
                                <span>{permission}</span>
                              </li>
                            ))}
                          </ul>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="bg-gray-50 p-4 rounded-lg">
            <h4 className="text-sm font-medium text-gray-700 mb-2">Current Assignment</h4>
            <div className="text-sm text-gray-600 space-y-1">
              <div>User: {user.full_name || user.username}</div>
              <div>Email: {user.email}</div>
              <div>Current Role: {user.role || user.roles?.[0] || 'USER'}</div>
              <div>New Role: {selectedRole}</div>
              <div>Tenant: {user.tenant_id || 'Default'}</div>
            </div>
          </div>
        </div>

        <div className="flex justify-end space-x-3 p-6 border-t border-gray-200">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={loading}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? 'Updating...' : 'Update Role'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default RoleAssignment;
