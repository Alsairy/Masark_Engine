import React from 'react';
import { Shield, Users, Settings, Eye, Edit, Trash2, Download, Upload, Database, BarChart3 } from 'lucide-react';

interface RolePermissionsProps {
  role?: string;
  showAll?: boolean;
}

const RolePermissions: React.FC<RolePermissionsProps> = ({ role, showAll = false }) => {
  const permissionCategories = [
    {
      name: 'User Management',
      icon: Users,
      permissions: [
        { name: 'View Users', roles: ['USER', 'MANAGER', 'ADMIN'], icon: Eye },
        { name: 'Create Users', roles: ['ADMIN'], icon: Edit },
        { name: 'Edit Users', roles: ['MANAGER', 'ADMIN'], icon: Edit },
        { name: 'Delete Users', roles: ['ADMIN'], icon: Trash2 },
        { name: 'Assign Roles', roles: ['ADMIN'], icon: Shield },
      ]
    },
    {
      name: 'Assessment Management',
      icon: BarChart3,
      permissions: [
        { name: 'Take Assessments', roles: ['USER', 'MANAGER', 'ADMIN'], icon: Edit },
        { name: 'View Own Results', roles: ['USER', 'MANAGER', 'ADMIN'], icon: Eye },
        { name: 'View Team Results', roles: ['MANAGER', 'ADMIN'], icon: Eye },
        { name: 'View All Results', roles: ['ADMIN'], icon: Eye },
        { name: 'Export Results', roles: ['MANAGER', 'ADMIN'], icon: Download },
      ]
    },
    {
      name: 'Career Management',
      icon: Settings,
      permissions: [
        { name: 'View Career Matches', roles: ['USER', 'MANAGER', 'ADMIN'], icon: Eye },
        { name: 'Manage Career Data', roles: ['ADMIN'], icon: Edit },
        { name: 'Configure Matching', roles: ['ADMIN'], icon: Settings },
      ]
    },
    {
      name: 'System Administration',
      icon: Database,
      permissions: [
        { name: 'System Configuration', roles: ['ADMIN'], icon: Settings },
        { name: 'API Management', roles: ['ADMIN'], icon: Upload },
        { name: 'Database Access', roles: ['ADMIN'], icon: Database },
        { name: 'Audit Logs', roles: ['ADMIN'], icon: Eye },
        { name: 'Security Settings', roles: ['ADMIN'], icon: Shield },
      ]
    },
    {
      name: 'Reports & Analytics',
      icon: BarChart3,
      permissions: [
        { name: 'Personal Reports', roles: ['USER', 'MANAGER', 'ADMIN'], icon: Eye },
        { name: 'Team Reports', roles: ['MANAGER', 'ADMIN'], icon: Eye },
        { name: 'Organization Reports', roles: ['ADMIN'], icon: Eye },
        { name: 'Export Reports', roles: ['MANAGER', 'ADMIN'], icon: Download },
        { name: 'Custom Reports', roles: ['ADMIN'], icon: Edit },
      ]
    }
  ];


  const hasPermission = (permissionRoles: string[], userRole?: string) => {
    if (!userRole) return false;
    return permissionRoles.includes(userRole);
  };

  const filteredCategories = showAll 
    ? permissionCategories 
    : permissionCategories.map(category => ({
        ...category,
        permissions: category.permissions.filter(permission => 
          role ? hasPermission(permission.roles, role) : true
        )
      })).filter(category => category.permissions.length > 0);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium text-gray-900">
          {role ? `Permissions for ${role} Role` : 'Role Permissions Overview'}
        </h3>
        {role && (
          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
            role === 'ADMIN' ? 'bg-red-100 text-red-800' :
            role === 'MANAGER' ? 'bg-blue-100 text-blue-800' :
            'bg-green-100 text-green-800'
          }`}>
            {role}
          </span>
        )}
      </div>

      <div className="grid gap-6">
        {filteredCategories.map((category) => {
          const CategoryIcon = category.icon;
          
          return (
            <div key={category.name} className="bg-white border border-gray-200 rounded-lg p-6">
              <div className="flex items-center space-x-3 mb-4">
                <div className="p-2 bg-gray-100 rounded-lg">
                  <CategoryIcon className="h-5 w-5 text-gray-600" />
                </div>
                <h4 className="text-md font-medium text-gray-900">{category.name}</h4>
              </div>
              
              <div className="grid gap-3">
                {category.permissions.map((permission) => {
                  const PermissionIcon = permission.icon;
                  const isAllowed = role ? hasPermission(permission.roles, role) : true;
                  
                  return (
                    <div
                      key={permission.name}
                      className={`flex items-center justify-between p-3 rounded-lg border ${
                        isAllowed 
                          ? 'bg-green-50 border-green-200' 
                          : 'bg-gray-50 border-gray-200'
                      }`}
                    >
                      <div className="flex items-center space-x-3">
                        <PermissionIcon className={`h-4 w-4 ${
                          isAllowed ? 'text-green-600' : 'text-gray-400'
                        }`} />
                        <span className={`text-sm font-medium ${
                          isAllowed ? 'text-green-900' : 'text-gray-500'
                        }`}>
                          {permission.name}
                        </span>
                      </div>
                      
                      <div className="flex items-center space-x-2">
                        {showAll && (
                          <div className="flex space-x-1">
                            {permission.roles.map((permRole) => (
                              <span
                                key={permRole}
                                className={`inline-flex items-center px-2 py-1 rounded text-xs font-medium ${
                                  permRole === 'ADMIN' ? 'bg-red-100 text-red-700' :
                                  permRole === 'MANAGER' ? 'bg-blue-100 text-blue-700' :
                                  'bg-green-100 text-green-700'
                                }`}
                              >
                                {permRole}
                              </span>
                            ))}
                          </div>
                        )}
                        
                        <div className={`w-2 h-2 rounded-full ${
                          isAllowed ? 'bg-green-500' : 'bg-gray-300'
                        }`} />
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>

      {role && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h4 className="text-sm font-medium text-blue-900 mb-2">Role Summary</h4>
          <div className="text-sm text-blue-800">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <span className="font-medium">Total Permissions:</span>
                <span className="ml-2">
                  {filteredCategories.reduce((total, cat) => total + cat.permissions.length, 0)}
                </span>
              </div>
              <div>
                <span className="font-medium">Categories:</span>
                <span className="ml-2">{filteredCategories.length}</span>
              </div>
              <div>
                <span className="font-medium">Access Level:</span>
                <span className="ml-2">
                  {role === 'ADMIN' ? 'Full Access' : 
                   role === 'MANAGER' ? 'Management Access' : 'Basic Access'}
                </span>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default RolePermissions;
