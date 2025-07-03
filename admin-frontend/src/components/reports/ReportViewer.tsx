import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  FileText, 
  Eye, 
  Star, 
  MessageSquare, 
  BarChart3,
  User,
  Calendar,
  Globe,
  ChevronDown,
  ChevronRight,
  RefreshCw
} from 'lucide-react';
import { Button } from '../ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Textarea } from '../ui/textarea';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { reportService, ReportElement } from '../../services/reportService';

interface ReportViewerProps {
  reportId?: number;
  assessmentId?: number;
  language?: string;
  onElementRate?: (elementId: number, rating: number, comment?: string) => void;
}

const ReportViewer: React.FC<ReportViewerProps> = ({
  reportId,
  assessmentId,
  language = 'en',
  onElementRate
}) => {
  const [expandedElements, setExpandedElements] = useState<Set<number>>(new Set());
  const [ratings, setRatings] = useState<Record<number, { rating: number; comment: string }>>({});

  const { data: reportData, isLoading: reportLoading } = useQuery({
    queryKey: ['achieveworks-report', reportId, language],
    queryFn: () => reportService.getAchieveWorksReport(reportId!, language),
    enabled: !!reportId,
  });

  const { data: elementsData, isLoading: elementsLoading } = useQuery({
    queryKey: ['report-elements', assessmentId, language],
    queryFn: () => reportService.getReportElements(assessmentId!, language),
    enabled: !!assessmentId,
  });

  const { data: feedbackData } = useQuery({
    queryKey: ['report-feedback', assessmentId],
    queryFn: () => reportService.getReportFeedback(assessmentId!),
    enabled: !!assessmentId,
  });

  const report = reportData?.report || elementsData;
  const elements = report?.report_elements || [];

  const toggleElement = (elementId: number) => {
    const newExpanded = new Set(expandedElements);
    if (newExpanded.has(elementId)) {
      newExpanded.delete(elementId);
    } else {
      newExpanded.add(elementId);
    }
    setExpandedElements(newExpanded);
  };

  const handleRating = (elementId: number, rating: number) => {
    setRatings(prev => ({
      ...prev,
      [elementId]: { ...prev[elementId], rating }
    }));
  };

  const handleComment = (elementId: number, comment: string) => {
    setRatings(prev => ({
      ...prev,
      [elementId]: { ...prev[elementId], comment }
    }));
  };

  const submitRating = (elementId: number) => {
    const rating = ratings[elementId];
    if (rating?.rating && onElementRate) {
      onElementRate(elementId, rating.rating, rating.comment);
    }
  };

  const renderStarRating = (elementId: number, currentRating = 0) => {
    return (
      <div className="flex items-center space-x-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            onClick={() => handleRating(elementId, star)}
            className={`p-1 rounded ${
              star <= (ratings[elementId]?.rating || currentRating)
                ? 'text-yellow-500'
                : 'text-gray-300 hover:text-yellow-400'
            }`}
          >
            <Star className="h-4 w-4 fill-current" />
          </button>
        ))}
        <span className="text-sm text-gray-600 ml-2">
          {ratings[elementId]?.rating || currentRating || 0}/5
        </span>
      </div>
    );
  };

  const renderElement = (element: ReportElement, depth = 0) => {
    const isExpanded = expandedElements.has(element.id);
    const hasChildren = element.child_elements && element.child_elements.length > 0;

    return (
      <div key={element.id} className={`${depth > 0 ? 'ml-6 border-l-2 border-gray-200 pl-4' : ''}`}>
        <Card className="mb-4">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                {hasChildren && (
                  <button
                    onClick={() => toggleElement(element.id)}
                    className="p-1 hover:bg-gray-100 rounded"
                  >
                    {isExpanded ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </button>
                )}
                
                <div className="flex items-center space-x-2">
                  {element.element_type === 'GraphSection' && <BarChart3 className="h-4 w-4 text-blue-600" />}
                  {element.element_type === 'TextBlock' && <FileText className="h-4 w-4 text-green-600" />}
                  {element.element_type === 'Activity' && <MessageSquare className="h-4 w-4 text-purple-600" />}
                  {element.element_type === 'Section' && <Eye className="h-4 w-4 text-gray-600" />}
                  
                  <CardTitle className="text-lg">{element.title}</CardTitle>
                </div>
              </div>
              
              <Badge variant="outline" className="text-xs">
                {element.element_type}
              </Badge>
            </div>
          </CardHeader>
          
          <CardContent>
            {element.content && (
              <div className="mb-4">
                <p className="text-gray-700 whitespace-pre-wrap">{element.content}</p>
              </div>
            )}
            
            {element.graph_data && (
              <div className="mb-4 p-4 bg-gray-50 rounded-lg">
                <h4 className="font-medium mb-2">Graph Data</h4>
                <pre className="text-sm text-gray-600 overflow-x-auto">
                  {JSON.stringify(JSON.parse(element.graph_data), null, 2)}
                </pre>
              </div>
            )}
            
            {element.activity_data && (
              <div className="mb-4 p-4 bg-blue-50 rounded-lg">
                <h4 className="font-medium mb-2">Activity</h4>
                <pre className="text-sm text-gray-600">
                  {JSON.stringify(JSON.parse(element.activity_data), null, 2)}
                </pre>
              </div>
            )}
            
            {element.is_interactive && (
              <div className="mt-4 p-4 border border-gray-200 rounded-lg bg-gray-50">
                <h4 className="font-medium mb-3">Rate this element</h4>
                
                {renderStarRating(element.id)}
                
                <div className="mt-3">
                  <Textarea
                    placeholder="Add your comments (optional)"
                    value={ratings[element.id]?.comment || ''}
                    onChange={(e) => handleComment(element.id, e.target.value)}
                    className="mb-3"
                  />
                  
                  <Button
                    size="sm"
                    onClick={() => submitRating(element.id)}
                    disabled={!ratings[element.id]?.rating}
                  >
                    Submit Rating
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
        
        {hasChildren && isExpanded && (
          <div className="mb-4">
            {element.child_elements!.map(child => renderElement(child, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  if (reportLoading || elementsLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
          <p className="text-gray-600">Loading report...</p>
        </div>
      </div>
    );
  }

  if (!report) {
    return (
      <Card>
        <CardContent className="p-8 text-center">
          <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">No report data</h3>
          <p className="text-gray-600">
            Unable to load report data. Please check the report ID or assessment ID.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {report && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <FileText className="h-5 w-5" />
              <span>Report Overview</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {report.student_name && (
                <div className="flex items-center space-x-2">
                  <User className="h-4 w-4 text-gray-500" />
                  <span className="text-sm text-gray-600">Student:</span>
                  <span className="font-medium">{report.student_name}</span>
                </div>
              )}
              
              {report.personality_type && (
                <div className="flex items-center space-x-2">
                  <Badge>{report.personality_type}</Badge>
                </div>
              )}
              
              {report.generated_at && (
                <div className="flex items-center space-x-2">
                  <Calendar className="h-4 w-4 text-gray-500" />
                  <span className="text-sm">
                    {new Date(report.generated_at).toLocaleString()}
                  </span>
                </div>
              )}
              
              <div className="flex items-center space-x-2">
                <Globe className="h-4 w-4 text-gray-500" />
                <Badge variant="outline">
                  {language === 'en' ? 'English' : 'العربية'}
                </Badge>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <Tabs defaultValue="elements" className="space-y-6">
        <TabsList>
          <TabsTrigger value="elements">Report Elements</TabsTrigger>
          <TabsTrigger value="feedback">Feedback Summary</TabsTrigger>
        </TabsList>

        <TabsContent value="elements" className="space-y-4">
          {elements.length === 0 ? (
            <Card>
              <CardContent className="p-8 text-center">
                <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No elements found</h3>
                <p className="text-gray-600">
                  This report doesn't contain any elements yet.
                </p>
              </CardContent>
            </Card>
          ) : (
            <div>
              {elements.map((element: ReportElement) => renderElement(element))}
            </div>
          )}
        </TabsContent>

        <TabsContent value="feedback" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Feedback Summary</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="text-center">
                  <div className="text-2xl font-bold text-blue-600">
                    {feedbackData?.total_answers || 0}
                  </div>
                  <div className="text-sm text-gray-600">Total Answers</div>
                </div>
                
                <div className="text-center">
                  <div className="text-2xl font-bold text-green-600">
                    {feedbackData?.total_ratings || 0}
                  </div>
                  <div className="text-sm text-gray-600">Total Ratings</div>
                </div>
                
                <div className="text-center">
                  <div className="text-2xl font-bold text-purple-600">
                    {feedbackData?.average_rating?.toFixed(1) || '0.0'}
                  </div>
                  <div className="text-sm text-gray-600">Average Rating</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ReportViewer;
