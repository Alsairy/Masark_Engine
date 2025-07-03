import React, { useState } from 'react';
import { Trash2, Shield, ShieldOff, Download, Mail } from 'lucide-react';

interface BulkActionsProps {
  selectedUsers: string[];
  onBulkAction: (action: string, userIds: string[]) => Promise<void>;
}

const BulkActions: React.FC<BulkActionsProps> = ({ selectedUsers, onBulkAction }) => {
  const [loading, setLoading] = useState<string | null>(null);

  const handleAction = async (action: string) => {
    if (selectedUsers.length === 0) return;

    let confirmMessage = '';
    switch (action) {
      case 'delete':
        confirmMessage = `Are you sure you want to delete ${selectedUsers.length} user(s)? This action cannot be undone.`;
        break;
      case 'activate':
        confirmMessage = `Are you sure you want to activate ${selectedUsers.length} user(s)?`;
        break;
      case 'deactivate':
        confirmMessage = `Are you sure you want to deactivate ${selectedUsers.length} user(s)?`;
        break;
      case 'export':
        confirmMessage = `Export data for ${selectedUsers.length} user(s)?`;
        break;
      case 'email':
        confirmMessage = `Send email to ${selectedUsers.length} user(s)?`;
        break;
      default:
        return;
    }

    if (!window.confirm(confirmMessage)) {
      return;
    }

    setLoading(action);
    try {
      await onBulkAction(action, selectedUsers);
    } catch (error) {
      console.error(`Error performing bulk ${action}:`, error);
    } finally {
      setLoading(null);
    }
  };

  const actions = [
    {
      id: 'activate',
      label: 'Activate',
      icon: Shield,
      className: 'text-green-600 hover:text-green-700',
    },
    {
      id: 'deactivate',
      label: 'Deactivate',
      icon: ShieldOff,
      className: 'text-orange-600 hover:text-orange-700',
    },
    {
      id: 'email',
      label: 'Send Email',
      icon: Mail,
      className: 'text-blue-600 hover:text-blue-700',
    },
    {
      id: 'export',
      label: 'Export',
      icon: Download,
      className: 'text-gray-600 hover:text-gray-700',
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: Trash2,
      className: 'text-red-600 hover:text-red-700',
    },
  ];

  return (
    <div className="bg-blue-50 border-b border-blue-200 px-6 py-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <span className="text-sm font-medium text-blue-900">
            {selectedUsers.length} user(s) selected
          </span>
        </div>
        <div className="flex items-center space-x-2">
          {actions.map((action) => {
            const Icon = action.icon;
            const isLoading = loading === action.id;
            
            return (
              <button
                key={action.id}
                onClick={() => handleAction(action.id)}
                disabled={isLoading}
                className={`flex items-center space-x-1 px-3 py-1 rounded text-sm font-medium transition-colors ${
                  isLoading ? 'opacity-50 cursor-not-allowed' : action.className
                }`}
                title={action.label}
              >
                <Icon className="h-4 w-4" />
                <span>{isLoading ? 'Processing...' : action.label}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default BulkActions;
