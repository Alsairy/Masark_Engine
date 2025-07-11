import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const sessionCreationTime = new Trend('session_creation_time');
const assessmentCompletionTime = new Trend('assessment_completion_time');
const errorRate = new Rate('error_rate');

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up to 10 users
    { duration: '1m', target: 50 },    // Stay at 50 users
    { duration: '30s', target: 100 },  // Ramp up to 100 users
    { duration: '2m', target: 100 },   // Stay at 100 users
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    session_creation_time: ['p(95)<100'], // 95% of session creations must be below 100ms
    assessment_completion_time: ['p(95)<2000'], // 95% of assessments must complete below 2s
    error_rate: ['rate<0.05'], // Error rate must be below 5%
    http_req_failed: ['rate<0.05'], // Failed requests must be below 5%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

const testUsers = [
  { email: 'test1@masark.com', password: 'TestPassword123!' },
  { email: 'test2@masark.com', password: 'TestPassword123!' },
  { email: 'test3@masark.com', password: 'TestPassword123!' },
];

const sampleAnswers = [
  { questionId: 1, selectedOption: 'A', strength: 'STRONG' },
  { questionId: 2, selectedOption: 'B', strength: 'MODERATE' },
  { questionId: 3, selectedOption: 'C', strength: 'WEAK' },
  { questionId: 4, selectedOption: 'A', strength: 'STRONG' },
  { questionId: 5, selectedOption: 'B', strength: 'MODERATE' },
];

export function setup() {
  const healthResponse = http.get(`${BASE_URL}/health`);
  check(healthResponse, {
    'health check status is 200': (r) => r.status === 200,
  });
  
  console.log('Performance test setup completed');
  return { baseUrl: BASE_URL };
}

export default function (data) {
  const user = testUsers[Math.floor(Math.random() * testUsers.length)];
  
  const authPayload = JSON.stringify({
    email: user.email,
    password: user.password,
  });
  
  const authHeaders = {
    'Content-Type': 'application/json',
  };
  
  const authResponse = http.post(`${BASE_URL}/api/auth/login`, authPayload, {
    headers: authHeaders,
  });
  
  const authSuccess = check(authResponse, {
    'authentication successful': (r) => r.status === 200,
    'auth response has token': (r) => r.json('token') !== undefined,
  });
  
  if (!authSuccess) {
    errorRate.add(1);
    return;
  }
  
  const token = authResponse.json('token');
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
  
  const sessionStart = Date.now();
  const sessionPayload = JSON.stringify({
    languagePreference: Math.random() > 0.5 ? 'en' : 'ar',
    tenantId: 'default',
  });
  
  const sessionResponse = http.post(`${BASE_URL}/api/assessment/start`, sessionPayload, {
    headers: headers,
  });
  
  const sessionCreated = check(sessionResponse, {
    'session creation successful': (r) => r.status === 200,
    'session has token': (r) => r.json('sessionToken') !== undefined,
  });
  
  if (sessionCreated) {
    const sessionTime = Date.now() - sessionStart;
    sessionCreationTime.add(sessionTime);
    
    check(sessionTime, {
      'session creation under 100ms': (time) => time < 100,
    });
  } else {
    errorRate.add(1);
    return;
  }
  
  const sessionToken = sessionResponse.json('sessionToken');
  
  const questionsResponse = http.get(`${BASE_URL}/api/assessment/questions?language=${Math.random() > 0.5 ? 'en' : 'ar'}`, {
    headers: headers,
  });
  
  check(questionsResponse, {
    'questions retrieved successfully': (r) => r.status === 200,
    'questions array not empty': (r) => r.json('questions') && r.json('questions').length > 0,
  });
  
  const assessmentStart = Date.now();
  
  for (let i = 0; i < sampleAnswers.length; i++) {
    const answer = sampleAnswers[i];
    const answerPayload = JSON.stringify({
      sessionToken: sessionToken,
      questionId: answer.questionId,
      selectedOption: answer.selectedOption,
      strength: answer.strength,
    });
    
    const answerResponse = http.post(`${BASE_URL}/api/assessment/submit-answer`, answerPayload, {
      headers: headers,
    });
    
    check(answerResponse, {
      [`answer ${i + 1} submitted successfully`]: (r) => r.status === 200,
    });
    
    sleep(0.1);
  }
  
  const completePayload = JSON.stringify({
    sessionToken: sessionToken,
  });
  
  const completeResponse = http.post(`${BASE_URL}/api/assessment/complete`, completePayload, {
    headers: headers,
  });
  
  const assessmentCompleted = check(completeResponse, {
    'assessment completion successful': (r) => r.status === 200,
    'personality type assigned': (r) => r.json('personalityType') !== undefined,
    'career matches provided': (r) => r.json('careerMatches') && r.json('careerMatches').length > 0,
  });
  
  if (assessmentCompleted) {
    const assessmentTime = Date.now() - assessmentStart;
    assessmentCompletionTime.add(assessmentTime);
  } else {
    errorRate.add(1);
  }
  
  const personalityType = completeResponse.json('personalityType');
  if (personalityType) {
    const careersResponse = http.get(`${BASE_URL}/api/careers/matches/${personalityType}`, {
      headers: headers,
    });
    
    check(careersResponse, {
      'career recommendations retrieved': (r) => r.status === 200,
      'career matches not empty': (r) => r.json('matches') && r.json('matches').length > 0,
    });
  }
  
  if (sessionToken) {
    const reportResponse = http.get(`${BASE_URL}/api/reports/assessment/${sessionToken}`, {
      headers: headers,
    });
    
    check(reportResponse, {
      'report generation successful': (r) => r.status === 200,
    });
  }
  
  sleep(Math.random() * 2 + 1);
}

