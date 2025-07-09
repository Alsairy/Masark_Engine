import React, { useState, useEffect } from 'react';
import { X, Save, Globe, GraduationCap, AlertCircle, CheckCircle } from 'lucide-react';
import { pathwayService, Pathway, PathwayCreateRequest, PathwayUpdateRequest } from '../../services/pathwayService';

interface PathwayFormProps {
  pathway?: Pathway;
  onSave?: (pathway: Pathway) => void;
  onCancel?: () => void;
  isModal?: boolean;
}

const PathwayForm: React.FC<PathwayFormProps> = ({ 
  pathway, 
  onSave, 
  onCancel, 
  isModal = false 
}) => {
  const [formData, setFormData] = useState<PathwayCreateRequest>({
    nameEn: '',
    nameAr: '',
    descriptionEn: '',
    descriptionAr: '',
    source: 'MOE',
    isActive: true
  });

  const [loading, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (pathway) {
      setFormData({
        nameEn: pathway.nameEn,
        nameAr: pathway.nameAr,
        descriptionEn: pathway.descriptionEn,
        descriptionAr: pathway.descriptionAr,
        source: pathway.source,
        isActive: pathway.isActive
      });
    }
  }, [pathway]);

  const handleInputChange = (field: keyof PathwayCreateRequest, value: string | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
    setError(null);
  };

  const validateForm = (): string | null => {
    if (!formData.nameEn.trim()) {
      return 'English name is required';
    }
    if (!formData.nameAr.trim()) {
      return 'Arabic name is required';
    }
    if (!formData.descriptionEn.trim()) {
      return 'English description is required';
    }
    if (!formData.descriptionAr.trim()) {
      return 'Arabic description is required';
    }
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      setSaving(true);
      setError(null);
      setSuccess(false);

      let result;
      if (pathway) {
        const updateData: PathwayUpdateRequest = {
          ...formData,
          id: pathway.id
        };
        result = await pathwayService.updatePathway(updateData);
      } else {
        result = await pathwayService.createPathway(formData);
      }

      setSuccess(true);
      onSave?.(result.pathway);

      if (!isModal) {
        setFormData({
          nameEn: '',
          nameAr: '',
          descriptionEn: '',
          descriptionAr: '',
          source: 'MOE',
          isActive: true
        });
      }

      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      console.error('Failed to save pathway:', err);
      setError(err.response?.data?.message || 'Failed to save pathway');
    } finally {
      setSaving(false);
    }
  };

  const getSourceInfo = (source: 'MOE' | 'MAWHIBA') => {
    if (source === 'MOE') {
      return {
        icon: Globe,
        color: 'text-blue-600',
        bgColor: 'bg-blue-50',
        borderColor: 'border-blue-200',
        description: 'Ministry of Education pathway (2-4 years, moderate to high difficulty)',
        characteristics: [
          'Duration: 2-4 years',
          'Difficulty: Moderate to High',
          'Prerequisites: High school diploma, minimum GPA requirements',
          'Target: General education population'
        ]
      };
    } else {
      return {
        icon: GraduationCap,
        color: 'text-purple-600',
        bgColor: 'bg-purple-50',
        borderColor: 'border-purple-200',
        description: 'Mawhiba gifted program pathway (1-2 years, high difficulty)',
        characteristics: [
          'Duration: 1-2 years (accelerated)',
          'Difficulty: High (specialized requirements)',
          'Prerequisites: Exceptional academic performance, specialized aptitude tests',
          'Target: Gifted students'
        ]
      };
    }
  };

  const sourceInfo = getSourceInfo(formData.source);
  const SourceIcon = sourceInfo.icon;

  const formContent = (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium text-gray-900">
          {pathway ? 'Edit Pathway' : 'Create New Pathway'}
        </h3>
        {isModal && onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg"
          >
            <X className="h-5 w-5" />
          </button>
        )}
      </div>

      {/* Success Message */}
      {success && (
        <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
          <div className="flex items-center space-x-2">
            <CheckCircle className="h-5 w-5 text-green-600" />
            <span className="text-sm text-green-800 font-medium">
              Pathway {pathway ? 'updated' : 'created'} successfully!
            </span>
          </div>
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <div className="flex items-center space-x-2">
            <AlertCircle className="h-5 w-5 text-red-600" />
            <span className="text-sm text-red-800">{error}</span>
          </div>
        </div>
      )}

      {/* Source Selection */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-3">
          Pathway Source
        </label>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {(['MOE', 'MAWHIBA'] as const).map((source) => {
            const info = getSourceInfo(source);
            const Icon = info.icon;
            const isSelected = formData.source === source;

            return (
              <div
                key={source}
                className={`relative border-2 rounded-lg p-4 cursor-pointer transition-all ${
                  isSelected
                    ? `${info.borderColor} ${info.bgColor}`
                    : 'border-gray-200 hover:border-gray-300 bg-white'
                }`}
                onClick={() => handleInputChange('source', source)}
              >
                {isSelected && (
                  <div className="absolute top-3 right-3">
                    <CheckCircle className={`h-5 w-5 ${info.color}`} />
                  </div>
                )}

                <div className="flex items-center space-x-3 mb-3">
                  <div className={`p-2 rounded-lg ${isSelected ? info.bgColor : 'bg-gray-100'}`}>
                    <Icon className={`h-5 w-5 ${isSelected ? info.color : 'text-gray-600'}`} />
                  </div>
                  <div>
                    <h4 className={`font-medium ${isSelected ? info.color : 'text-gray-900'}`}>
                      {source}
                    </h4>
                    <p className={`text-sm ${isSelected ? info.color : 'text-gray-600'}`}>
                      {info.description}
                    </p>
                  </div>
                </div>

                <ul className="space-y-1">
                  {info.characteristics.map((char, index) => (
                    <li key={index} className={`text-xs flex items-start space-x-2 ${
                      isSelected ? info.color : 'text-gray-600'
                    }`}>
                      <span className="mt-1">•</span>
                      <span>{char}</span>
                    </li>
                  ))}
                </ul>
              </div>
            );
          })}
        </div>
      </div>

      {/* English Name */}
      <div>
        <label htmlFor="nameEn" className="block text-sm font-medium text-gray-700 mb-2">
          English Name *
        </label>
        <input
          type="text"
          id="nameEn"
          value={formData.nameEn}
          onChange={(e) => handleInputChange('nameEn', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          placeholder="Enter pathway name in English"
          required
        />
      </div>

      {/* Arabic Name */}
      <div>
        <label htmlFor="nameAr" className="block text-sm font-medium text-gray-700 mb-2">
          Arabic Name *
        </label>
        <input
          type="text"
          id="nameAr"
          value={formData.nameAr}
          onChange={(e) => handleInputChange('nameAr', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent font-arabic text-right"
          placeholder="أدخل اسم المسار باللغة العربية"
          dir="rtl"
          required
        />
      </div>

      {/* English Description */}
      <div>
        <label htmlFor="descriptionEn" className="block text-sm font-medium text-gray-700 mb-2">
          English Description *
        </label>
        <textarea
          id="descriptionEn"
          value={formData.descriptionEn}
          onChange={(e) => handleInputChange('descriptionEn', e.target.value)}
          rows={4}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          placeholder="Enter detailed description in English"
          required
        />
      </div>

      {/* Arabic Description */}
      <div>
        <label htmlFor="descriptionAr" className="block text-sm font-medium text-gray-700 mb-2">
          Arabic Description *
        </label>
        <textarea
          id="descriptionAr"
          value={formData.descriptionAr}
          onChange={(e) => handleInputChange('descriptionAr', e.target.value)}
          rows={4}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent font-arabic text-right"
          placeholder="أدخل وصفاً مفصلاً باللغة العربية"
          dir="rtl"
          required
        />
      </div>

      {/* Status */}
      <div>
        <label className="flex items-center space-x-3">
          <input
            type="checkbox"
            checked={formData.isActive}
            onChange={(e) => handleInputChange('isActive', e.target.checked)}
            className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
          />
          <span className="text-sm font-medium text-gray-700">
            Active (pathway will be available for career recommendations)
          </span>
        </label>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end space-x-3 pt-6 border-t border-gray-200">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            disabled={loading}
          >
            Cancel
          </button>
        )}
        <button
          type="submit"
          disabled={loading}
          className="inline-flex items-center px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {loading ? (
            <>
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              {pathway ? 'Updating...' : 'Creating...'}
            </>
          ) : (
            <>
              <Save className="h-4 w-4 mr-2" />
              {pathway ? 'Update Pathway' : 'Create Pathway'}
            </>
          )}
        </button>
      </div>
    </form>
  );

  if (isModal) {
    return (
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {formContent}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      {formContent}
    </div>
  );
};

export default PathwayForm;
