import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  FileText, 
  Download, 
  Trash2, 
  Search, 
  Filter,
  Calendar,
  User,
  Globe,
  RefreshCw,
  AlertCircle
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent } from '../ui/card';
import { Badge } from '../ui/badge';
import { Alert, AlertDescription } from '../ui/alert';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '../ui/alert-dialog';
import { reportService, Report } from '../../services/reportService';

interface ReportListProps {
  limit?: number;
  onReportSelect?: (report: Report) => void;
  showActions?: boolean;
}

const ReportList: React.FC<ReportListProps> = ({
  limit = 50,
  onReportSelect,
  showActions = true
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedLanguage, setSelectedLanguage] = useState('all');
  const [selectedType, setSelectedType] = useState('all');
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [reportToDelete, setReportToDelete] = useState<Report | null>(null);

  const queryClient = useQueryClient();

  const { data: reportsData, isLoading, error, refetch } = useQuery({
    queryKey: ['reports', limit],
    queryFn: () => reportService.listReports(limit),
    refetchInterval: 30000,
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

  const deleteMutation = useMutation({
    mutationFn: (filename: string) => reportService.deleteReport(filename),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reports'] });
      setDeleteDialogOpen(false);
      setReportToDelete(null);
    },
  });

  const reports = reportsData?.reports || [];

  const filteredReports = reports.filter((report: Report) => {
    const matchesSearch = 
      report.filename.toLowerCase().includes(searchTerm.toLowerCase()) ||
      report.student_name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      report.personality_type.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesLanguage = selectedLanguage === 'all' || report.language === selectedLanguage;
    const matchesType = selectedType === 'all' || report.report_type === selectedType;
    
    return matchesSearch && matchesLanguage && matchesType;
  });

  const handleDownload = (report: Report) => {
    downloadMutation.mutate(report.filename);
  };

  const handleDelete = (report: Report) => {
    setReportToDelete(report);
    setDeleteDialogOpen(true);
  };

  const confirmDelete = () => {
    if (reportToDelete) {
      deleteMutation.mutate(reportToDelete.filename);
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getReportTypeColor = (type: string) => {
    switch (type) {
      case 'comprehensive':
        return 'bg-blue-100 text-blue-800';
      case 'summary':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[...Array(5)].map((_, i) => (
          <Card key={i} className="animate-pulse">
            <CardContent className="p-6">
              <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-gray-200 rounded w-1/2 mb-4"></div>
              <div className="flex space-x-2">
                <div className="h-6 bg-gray-200 rounded w-20"></div>
                <div className="h-6 bg-gray-200 rounded w-16"></div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertDescription>
          Failed to load reports. Please try again.
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4 flex-1">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              placeholder="Search reports..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
            />
          </div>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <Globe className="h-4 w-4 mr-2" />
                Language: {selectedLanguage === 'all' ? 'All' : selectedLanguage}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem onClick={() => setSelectedLanguage('all')}>
                All Languages
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setSelectedLanguage('en')}>
                English
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setSelectedLanguage('ar')}>
                العربية
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <Filter className="h-4 w-4 mr-2" />
                Type: {selectedType === 'all' ? 'All' : selectedType}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem onClick={() => setSelectedType('all')}>
                All Types
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setSelectedType('comprehensive')}>
                Comprehensive
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setSelectedType('summary')}>
                Summary
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <Button variant="outline" size="sm" onClick={() => refetch()}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Refresh
        </Button>
      </div>

      {filteredReports.length === 0 ? (
        <Card>
          <CardContent className="p-8 text-center">
            <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">No reports found</h3>
            <p className="text-gray-600">
              {searchTerm || selectedLanguage !== 'all' || selectedType !== 'all'
                ? 'Try adjusting your search or filter criteria.'
                : 'No reports have been generated yet.'}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {filteredReports.map((report: Report) => (
            <Card 
              key={report.filename} 
              className={`hover:shadow-md transition-shadow ${
                onReportSelect ? 'cursor-pointer' : ''
              }`}
              onClick={() => onReportSelect?.(report)}
            >
              <CardContent className="p-6">
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-2 mb-2">
                      <FileText className="h-5 w-5 text-blue-600" />
                      <h3 className="text-lg font-semibold text-gray-900 truncate">
                        {report.filename}
                      </h3>
                    </div>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
                      <div className="flex items-center space-x-2">
                        <User className="h-4 w-4 text-gray-500" />
                        <span className="text-sm text-gray-600">Student:</span>
                        <span className="text-sm font-medium">{report.student_name}</span>
                      </div>
                      
                      <div className="flex items-center space-x-2">
                        <Badge>{report.personality_type}</Badge>
                      </div>
                      
                      <div className="flex items-center space-x-2">
                        <Calendar className="h-4 w-4 text-gray-500" />
                        <span className="text-sm text-gray-600">
                          {new Date(report.generated_at).toLocaleDateString()}
                        </span>
                      </div>
                      
                      <div className="flex items-center space-x-2">
                        <span className="text-sm text-gray-600">Size:</span>
                        <span className="text-sm">{formatFileSize(report.file_size_bytes)}</span>
                      </div>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Badge className={getReportTypeColor(report.report_type)}>
                        {report.report_type}
                      </Badge>
                      
                      <Badge variant="outline">
                        {report.language === 'en' ? 'English' : 'العربية'}
                      </Badge>
                      
                      <span className="text-xs text-gray-500 font-mono">
                        {report.session_token}
                      </span>
                    </div>
                  </div>
                  
                  {showActions && (
                    <div className="flex items-center space-x-2 ml-4">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDownload(report);
                        }}
                        disabled={downloadMutation.isPending}
                      >
                        <Download className="h-4 w-4" />
                      </Button>
                      
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDelete(report);
                        }}
                        disabled={deleteMutation.isPending}
                        className="text-red-600 hover:text-red-700"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Report</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{reportToDelete?.filename}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-red-600 hover:bg-red-700"
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
};

export default ReportList;
