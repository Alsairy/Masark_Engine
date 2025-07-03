import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Plus, 
  Edit, 
  Trash2, 
  Search, 
  Save,
  X,
  AlertCircle,
  FileText,
  Languages
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { assessmentApi } from '../../services/api';

interface Question {
  id: number;
  text: string;
  dimension: string;
  order: number;
  options: Array<{
    value: string;
    text: string;
  }>;
  is_reverse_scored: boolean;
  language?: string;
}

interface QuestionFormData {
  text: string;
  textAr?: string;
  dimension: string;
  orderNumber: number;
  optionAText: string;
  optionATextAr?: string;
  optionBText: string;
  optionBTextAr?: string;
  isReverseScored: boolean;
}

const QuestionManagement: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterDimension, setFilterDimension] = useState('all');
  const [selectedLanguage, setSelectedLanguage] = useState('en');
  const [editingQuestion, setEditingQuestion] = useState<Question | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState<QuestionFormData>({
    text: '',
    textAr: '',
    dimension: 'E',
    orderNumber: 1,
    optionAText: '',
    optionATextAr: '',
    optionBText: '',
    optionBTextAr: '',
    isReverseScored: false
  });

  const queryClient = useQueryClient();

  const { data: questions, isLoading, error } = useQuery({
    queryKey: ['questions', selectedLanguage],
    queryFn: () => assessmentApi.getQuestions(selectedLanguage),
  });

  const createQuestionMutation = useMutation({
    mutationFn: (questionData: QuestionFormData) => assessmentApi.createQuestion(questionData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['questions'] });
      setShowAddForm(false);
      resetForm();
    },
  });

  const updateQuestionMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: QuestionFormData }) => 
      assessmentApi.updateQuestion(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['questions'] });
      setEditingQuestion(null);
      resetForm();
    },
  });

  const deleteQuestionMutation = useMutation({
    mutationFn: (id: number) => assessmentApi.deleteQuestion(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['questions'] });
    },
  });

  const dimensions = [
    { value: 'E', label: 'Extraversion', color: 'bg-red-100 text-red-800' },
    { value: 'I', label: 'Introversion', color: 'bg-blue-100 text-blue-800' },
    { value: 'S', label: 'Sensing', color: 'bg-green-100 text-green-800' },
    { value: 'N', label: 'Intuition', color: 'bg-purple-100 text-purple-800' },
    { value: 'T', label: 'Thinking', color: 'bg-orange-100 text-orange-800' },
    { value: 'F', label: 'Feeling', color: 'bg-pink-100 text-pink-800' },
    { value: 'J', label: 'Judging', color: 'bg-indigo-100 text-indigo-800' },
    { value: 'P', label: 'Perceiving', color: 'bg-yellow-100 text-yellow-800' }
  ];

  const resetForm = () => {
    setFormData({
      text: '',
      textAr: '',
      dimension: 'E',
      orderNumber: 1,
      optionAText: '',
      optionATextAr: '',
      optionBText: '',
      optionBTextAr: '',
      isReverseScored: false
    });
  };

  const handleEdit = (question: Question) => {
    setEditingQuestion(question);
    setFormData({
      text: question.text,
      textAr: '', // Would need to fetch from API
      dimension: question.dimension,
      orderNumber: question.order,
      optionAText: question.options[0]?.text || '',
      optionATextAr: '',
      optionBText: question.options[1]?.text || '',
      optionBTextAr: '',
      isReverseScored: question.is_reverse_scored
    });
    setShowAddForm(true);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (editingQuestion) {
      updateQuestionMutation.mutate({ id: editingQuestion.id, data: formData });
    } else {
      createQuestionMutation.mutate(formData);
    }
  };

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this question?')) {
      deleteQuestionMutation.mutate(id);
    }
  };

  const getDimensionInfo = (dimension: string) => {
    return dimensions.find(d => d.value === dimension) || dimensions[0];
  };

  const filteredQuestions = questions?.filter((question: any) => {
    const matchesSearch = !searchTerm || 
      question.text.toLowerCase().includes(searchTerm.toLowerCase()) ||
      question.options.some((opt: any) => opt.text.toLowerCase().includes(searchTerm.toLowerCase()));
    
    const matchesDimension = filterDimension === 'all' || question.dimension === filterDimension;
    
    return matchesSearch && matchesDimension;
  }) || [];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <FileText className="h-8 w-8 animate-pulse mx-auto mb-4 text-blue-600" />
          <p className="text-gray-600">Loading questions...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <AlertCircle className="h-8 w-8 mx-auto mb-4 text-red-600" />
          <p className="text-red-600 mb-4">Error loading questions</p>
          <Button onClick={() => queryClient.invalidateQueries({ queryKey: ['questions'] })} variant="outline">
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Question Management</h2>
          <p className="text-gray-600 mt-1">
            Manage assessment questions and their translations
          </p>
        </div>
        <Button onClick={() => setShowAddForm(true)} className="flex items-center space-x-2">
          <Plus className="h-4 w-4" />
          <span>Add Question</span>
        </Button>
      </div>

      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <div className="relative">
            <Search className="h-4 w-4 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
            <Input
              placeholder="Search questions..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 w-64"
            />
          </div>
          
          <select
            value={filterDimension}
            onChange={(e) => setFilterDimension(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
          >
            <option value="all">All Dimensions</option>
            {dimensions.map(dim => (
              <option key={dim.value} value={dim.value}>
                {dim.label} ({dim.value})
              </option>
            ))}
          </select>

          <select
            value={selectedLanguage}
            onChange={(e) => setSelectedLanguage(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
          >
            <option value="en">English</option>
            <option value="ar">Arabic</option>
          </select>
        </div>

        <div className="flex items-center space-x-2 text-sm text-gray-600">
          <FileText className="h-4 w-4" />
          <span>{filteredQuestions.length} questions</span>
        </div>
      </div>

      {showAddForm && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span>{editingQuestion ? 'Edit Question' : 'Add New Question'}</span>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={() => {
                  setShowAddForm(false);
                  setEditingQuestion(null);
                  resetForm();
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              <Tabs defaultValue="english" className="w-full">
                <TabsList className="grid w-full grid-cols-2">
                  <TabsTrigger value="english">English</TabsTrigger>
                  <TabsTrigger value="arabic">Arabic</TabsTrigger>
                </TabsList>
                
                <TabsContent value="english" className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Question Text (English)
                    </label>
                    <textarea
                      value={formData.text}
                      onChange={(e) => setFormData({ ...formData, text: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      rows={3}
                      required
                    />
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Option A (English)
                      </label>
                      <Input
                        value={formData.optionAText}
                        onChange={(e) => setFormData({ ...formData, optionAText: e.target.value })}
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Option B (English)
                      </label>
                      <Input
                        value={formData.optionBText}
                        onChange={(e) => setFormData({ ...formData, optionBText: e.target.value })}
                        required
                      />
                    </div>
                  </div>
                </TabsContent>

                <TabsContent value="arabic" className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Question Text (Arabic)
                    </label>
                    <textarea
                      value={formData.textAr || ''}
                      onChange={(e) => setFormData({ ...formData, textAr: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      rows={3}
                      dir="rtl"
                    />
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Option A (Arabic)
                      </label>
                      <Input
                        value={formData.optionATextAr || ''}
                        onChange={(e) => setFormData({ ...formData, optionATextAr: e.target.value })}
                        dir="rtl"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Option B (Arabic)
                      </label>
                      <Input
                        value={formData.optionBTextAr || ''}
                        onChange={(e) => setFormData({ ...formData, optionBTextAr: e.target.value })}
                        dir="rtl"
                      />
                    </div>
                  </div>
                </TabsContent>
              </Tabs>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Dimension
                  </label>
                  <select
                    value={formData.dimension}
                    onChange={(e) => setFormData({ ...formData, dimension: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    required
                  >
                    {dimensions.map(dim => (
                      <option key={dim.value} value={dim.value}>
                        {dim.label} ({dim.value})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Order Number
                  </label>
                  <Input
                    type="number"
                    value={formData.orderNumber}
                    onChange={(e) => setFormData({ ...formData, orderNumber: parseInt(e.target.value) })}
                    min={1}
                    required
                  />
                </div>

                <div className="flex items-center space-x-2 pt-8">
                  <input
                    type="checkbox"
                    id="reverseScored"
                    checked={formData.isReverseScored}
                    onChange={(e) => setFormData({ ...formData, isReverseScored: e.target.checked })}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="reverseScored" className="text-sm text-gray-700">
                    Reverse Scored
                  </label>
                </div>
              </div>

              <div className="flex justify-end space-x-3">
                <Button 
                  type="button" 
                  variant="outline"
                  onClick={() => {
                    setShowAddForm(false);
                    setEditingQuestion(null);
                    resetForm();
                  }}
                >
                  Cancel
                </Button>
                <Button 
                  type="submit" 
                  disabled={createQuestionMutation.isPending || updateQuestionMutation.isPending}
                >
                  <Save className="h-4 w-4 mr-2" />
                  {editingQuestion ? 'Update' : 'Create'} Question
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      <div className="space-y-4">
        {filteredQuestions.map((question: any) => {
          const dimensionInfo = getDimensionInfo(question.dimension);
          
          return (
            <Card key={question.id} className="hover:shadow-md transition-shadow">
              <CardContent className="p-6">
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-3 mb-3">
                      <Badge className={`${dimensionInfo.color} border`}>
                        {question.dimension}
                      </Badge>
                      <span className="text-sm text-gray-600">Question #{question.order}</span>
                      {question.is_reverse_scored && (
                        <Badge variant="outline" className="bg-yellow-50 text-yellow-700 border-yellow-200">
                          Reverse Scored
                        </Badge>
                      )}
                      <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200">
                        <Languages className="h-3 w-3 mr-1" />
                        {selectedLanguage.toUpperCase()}
                      </Badge>
                    </div>
                    
                    <h3 className="text-lg font-medium text-gray-900 mb-3">
                      {question.text}
                    </h3>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {question.options.map((option: any) => (
                        <div key={option.value} className="p-3 bg-gray-50 rounded-lg">
                          <div className="flex items-center space-x-2">
                            <div className="flex items-center justify-center w-6 h-6 bg-white border border-gray-300 rounded text-sm font-medium">
                              {option.value}
                            </div>
                            <span className="text-gray-900">{option.text}</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="flex items-center space-x-2 ml-4">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEdit(question)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDelete(question.id)}
                      className="text-red-600 hover:text-red-700 hover:bg-red-50"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {filteredQuestions.length === 0 && (
        <Card>
          <CardContent className="p-8 text-center">
            <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">No questions found</h3>
            <p className="text-gray-600">
              {searchTerm || filterDimension !== 'all' 
                ? 'Try adjusting your search or filter criteria.'
                : 'Get started by adding your first assessment question.'}
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default QuestionManagement;
