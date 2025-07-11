import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const apiResponseTime = new Trend('api_response_time');
const cacheHitRate = new Rate('cache_hit_rate');
const databaseQueryTime = new Trend('database_query_time');

export const options = {
  scenarios: {
    session_creation: {
      executor: 'constant-arrival-rate',
      rate: 100, // 100 requests per second
      timeUnit: '1s',
      duration: '2m',
      preAllocatedVUs: 10,
      maxVUs: 50,
      exec: 'benchmarkSessionCreation',
    },
    
    cache_performance: {
      executor: 'constant-vus',
      vus: 20,
      duration: '3m',
      exec: 'benchmarkCachePerformance',
    },
    
    database_performance: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '1m', target: 10 },
        { duration: '2m', target: 10 },
        { duration: '1m', target: 1 },
      ],
      exec: 'benchmarkDatabasePerformance',
    },
  },
  thresholds: {
    'api_response_time{scenario:session_creation}': ['p(95)<100'], // Session creation must be <100ms
    'api_response_time{scenario:cache_performance}': ['p(95)<50'],  // Cache hits must be <50ms
    'database_query_time': ['p(95)<200'], // Database queries must be <200ms
    'cache_hit_rate': ['rate>0.80'], // Cache hit rate must be >80%
    'http_req_failed': ['rate<0.01'], // Less than 1% failure rate
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export function benchmarkSessionCreation() {
  const start = Date.now();
  
  const payload = JSON.stringify({
    languagePreference: 'en',
    tenantId: 'benchmark',
  });
  
  const response = http.post(`${BASE_URL}/api/assessment/start`, payload, {
    headers: { 'Content-Type': 'application/json' },
  });
  
  const responseTime = Date.now() - start;
  apiResponseTime.add(responseTime, { scenario: 'session_creation' });
  
  check(response, {
    'session creation benchmark success': (r) => r.status === 200,
    'session creation under 100ms': () => responseTime < 100,
  });
}

export function benchmarkCachePerformance() {
  const cacheTestUrls = [
    `${BASE_URL}/api/assessment/questions?language=en`,
    `${BASE_URL}/api/careers?language=en`,
    `${BASE_URL}/api/personality-types?language=en`,
  ];
  
  const url = cacheTestUrls[Math.floor(Math.random() * cacheTestUrls.length)];
  const start = Date.now();
  
  const response = http.get(url);
  const responseTime = Date.now() - start;
  
  apiResponseTime.add(responseTime, { scenario: 'cache_performance' });
  
  const isCacheHit = responseTime < 50;
  cacheHitRate.add(isCacheHit ? 1 : 0);
  
  check(response, {
    'cache performance benchmark success': (r) => r.status === 200,
    'cache response under 50ms': () => responseTime < 50,
  });
  
  sleep(0.1);
}

export function benchmarkDatabasePerformance() {
  const start = Date.now();
  
  const response = http.get(`${BASE_URL}/api/assessment/statistics`);
  const responseTime = Date.now() - start;
  
  databaseQueryTime.add(responseTime);
  
  check(response, {
    'database benchmark success': (r) => r.status === 200,
    'database query under 200ms': () => responseTime < 200,
  });
  
  sleep(0.5);
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    test_type: 'benchmark',
    results: {
      session_creation: {
        avg_response_time: data.metrics.api_response_time?.values?.avg || 0,
        p95_response_time: data.metrics.api_response_time?.values?.['p(95)'] || 0,
        target_met: (data.metrics.api_response_time?.values?.['p(95)'] || 0) < 100,
      },
      cache_performance: {
        hit_rate: (data.metrics.cache_hit_rate?.values?.rate || 0) * 100,
        avg_response_time: data.metrics.api_response_time?.values?.avg || 0,
        target_met: (data.metrics.cache_hit_rate?.values?.rate || 0) > 0.80,
      },
      database_performance: {
        avg_query_time: data.metrics.database_query_time?.values?.avg || 0,
        p95_query_time: data.metrics.database_query_time?.values?.['p(95)'] || 0,
        target_met: (data.metrics.database_query_time?.values?.['p(95)'] || 0) < 200,
      },
      overall: {
        total_requests: data.metrics.http_reqs?.values?.count || 0,
        failure_rate: (data.metrics.http_req_failed?.values?.rate || 0) * 100,
        test_duration: Math.round((data.state.testRunDurationMs || 0) / 1000),
      },
    },
  };
  
  return {
    'benchmark-results.json': JSON.stringify(summary, null, 2),
    'benchmark-summary.txt': generateBenchmarkSummary(summary),
  };
}

function generateBenchmarkSummary(data) {
  let summary = 'Masark Engine Performance Benchmark Results\n';
  summary += '==========================================\n\n';
  
  summary += `Test Date: ${data.timestamp}\n`;
  summary += `Test Duration: ${data.results.overall.test_duration}s\n`;
  summary += `Total Requests: ${data.results.overall.total_requests}\n`;
  summary += `Failure Rate: ${data.results.overall.failure_rate.toFixed(2)}%\n\n`;
  
  summary += 'Performance Targets:\n';
  summary += '-------------------\n';
  summary += `Session Creation (<100ms): ${data.results.session_creation.target_met ? 'PASS' : 'FAIL'}\n`;
  summary += `  - P95 Response Time: ${data.results.session_creation.p95_response_time.toFixed(2)}ms\n`;
  summary += `Cache Performance (>80% hit rate): ${data.results.cache_performance.target_met ? 'PASS' : 'FAIL'}\n`;
  summary += `  - Hit Rate: ${data.results.cache_performance.hit_rate.toFixed(2)}%\n`;
  summary += `Database Performance (<200ms): ${data.results.database_performance.target_met ? 'PASS' : 'FAIL'}\n`;
  summary += `  - P95 Query Time: ${data.results.database_performance.p95_query_time.toFixed(2)}ms\n\n`;
  
  const allTargetsMet = data.results.session_creation.target_met && 
                       data.results.cache_performance.target_met && 
                       data.results.database_performance.target_met;
  
  summary += `Overall Result: ${allTargetsMet ? 'ALL TARGETS MET' : 'SOME TARGETS MISSED'}\n`;
  
  return summary;
}
