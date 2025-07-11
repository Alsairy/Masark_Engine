import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

const sessionCreationTime = new Trend('session_creation_time');
const memoryUsage = new Trend('memory_usage');
const cpuUsage = new Trend('cpu_usage');
const errorRate = new Rate('error_rate');
const concurrentUsers = new Counter('concurrent_users');

export const options = {
  stages: [
    { duration: '1m', target: 50 },    // Warm up
    { duration: '2m', target: 200 },   // Ramp up to 200 users
    { duration: '3m', target: 500 },   // Stress test with 500 users
    { duration: '2m', target: 1000 },  // Peak stress with 1000 users
    { duration: '5m', target: 1000 },  // Sustain peak load
    { duration: '2m', target: 0 },     // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // Allow higher latency under stress
    session_creation_time: ['p(95)<200'], // Relaxed session creation time
    error_rate: ['rate<0.10'], // Allow up to 10% error rate under stress
    http_req_failed: ['rate<0.10'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export function setup() {
  console.log('Starting stress test...');
  
  const healthResponse = http.get(`${BASE_URL}/health`);
  check(healthResponse, {
    'pre-stress health check': (r) => r.status === 200,
  });
  
  return { baseUrl: BASE_URL };
}

export default function (data) {
  concurrentUsers.add(1);
  
  const sessionStart = Date.now();
  
  const sessionPayload = JSON.stringify({
    languagePreference: Math.random() > 0.5 ? 'en' : 'ar',
    tenantId: `tenant_${Math.floor(Math.random() * 10)}`, // Multiple tenants
  });
  
  const headers = {
    'Content-Type': 'application/json',
  };
  
  const sessionResponse = http.post(`${BASE_URL}/api/assessment/start`, sessionPayload, {
    headers: headers,
  });
  
  const sessionSuccess = check(sessionResponse, {
    'stress session creation': (r) => r.status === 200,
  });
  
  if (sessionSuccess) {
    const sessionTime = Date.now() - sessionStart;
    sessionCreationTime.add(sessionTime);
  } else {
    errorRate.add(1);
  }
  
  const questionsResponse = http.get(`${BASE_URL}/api/assessment/questions`, {
    headers: headers,
  });
  
  check(questionsResponse, {
    'stress questions retrieval': (r) => r.status === 200,
  });
  
  const metricsResponse = http.get(`${BASE_URL}/api/system/metrics`, {
    headers: headers,
  });
  
  if (metricsResponse.status === 200) {
    const metrics = metricsResponse.json();
    if (metrics.memoryUsage) {
      memoryUsage.add(metrics.memoryUsage);
    }
    if (metrics.cpuUsage) {
      cpuUsage.add(metrics.cpuUsage);
    }
  }
  
  sleep(0.1);
}

export function teardown(data) {
  console.log('Stress test completed');
  
  const healthResponse = http.get(`${BASE_URL}/health`);
  check(healthResponse, {
    'post-stress health check': (r) => r.status === 200,
  });
}
