import React, { useState } from 'react';
import { 
  Eye, 
  Download, 
  Clock, 
  CheckCircle, 
  AlertCircle, 
  Users,
  Calendar,
  Globe,
  Monitor,
  MoreHorizontal
} from 'lucide-react';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Card, CardContent } from '../ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
// import AssessmentSessionDetails from './AssessmentSessionDetails';

interface AssessmentSession {
  id: number;
  sessionToken: string;
  tenantId: number;
  languagePreference: string;
  deploymentMode: string;
  startedAt: string;
  completedAt?: string;
  personalityType?: string;
  currentState: string;
  progressPercentage: number;
  userAgent?: string;
  ipAddress?: string;
  studentName?: string;
  studentEmail?: string;
  dimensionScores?: Record<string, number>;
}

interface AssessmentSessionListProps {
  sessions: AssessmentSession[];
  searchTerm: string;
  filterStatus: string;
}

const AssessmentSessionList: React.FC<AssessmentSessionListProps> = ({
  sessions,
  searchTerm,
  filterStatus
}) => {
  const [selectedSession, setSelectedSession] = useState<AssessmentSession | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  const getStatusColor = (state: string, completedAt?: string) => {
    if (completedAt) {
      return 'bg-green-100 text-green-800 border-green-200';
    }
    
    switch (state?.toLowerCase()) {
      case 'in_progress':
      case 'answering_questions':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'paused':
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'abandoned':
      case 'error':
        return 'bg-red-100 text-red-800 border-red-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getStatusIcon = (state: string, completedAt?: string) => {
    if (completedAt) {
      return <CheckCircle className="h-4 w-4" />;
    }
    
    switch (state?.toLowerCase()) {
      case 'in_progress':
      case 'answering_questions':
        return <Clock className="h-4 w-4" />;
      case 'error':
      case 'abandoned':
        return <AlertCircle className="h-4 w-4" />;
      default:
        return <Users className="h-4 w-4" />;
    }
  };

  const getStatusText = (state: string, completedAt?: string) => {
    if (completedAt) return 'Completed';
    
    switch (state?.toLowerCase()) {
      case 'answering_questions':
        return 'In Progress';
      case 'career_cluster_rating':
        return 'Rating Clusters';
      case 'tie_breaker':
        return 'Tie Breaker';
      case 'calculate_assessment':
        return 'Calculating';
      case 'assessment_rating':
        return 'Rating Assessment';
      case 'paused':
        return 'Paused';
      case 'abandoned':
        return 'Abandoned';
      case 'error':
        return 'Error';
      default:
        return state || 'Unknown';
    }
  };

  const formatDuration = (startedAt: string, completedAt?: string) => {
    const start = new Date(startedAt);
    const end = completedAt ? new Date(completedAt) : new Date();
    const duration = Math.floor((end.getTime() - start.getTime()) / 1000 / 60);
    
    if (duration < 60) {
      return `${duration}m`;
    } else {
      const hours = Math.floor(duration / 60);
      const minutes = duration % 60;
      return `${hours}h ${minutes}m`;
    }
  };

  const filteredSessions = sessions.filter(session => {
    const matchesSearch = !searchTerm || 
      session.studentName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      session.studentEmail?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      session.sessionToken.toLowerCase().includes(searchTerm.toLowerCase()) ||
      session.personalityType?.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesFilter = filterStatus === 'all' || 
      (filterStatus === 'completed' && session.completedAt) ||
      (filterStatus === 'in_progress' && !session.completedAt && session.currentState !== 'abandoned') ||
      (filterStatus === 'abandoned' && session.currentState === 'abandoned') ||
      (filterStatus === 'paused' && session.currentState === 'paused');

    return matchesSearch && matchesFilter;
  });

  const handleViewDetails = (session: AssessmentSession) => {
    setSelectedSession(session);
    setShowDetails(true);
  };

  const handleExportSession = (session: AssessmentSession) => {
    const data = {
      sessionInfo: session,
      exportedAt: new Date().toISOString(),
      exportedBy: 'admin'
    };
    
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `assessment-session-${session.id}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  if (filteredSessions.length === 0) {
    return (
      <Card>
        <CardContent className="p-8 text-center">
          <Users className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">No sessions found</h3>
          <p className="text-gray-600">
            {searchTerm || filterStatus !== 'all' 
              ? 'Try adjusting your search or filter criteria.'
              : 'No assessment sessions have been started yet.'}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      <div className="space-y-4">
        {filteredSessions.map((session) => (
          <Card key={session.id} className="hover:shadow-md transition-shadow">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center space-x-3 mb-2">
                    <h3 className="text-lg font-medium text-gray-900 truncate">
                      {session.studentName || `Session ${session.id}`}
                    </h3>
                    <Badge className={`${getStatusColor(session.currentState, session.completedAt)} flex items-center space-x-1`}>
                      {getStatusIcon(session.currentState, session.completedAt)}
                      <span>{getStatusText(session.currentState, session.completedAt)}</span>
                    </Badge>
                    {session.personalityType && (
                      <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200">
                        {session.personalityType}
                      </Badge>
                    )}
                  </div>
                  
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-sm text-gray-600">
                    <div className="flex items-center space-x-2">
                      <Calendar className="h-4 w-4" />
                      <span>Started: {new Date(session.startedAt).toLocaleDateString()}</span>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Clock className="h-4 w-4" />
                      <span>Duration: {formatDuration(session.startedAt, session.completedAt)}</span>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Globe className="h-4 w-4" />
                      <span>Language: {session.languagePreference?.toUpperCase() || 'EN'}</span>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Monitor className="h-4 w-4" />
                      <span>Mode: {session.deploymentMode || 'STANDARD'}</span>
                    </div>
                  </div>

                  {session.studentEmail && (
                    <div className="mt-2 text-sm text-gray-600">
                      Email: {session.studentEmail}
                    </div>
                  )}

                  {!session.completedAt && session.progressPercentage > 0 && (
                    <div className="mt-3">
                      <div className="flex items-center justify-between text-sm text-gray-600 mb-1">
                        <span>Progress</span>
                        <span>{session.progressPercentage}%</span>
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2">
                        <div 
                          className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                          style={{ width: `${session.progressPercentage}%` }}
                        />
                      </div>
                    </div>
                  )}
                </div>

                <div className="flex items-center space-x-2 ml-4">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleViewDetails(session)}
                  >
                    <Eye className="h-4 w-4 mr-2" />
                    View
                  </Button>
                  
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="outline" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onClick={() => handleViewDetails(session)}>
                        <Eye className="h-4 w-4 mr-2" />
                        View Details
                      </DropdownMenuItem>
                      <DropdownMenuItem onClick={() => handleExportSession(session)}>
                        <Download className="h-4 w-4 mr-2" />
                        Export Data
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {showDetails && selectedSession && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-hidden">
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h2 className="text-xl font-semibold text-gray-900">Session Details</h2>
              <button
                onClick={() => {
                  setShowDetails(false);
                  setSelectedSession(null);
                }}
                className="text-gray-400 hover:text-gray-600"
              >
                ×
              </button>
            </div>
            <div className="p-6">
              <p className="text-gray-600">Session details will be implemented in the next step.</p>
              <div className="mt-4">
                <p><strong>Session ID:</strong> {selectedSession.id}</p>
                <p><strong>Status:</strong> {selectedSession.currentState}</p>
                <p><strong>Progress:</strong> {selectedSession.progressPercentage}%</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default AssessmentSessionList;
