import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  X, 
  User, 
  Clock, 
  Globe, 
  Monitor, 
  Brain, 
  BarChart3,
  Download,
  RefreshCw,
  AlertCircle,
  CheckCircle
} from 'lucide-react';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { assessmentApi } from '../../services/api';

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

interface AssessmentSessionDetailsProps {
  session: AssessmentSession;
  onClose: () => void;
}

const AssessmentSessionDetails: React.FC<AssessmentSessionDetailsProps> = ({
  session,
  onClose
}) => {
  const [activeTab, setActiveTab] = useState('overview');

  const { data: sessionResults, isLoading: resultsLoading } = useQuery({
    queryKey: ['session-results', session.id],
    queryFn: () => assessmentApi.getAssessmentResults(session.id, true),
    enabled: !!session.completedAt,
  });

  const { data: sessionState, isLoading: stateLoading } = useQuery({
    queryKey: ['session-state', session.id],
    queryFn: () => assessmentApi.getAssessmentState(session.id),
    refetchInterval: !session.completedAt ? 10000 : false,
  });

  const formatDuration = (startedAt: string, completedAt?: string) => {
    const start = new Date(startedAt);
    const end = completedAt ? new Date(completedAt) : new Date();
    const duration = Math.floor((end.getTime() - start.getTime()) / 1000 / 60);
    
    if (duration < 60) {
      return `${duration} minutes`;
    } else {
      const hours = Math.floor(duration / 60);
      const minutes = duration % 60;
      return `${hours} hours ${minutes} minutes`;
    }
  };

  const getStateColor = (state: string) => {
    switch (state?.toLowerCase()) {
      case 'completed':
        return 'bg-green-100 text-green-800 border-green-200';
      case 'in_progress':
      case 'answering_questions':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'paused':
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'error':
      case 'abandoned':
        return 'bg-red-100 text-red-800 border-red-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getDimensionName = (dimension: string) => {
    const dimensionNames: Record<string, string> = {
      'E': 'Extraversion',
      'I': 'Introversion',
      'S': 'Sensing',
      'N': 'Intuition',
      'T': 'Thinking',
      'F': 'Feeling',
      'J': 'Judging',
      'P': 'Perceiving'
    };
    return dimensionNames[dimension] || dimension;
  };

  const exportSessionData = () => {
    const exportData = {
      session,
      results: sessionResults,
      state: sessionState,
      exportedAt: new Date().toISOString(),
      exportedBy: 'admin'
    };
    
    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `session-${session.id}-details.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] overflow-hidden">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">Assessment Session Details</h2>
            <p className="text-sm text-gray-600 mt-1">
              Session ID: {session.id} • {session.studentName || 'Anonymous'}
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <Button onClick={exportSessionData} variant="outline" size="sm">
              <Download className="h-4 w-4 mr-2" />
              Export
            </Button>
            <Button onClick={onClose} variant="outline" size="sm">
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <div className="p-6 overflow-y-auto max-h-[calc(90vh-120px)]">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
            <TabsList className="grid w-full grid-cols-4">
              <TabsTrigger value="overview">Overview</TabsTrigger>
              <TabsTrigger value="progress">Progress</TabsTrigger>
              <TabsTrigger value="results">Results</TabsTrigger>
              <TabsTrigger value="technical">Technical</TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                      <User className="h-5 w-5" />
                      <span>Student Information</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div>
                      <label className="text-sm font-medium text-gray-600">Name</label>
                      <p className="text-gray-900">{session.studentName || 'Not provided'}</p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-600">Email</label>
                      <p className="text-gray-900">{session.studentEmail || 'Not provided'}</p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-600">Language</label>
                      <p className="text-gray-900">{session.languagePreference?.toUpperCase() || 'EN'}</p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-600">Tenant ID</label>
                      <p className="text-gray-900">{session.tenantId}</p>
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                      <Clock className="h-5 w-5" />
                      <span>Session Timeline</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div>
                      <label className="text-sm font-medium text-gray-600">Started</label>
                      <p className="text-gray-900">
                        {new Date(session.startedAt).toLocaleString()}
                      </p>
                    </div>
                    {session.completedAt && (
                      <div>
                        <label className="text-sm font-medium text-gray-600">Completed</label>
                        <p className="text-gray-900">
                          {new Date(session.completedAt).toLocaleString()}
                        </p>
                      </div>
                    )}
                    <div>
                      <label className="text-sm font-medium text-gray-600">Duration</label>
                      <p className="text-gray-900">
                        {formatDuration(session.startedAt, session.completedAt)}
                      </p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-600">Current State</label>
                      <Badge className={getStateColor(session.currentState)}>
                        {session.currentState}
                      </Badge>
                    </div>
                  </CardContent>
                </Card>
              </div>

              {session.personalityType && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                      <Brain className="h-5 w-5" />
                      <span>Personality Assessment Result</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-center p-6">
                      <div className="text-3xl font-bold text-purple-600 mb-2">
                        {session.personalityType}
                      </div>
                      <p className="text-gray-600">Personality Type</p>
                    </div>
                  </CardContent>
                </Card>
              )}
            </TabsContent>

            <TabsContent value="progress" className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center space-x-2">
                    <BarChart3 className="h-5 w-5" />
                    <span>Assessment Progress</span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {stateLoading ? (
                    <div className="flex items-center justify-center p-8">
                      <RefreshCw className="h-6 w-6 animate-spin text-blue-600" />
                      <span className="ml-2">Loading progress...</span>
                    </div>
                  ) : sessionState ? (
                    <div className="space-y-4">
                      <div>
                        <div className="flex items-center justify-between text-sm text-gray-600 mb-2">
                          <span>Overall Progress</span>
                          <span>{sessionState.progress_percentage || session.progressPercentage}%</span>
                        </div>
                        <div className="w-full bg-gray-200 rounded-full h-3">
                          <div 
                            className="bg-blue-600 h-3 rounded-full transition-all duration-300"
                            style={{ width: `${sessionState.progress_percentage || session.progressPercentage}%` }}
                          />
                        </div>
                      </div>
                      
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
                        <div>
                          <label className="text-sm font-medium text-gray-600">Current State</label>
                          <p className="text-gray-900">{sessionState.current_state}</p>
                        </div>
                        {sessionState.previous_state && (
                          <div>
                            <label className="text-sm font-medium text-gray-600">Previous State</label>
                            <p className="text-gray-900">{sessionState.previous_state}</p>
                          </div>
                        )}
                        <div>
                          <label className="text-sm font-medium text-gray-600">Can Progress</label>
                          <div className="flex items-center space-x-2">
                            {sessionState.can_progress ? (
                              <CheckCircle className="h-4 w-4 text-green-600" />
                            ) : (
                              <AlertCircle className="h-4 w-4 text-red-600" />
                            )}
                            <span>{sessionState.can_progress ? 'Yes' : 'No'}</span>
                          </div>
                        </div>
                        {sessionState.blocking_reason && (
                          <div>
                            <label className="text-sm font-medium text-gray-600">Blocking Reason</label>
                            <p className="text-gray-900">{sessionState.blocking_reason}</p>
                          </div>
                        )}
                      </div>

                      {sessionState.allowed_transitions && sessionState.allowed_transitions.length > 0 && (
                        <div className="mt-6">
                          <label className="text-sm font-medium text-gray-600 mb-2 block">Allowed Transitions</label>
                          <div className="flex flex-wrap gap-2">
                            {sessionState.allowed_transitions.map((transition: string, index: number) => (
                              <Badge key={index} variant="outline">
                                {transition}
                              </Badge>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ) : (
                    <p className="text-gray-600 text-center p-8">No progress data available</p>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="results" className="space-y-6">
              {session.completedAt ? (
                resultsLoading ? (
                  <div className="flex items-center justify-center p-8">
                    <RefreshCw className="h-6 w-6 animate-spin text-blue-600" />
                    <span className="ml-2">Loading results...</span>
                  </div>
                ) : sessionResults ? (
                  <div className="space-y-6">
                    <Card>
                      <CardHeader>
                        <CardTitle>Assessment Results</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="text-center mb-6">
                          <div className="text-4xl font-bold text-purple-600 mb-2">
                            {sessionResults.personality_type}
                          </div>
                          <p className="text-gray-600">Final Personality Type</p>
                        </div>

                        {sessionResults.dimension_scores && (
                          <div>
                            <h4 className="text-lg font-medium text-gray-900 mb-4">Dimension Scores</h4>
                            <div className="space-y-3">
                              {Object.entries(sessionResults.dimension_scores).map(([dimension, score]) => (
                                <div key={dimension}>
                                  <div className="flex items-center justify-between text-sm text-gray-600 mb-1">
                                    <span>{getDimensionName(dimension)}</span>
                                    <span>{typeof score === 'number' ? score.toFixed(1) : String(score)}</span>
                                  </div>
                                  <div className="w-full bg-gray-200 rounded-full h-2">
                                    <div 
                                      className="bg-purple-600 h-2 rounded-full"
                                      style={{ width: `${typeof score === 'number' ? (score / 100) * 100 : 0}%` }}
                                    />
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </CardContent>
                    </Card>

                    {sessionResults.statistics && (
                      <Card>
                        <CardHeader>
                          <CardTitle>Session Statistics</CardTitle>
                        </CardHeader>
                        <CardContent>
                          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                            {Object.entries(sessionResults.statistics).map(([key, value]) => (
                              <div key={key} className="text-center">
                                <div className="text-2xl font-bold text-gray-900">
                                  {typeof value === 'number' ? value.toLocaleString() : String(value)}
                                </div>
                                <div className="text-sm text-gray-600 capitalize">
                                  {key.replace(/_/g, ' ')}
                                </div>
                              </div>
                            ))}
                          </div>
                        </CardContent>
                      </Card>
                    )}
                  </div>
                ) : (
                  <Card>
                    <CardContent className="p-8 text-center">
                      <AlertCircle className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                      <h3 className="text-lg font-medium text-gray-900 mb-2">Results Not Available</h3>
                      <p className="text-gray-600">
                        Assessment results could not be loaded.
                      </p>
                    </CardContent>
                  </Card>
                )
              ) : (
                <Card>
                  <CardContent className="p-8 text-center">
                    <Clock className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                    <h3 className="text-lg font-medium text-gray-900 mb-2">Assessment In Progress</h3>
                    <p className="text-gray-600">
                      Results will be available once the assessment is completed.
                    </p>
                  </CardContent>
                </Card>
              )}
            </TabsContent>

            <TabsContent value="technical" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                      <Monitor className="h-5 w-5" />
                      <span>Technical Details</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div>
                      <label className="text-sm font-medium text-gray-600">Session Token</label>
                      <p className="text-gray-900 font-mono text-sm break-all">
                        {session.sessionToken}
                      </p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-600">Deployment Mode</label>
                      <p className="text-gray-900">{session.deploymentMode}</p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-600">IP Address</label>
                      <p className="text-gray-900">{session.ipAddress || 'Not recorded'}</p>
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                      <Globe className="h-5 w-5" />
                      <span>User Agent</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-gray-900 text-sm break-all">
                      {session.userAgent || 'Not recorded'}
                    </p>
                  </CardContent>
                </Card>
              </div>

              {sessionState?.state_data && (
                <Card>
                  <CardHeader>
                    <CardTitle>State Data</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <pre className="bg-gray-50 p-4 rounded-lg text-sm overflow-x-auto">
                      {JSON.stringify(sessionState.state_data, null, 2)}
                    </pre>
                  </CardContent>
                </Card>
              )}
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  );
};

export default AssessmentSessionDetails;