export function teardown(data) {
  console.log('Performance test completed');
}

export function handleSummary(data) {
  return {
    'performance-test-results.json': JSON.stringify(data, null, 2),
    'performance-test-summary.txt': textSummary(data, { indent: ' ', enableColors: true }),
  };
}

function textSummary(data, options = {}) {
  const indent = options.indent || '';
  const enableColors = options.enableColors || false;
  
  let summary = `${indent}Performance Test Summary\n`;
  summary += `${indent}========================\n\n`;
  
  summary += `${indent}Test Duration: ${Math.round(data.state.testRunDurationMs / 1000)}s\n`;
  summary += `${indent}Virtual Users: ${data.metrics.vus.values.max}\n`;
  summary += `${indent}Iterations: ${data.metrics.iterations.values.count}\n\n`;
  
  summary += `${indent}HTTP Metrics:\n`;
  summary += `${indent}  Requests: ${data.metrics.http_reqs.values.count}\n`;
  summary += `${indent}  Failed: ${data.metrics.http_req_failed.values.rate * 100}%\n`;
  summary += `${indent}  Duration (avg): ${Math.round(data.metrics.http_req_duration.values.avg)}ms\n`;
  summary += `${indent}  Duration (p95): ${Math.round(data.metrics.http_req_duration.values['p(95)'])}ms\n\n`;
  
  if (data.metrics.session_creation_time) {
    summary += `${indent}Session Creation:\n`;
    summary += `${indent}  Average: ${Math.round(data.metrics.session_creation_time.values.avg)}ms\n`;
    summary += `${indent}  P95: ${Math.round(data.metrics.session_creation_time.values['p(95)'])}ms\n`;
    summary += `${indent}  Target: <100ms\n\n`;
  }
  
  if (data.metrics.assessment_completion_time) {
    summary += `${indent}Assessment Completion:\n`;
    summary += `${indent}  Average: ${Math.round(data.metrics.assessment_completion_time.values.avg)}ms\n`;
    summary += `${indent}  P95: ${Math.round(data.metrics.assessment_completion_time.values['p(95)'])}ms\n`;
    summary += `${indent}  Target: <2000ms\n\n`;
  }
  
  summary += `${indent}Threshold Results:\n`;
  for (const [name, threshold] of Object.entries(data.thresholds)) {
    const status = threshold.ok ? 'PASS' : 'FAIL';
    summary += `${indent}  ${name}: ${status}\n`;
  }
  
  return summary;
}
