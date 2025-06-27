"""
Assessment Algorithm Validation Service for Masark

This module provides comprehensive validation and testing for the MBTI assessment algorithm,
ensuring it meets psychological assessment standards and provides reliable results.
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

import numpy as np
import statistics
from typing import Dict, List, Tuple, Any
from flask import Flask
from flask_sqlalchemy import SQLAlchemy
from src.services.personality_scoring import PersonalityScoringService
from src.models.masark_models import Question, PersonalityDimension, db
import random
import json

class AssessmentValidationService:
    """Service for validating and testing the assessment algorithm"""
    
    def __init__(self):
        self.scoring_service = PersonalityScoringService()
        
    def generate_test_responses(self, personality_type: str, consistency_level: float = 0.85) -> List[int]:
        """
        Generate test responses for a specific personality type with controlled consistency
        
        Args:
            personality_type: Target MBTI type (e.g., 'INTJ')
            consistency_level: How consistent responses should be (0.0 to 1.0)
            
        Returns:
            List of 36 responses (1-5 scale)
        """
        responses = []
        
        # Define expected preferences for each dimension
        dimension_preferences = {
            'E': personality_type[0] == 'E',  # Extraversion
            'S': personality_type[1] == 'S',  # Sensing
            'T': personality_type[2] == 'T',  # Thinking
            'J': personality_type[3] == 'J'   # Judging
        }
        
        # Question mapping to dimensions (simplified for testing)
        question_dimensions = [
            'E', 'I', 'E', 'I', 'E', 'I', 'E', 'I', 'E',  # Questions 1-9: E/I
            'S', 'N', 'S', 'N', 'S', 'N', 'S', 'N', 'S',  # Questions 10-18: S/N
            'T', 'F', 'T', 'F', 'T', 'F', 'T', 'F', 'T',  # Questions 19-27: T/F
            'J', 'P', 'J', 'P', 'J', 'P', 'J', 'P', 'J'   # Questions 28-36: J/P
        ]
        
        for i, dimension in enumerate(question_dimensions):
            # Determine if this question aligns with the target personality
            if dimension in dimension_preferences:
                prefers_this_dimension = dimension_preferences[dimension]
            else:
                # For opposite dimensions (I, N, F, P)
                opposite_dim = {'I': 'E', 'N': 'S', 'F': 'T', 'P': 'J'}[dimension]
                prefers_this_dimension = not dimension_preferences[opposite_dim]
            
            # Generate response based on preference and consistency
            if random.random() < consistency_level:
                # Consistent response
                if prefers_this_dimension:
                    response = random.choice([4, 5])  # Agree/Strongly Agree
                else:
                    response = random.choice([1, 2])  # Disagree/Strongly Disagree
            else:
                # Inconsistent response (noise)
                response = random.choice([1, 2, 3, 4, 5])
            
            responses.append(response)
        
        return responses
    
    def validate_algorithm_accuracy(self, num_tests: int = 1000) -> Dict[str, Any]:
        """
        Validate the accuracy of the assessment algorithm using synthetic data
        
        Args:
            num_tests: Number of test cases to run
            
        Returns:
            Validation results with accuracy metrics
        """
        print(f"🧪 Running algorithm validation with {num_tests} test cases...")
        
        # All 16 MBTI types
        mbti_types = [
            'INTJ', 'INTP', 'ENTJ', 'ENTP',
            'INFJ', 'INFP', 'ENFJ', 'ENFP',
            'ISTJ', 'ISFJ', 'ESTJ', 'ESFJ',
            'ISTP', 'ISFP', 'ESTP', 'ESFP'
        ]
        
        results = {
            'total_tests': num_tests,
            'correct_predictions': 0,
            'accuracy_by_type': {},
            'dimension_accuracy': {'E/I': 0, 'S/N': 0, 'T/F': 0, 'J/P': 0},
            'consistency_levels_tested': [0.7, 0.8, 0.9, 0.95],
            'accuracy_by_consistency': {}
        }
        
        # Initialize accuracy tracking
        for mbti_type in mbti_types:
            results['accuracy_by_type'][mbti_type] = {'correct': 0, 'total': 0}
        
        for consistency in results['consistency_levels_tested']:
            results['accuracy_by_consistency'][consistency] = {'correct': 0, 'total': 0}
        
        # Run tests
        for test_num in range(num_tests):
            # Randomly select target type and consistency level
            target_type = random.choice(mbti_types)
            consistency = random.choice(results['consistency_levels_tested'])
            
            # Generate test responses
            responses = self.generate_test_responses(target_type, consistency)
            
            # Calculate personality type using the algorithm
            calculated_result = self.scoring_service.calculate_personality_type_from_responses(responses)
            predicted_type = calculated_result.personality_type
            
            # Check accuracy
            is_correct = predicted_type == target_type
            
            # Update results
            results['accuracy_by_type'][target_type]['total'] += 1
            results['accuracy_by_consistency'][consistency]['total'] += 1
            
            if is_correct:
                results['correct_predictions'] += 1
                results['accuracy_by_type'][target_type]['correct'] += 1
                results['accuracy_by_consistency'][consistency]['correct'] += 1
            
            # Check dimension accuracy
            for i, (target_dim, predicted_dim) in enumerate(zip(target_type, predicted_type)):
                if target_dim == predicted_dim:
                    dimension_names = ['E/I', 'S/N', 'T/F', 'J/P']
                    results['dimension_accuracy'][dimension_names[i]] += 1
            
            if (test_num + 1) % 100 == 0:
                print(f"  Completed {test_num + 1}/{num_tests} tests...")
        
        # Calculate final accuracy percentages
        results['overall_accuracy'] = results['correct_predictions'] / num_tests
        
        for mbti_type in mbti_types:
            type_data = results['accuracy_by_type'][mbti_type]
            if type_data['total'] > 0:
                type_data['accuracy'] = type_data['correct'] / type_data['total']
            else:
                type_data['accuracy'] = 0.0
        
        for consistency in results['consistency_levels_tested']:
            cons_data = results['accuracy_by_consistency'][consistency]
            if cons_data['total'] > 0:
                cons_data['accuracy'] = cons_data['correct'] / cons_data['total']
            else:
                cons_data['accuracy'] = 0.0
        
        for dimension in results['dimension_accuracy']:
            results['dimension_accuracy'][dimension] = results['dimension_accuracy'][dimension] / num_tests
        
        return results
    
    def test_edge_cases(self) -> Dict[str, Any]:
        """Test edge cases and boundary conditions"""
        print("🔍 Testing edge cases and boundary conditions...")
        
        edge_cases = {
            'all_neutral': [3] * 36,  # All neutral responses
            'all_strongly_agree': [5] * 36,  # All strongly agree
            'all_strongly_disagree': [1] * 36,  # All strongly disagree
            'alternating_extreme': [1, 5] * 18,  # Alternating extreme responses
            'random_responses': [random.randint(1, 5) for _ in range(36)]  # Random responses
        }
        
        results = {}
        
        for case_name, responses in edge_cases.items():
            try:
                result = self.scoring_service.calculate_personality_type_from_responses(responses)
                results[case_name] = {
                    'success': True,
                    'personality_type': result.personality_type,
                    'preference_strengths': result.preference_strengths,
                    'borderline_dimensions': result.borderline_dimensions
                }
                print(f"  ✅ {case_name}: {result.personality_type}")
            except Exception as e:
                results[case_name] = {
                    'success': False,
                    'error': str(e)
                }
                print(f"  ❌ {case_name}: Error - {str(e)}")
        
        return results
    
    def validate_statistical_properties(self, num_samples: int = 10000) -> Dict[str, Any]:
        """Validate statistical properties of the assessment"""
        print(f"📊 Validating statistical properties with {num_samples} samples...")
        
        # Generate random responses and analyze distribution
        personality_counts = {}
        dimension_distributions = {'E': 0, 'I': 0, 'S': 0, 'N': 0, 'T': 0, 'F': 0, 'J': 0, 'P': 0}
        
        for _ in range(num_samples):
            responses = [random.randint(1, 5) for _ in range(36)]
            result = self.scoring_service.calculate_personality_type_from_responses(responses)
            personality_type = result.personality_type
            
            # Count personality types
            personality_counts[personality_type] = personality_counts.get(personality_type, 0) + 1
            
            # Count dimensions
            for dimension in personality_type:
                dimension_distributions[dimension] += 1
        
        # Calculate statistics
        results = {
            'total_samples': num_samples,
            'unique_types_found': len(personality_counts),
            'personality_distribution': personality_counts,
            'dimension_distributions': dimension_distributions,
            'dimension_balance': {}
        }
        
        # Calculate dimension balance (should be roughly 50/50 for random data)
        for dim_pair in [('E', 'I'), ('S', 'N'), ('T', 'F'), ('J', 'P')]:
            dim1, dim2 = dim_pair
            total = dimension_distributions[dim1] + dimension_distributions[dim2]
            if total > 0:
                results['dimension_balance'][f"{dim1}/{dim2}"] = {
                    dim1: dimension_distributions[dim1] / total,
                    dim2: dimension_distributions[dim2] / total
                }
        
        return results
    
    def performance_benchmark(self, num_assessments: int = 1000) -> Dict[str, Any]:
        """Benchmark the performance of the assessment algorithm"""
        print(f"⚡ Running performance benchmark with {num_assessments} assessments...")
        
        import time
        
        times = []
        
        for i in range(num_assessments):
            responses = [random.randint(1, 5) for _ in range(36)]
            
            start_time = time.time()
            result = self.scoring_service.calculate_personality_type_from_responses(responses)
            end_time = time.time()
            
            times.append(end_time - start_time)
            
            if (i + 1) % 100 == 0:
                print(f"  Completed {i + 1}/{num_assessments} assessments...")
        
        results = {
            'total_assessments': num_assessments,
            'average_time_ms': statistics.mean(times) * 1000,
            'median_time_ms': statistics.median(times) * 1000,
            'min_time_ms': min(times) * 1000,
            'max_time_ms': max(times) * 1000,
            'std_dev_ms': statistics.stdev(times) * 1000 if len(times) > 1 else 0,
            'assessments_per_second': 1 / statistics.mean(times)
        }
        
        return results
    
    def run_comprehensive_validation(self) -> Dict[str, Any]:
        """Run all validation tests and return comprehensive results"""
        print("🚀 Starting comprehensive assessment algorithm validation...")
        
        validation_results = {
            'timestamp': str(np.datetime64('now')),
            'algorithm_accuracy': self.validate_algorithm_accuracy(1000),
            'edge_cases': self.test_edge_cases(),
            'statistical_properties': self.validate_statistical_properties(5000),
            'performance_benchmark': self.performance_benchmark(1000)
        }
        
        # Calculate overall quality score
        accuracy = validation_results['algorithm_accuracy']['overall_accuracy']
        edge_case_success_rate = sum(1 for case in validation_results['edge_cases'].values() if case['success']) / len(validation_results['edge_cases'])
        performance_score = min(1.0, 100 / validation_results['performance_benchmark']['average_time_ms'])  # Normalize to 1.0 for 100ms or better
        
        validation_results['overall_quality_score'] = (accuracy * 0.6 + edge_case_success_rate * 0.2 + performance_score * 0.2)
        
        print("✅ Comprehensive validation completed!")
        return validation_results

def main():
    """Main function for standalone execution"""
    validator = AssessmentValidationService()
    results = validator.run_comprehensive_validation()
    
    print("\n" + "="*80)
    print("📋 ASSESSMENT ALGORITHM VALIDATION RESULTS")
    print("="*80)
    
    print(f"\n🎯 Overall Quality Score: {results['overall_quality_score']:.3f} ({results['overall_quality_score']*100:.1f}%)")
    
    print(f"\n📊 Algorithm Accuracy:")
    print(f"  Overall Accuracy: {results['algorithm_accuracy']['overall_accuracy']:.3f} ({results['algorithm_accuracy']['overall_accuracy']*100:.1f}%)")
    print(f"  Correct Predictions: {results['algorithm_accuracy']['correct_predictions']}/{results['algorithm_accuracy']['total_tests']}")
    
    print(f"\n🔍 Edge Cases:")
    for case_name, case_result in results['edge_cases'].items():
        status = "✅" if case_result['success'] else "❌"
        print(f"  {status} {case_name}")
    
    print(f"\n⚡ Performance:")
    perf = results['performance_benchmark']
    print(f"  Average Time: {perf['average_time_ms']:.2f}ms")
    print(f"  Assessments/Second: {perf['assessments_per_second']:.1f}")
    
    print(f"\n📈 Statistical Properties:")
    stats = results['statistical_properties']
    print(f"  Unique Types Found: {stats['unique_types_found']}/16")
    print(f"  Sample Size: {stats['total_samples']:,}")
    
    # Save results to file
    with open('/home/ubuntu/masark-engine/validation_results.json', 'w') as f:
        json.dump(results, f, indent=2, default=str)
    
    print(f"\n💾 Results saved to validation_results.json")
    print("="*80)

if __name__ == "__main__":
    main()

