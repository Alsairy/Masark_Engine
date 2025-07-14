import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  FileText, 
  Download, 
  Settings, 
  Globe, 
  User,
  Calendar,
  AlertCircle,
  CheckCircle
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Alert, AlertDescription } from '../ui/alert';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';
import { Checkbox } from '../ui/checkbox';
import { reportService, GenerateReportRequest } from '../../services/reportService';

interface GeneratedReport {
  filename: string;
  student_name: string;
  personality_type: string;
  generated_at: string;
}

interface ReportGeneratorProps {
  onReportGenerated?: (report: GeneratedReport) => void;
  defaultSessionToken?: string;
}

const ReportGenerator: React.FC<ReportGeneratorProps> = ({
  onReportGenerated,
  defaultSessionToken = ''
}) => {
  const [sessionToken, setSessionToken] = useState(defaultSessionToken);
  const [language, setLanguage] = useState('en');
  const [reportType, setReportType] = useState('comprehensive');
  const [includeCareerDetails, setIncludeCareerDetails] = useState(true);
  const [generatedReport, setGeneratedReport] = useState<GeneratedReport | null>(null);

  const queryClient = useQueryClient();

  const generateMutation = useMutation({
    mutationFn: (request: GenerateReportRequest) => reportService.generateReport(request),
    onSuccess: (data) => {
      setGeneratedReport(data.report);
      onReportGenerated?.(data.report);
      queryClient.invalidateQueries({ queryKey: ['reports'] });
    },
  });

  const downloadMutation = useMutation({
    mutationFn: (filename: string) => reportService.downloadReport(filename),
    onSuccess: (blob, filename) => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
  });

  const handleGenerate = () => {
    if (!sessionToken.trim()) {
      return;
    }

    generateMutation.mutate({
      sessionToken: sessionToken.trim(),
      language,
      reportType,
      includeCareerDetails
    });
  };

  const handleDownload = () => {
    if (generatedReport?.filename) {
      downloadMutation.mutate(generatedReport.filename);
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center space-x-2">
            <FileText className="h-5 w-5" />
            <span>Generate Report</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium text-gray-700 mb-2 block">
                  Session Token *
                </label>
                <Input
                  placeholder="Enter assessment session token"
                  value={sessionToken}
                  onChange={(e) => setSessionToken(e.target.value)}
                  className="w-full"
                />
              </div>

              <div>
                <label className="text-sm font-medium text-gray-700 mb-2 block">
                  <Globe className="h-4 w-4 inline mr-1" />
                  Language
                </label>
                <Select value={language} onValueChange={setLanguage}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="en">English</SelectItem>
                    <SelectItem value="ar">العربية</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div>
                <label className="text-sm font-medium text-gray-700 mb-2 block">
                  <Settings className="h-4 w-4 inline mr-1" />
                  Report Type
                </label>
                <Select value={reportType} onValueChange={setReportType}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="comprehensive">Comprehensive Report</SelectItem>
                    <SelectItem value="summary">Summary Report</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="flex items-center space-x-2">
                <Checkbox
                  id="includeCareerDetails"
                  checked={includeCareerDetails}
                  onCheckedChange={(checked) => setIncludeCareerDetails(checked as boolean)}
                />
                <label
                  htmlFor="includeCareerDetails"
                  className="text-sm font-medium text-gray-700"
                >
                  Include Career Details
                </label>
              </div>
            </div>

            <div className="space-y-4">
              <div className="bg-gray-50 p-4 rounded-lg">
                <h4 className="font-medium text-gray-900 mb-2">Report Configuration</h4>
                <div className="space-y-2 text-sm text-gray-600">
                  <div className="flex justify-between">
                    <span>Language:</span>
                    <Badge variant="outline">{language === 'en' ? 'English' : 'العربية'}</Badge>
                  </div>
                  <div className="flex justify-between">
                    <span>Type:</span>
                    <Badge variant="outline">{reportType}</Badge>
                  </div>
                  <div className="flex justify-between">
                    <span>Career Details:</span>
                    <Badge variant={includeCareerDetails ? "default" : "secondary"}>
                      {includeCareerDetails ? 'Included' : 'Excluded'}
                    </Badge>
                  </div>
                </div>
              </div>

              <Button
                onClick={handleGenerate}
                disabled={!sessionToken.trim() || generateMutation.isPending}
                className="w-full"
              >
                {generateMutation.isPending ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Generating...
                  </>
                ) : (
                  <>
                    <FileText className="h-4 w-4 mr-2" />
                    Generate Report
                  </>
                )}
              </Button>
            </div>
          </div>

          {generateMutation.isError && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>
                Failed to generate report. Please check the session token and try again.
              </AlertDescription>
            </Alert>
          )}

          {generatedReport && (
            <Alert>
              <CheckCircle className="h-4 w-4" />
              <AlertDescription>
                Report generated successfully! You can now download it.
              </AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>

      {generatedReport && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Download className="h-5 w-5" />
              <span>Generated Report</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-3">
                <div className="flex items-center space-x-2">
                  <User className="h-4 w-4 text-gray-500" />
                  <span className="text-sm text-gray-600">Student:</span>
                  <span className="font-medium">{generatedReport.student_name}</span>
                </div>
                
                <div className="flex items-center space-x-2">
                  <FileText className="h-4 w-4 text-gray-500" />
                  <span className="text-sm text-gray-600">Personality Type:</span>
                  <Badge>{generatedReport.personality_type}</Badge>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Calendar className="h-4 w-4 text-gray-500" />
                  <span className="text-sm text-gray-600">Generated:</span>
                  <span className="text-sm">
                    {new Date(generatedReport.generated_at).toLocaleString()}
                  </span>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Settings className="h-4 w-4 text-gray-500" />
                  <span className="text-sm text-gray-600">File:</span>
                  <span className="text-sm font-mono text-gray-800">
                    {generatedReport.filename}
                  </span>
                </div>
              </div>

              <div className="flex flex-col justify-center">
                <Button
                  onClick={handleDownload}
                  disabled={downloadMutation.isPending}
                  className="w-full"
                >
                  {downloadMutation.isPending ? (
                    <>
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                      Downloading...
                    </>
                  ) : (
                    <>
                      <Download className="h-4 w-4 mr-2" />
                      Download Report
                    </>
                  )}
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default ReportGenerator;
