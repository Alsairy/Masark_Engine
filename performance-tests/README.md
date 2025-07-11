# Masark Engine Performance Testing

This directory contains comprehensive performance tests for the Masark Personality-Career Matching Engine using k6.

## Test Types

### 1. Load Testing (`load-test.js`)
- **Purpose**: Validate system performance under expected load
- **Configuration**: 10-100 concurrent users over 4 minutes
- **Key Metrics**:
  - Session creation time: <100ms (P95)
  - Assessment completion time: <2000ms (P95)
  - Error rate: <5%
  - HTTP request duration: <500ms (P95)

### 2. Stress Testing (`stress-test.js`)
- **Purpose**: Determine system breaking point and recovery
- **Configuration**: 50-1000 concurrent users over 15 minutes
- **Key Metrics**:
  - Session creation time: <200ms (P95) under stress
  - Error rate: <10% under peak load
  - System stability during ramp-down

### 3. Benchmark Testing (`benchmark.js`)
- **Purpose**: Validate specific performance targets
- **Scenarios**:
  - Session creation: 100 RPS for 2 minutes
  - Cache performance: 20 VUs for 3 minutes
  - Database performance: 1-10 VUs ramping
- **Critical Targets**:
  - Session creation: <100ms (P95)
  - Cache hits: <50ms (P95)
  - Database queries: <200ms (P95)
  - Cache hit rate: >80%

## Running Tests Locally

### Prerequisites
```bash
# Install k6
npm install -g k6

# Start the Masark Engine
cd ../Masark.API
dotnet run
```

### Execute Tests
```bash
# Load test
k6 run load-test.js

# Stress test
k6 run stress-test.js

# Benchmark test
k6 run benchmark.js

# Custom configuration
k6 run --vus 50 --duration 2m load-test.js
```

### Environment Variables
- `BASE_URL`: Target application URL (default: http://localhost:5000)

## CI/CD Integration

Performance tests run automatically:
- **On Push**: Load and benchmark tests on main/develop branches
- **On PR**: Load and benchmark tests with results commented
- **Daily**: Full test suite at 2 AM UTC
- **Manual**: Workflow dispatch with test type selection

## Performance Targets

### Critical Performance Requirements
| Metric | Target | Test |
|--------|--------|------|
| Session Creation | <100ms (P95) | Benchmark |
| Assessment Completion | <2000ms (P95) | Load |
| Cache Hit Rate | >80% | Benchmark |
| Database Queries | <200ms (P95) | Benchmark |
| Error Rate | <5% | Load |
| Concurrent Users | 100+ | Load |

### System Scalability Targets
| Load Level | Users | Duration | Expected Performance |
|------------|-------|----------|---------------------|
| Normal | 50 | Sustained | All targets met |
| Peak | 100 | 2 minutes | All targets met |
| Stress | 500 | 3 minutes | Graceful degradation |
| Breaking Point | 1000 | 5 minutes | <10% error rate |

## Test Scenarios

### 1. Complete Assessment Flow
1. User authentication
2. Session creation (⚡ Critical: <100ms)
3. Question retrieval
4. Answer submission (5 answers)
5. Assessment completion
6. Career recommendations
7. Report generation

### 2. Cache Performance
- Repeated requests to cached endpoints
- Cache hit rate measurement
- Response time validation

### 3. Database Performance
- Assessment statistics queries
- Bulk data operations
- Connection pooling efficiency

## Monitoring and Alerting

### Key Performance Indicators (KPIs)
- **Availability**: >99.9% uptime
- **Response Time**: P95 <500ms for all endpoints
- **Throughput**: >100 RPS sustained
- **Error Rate**: <1% under normal load

### Performance Regression Detection
- Automated threshold validation in CI
- Historical performance comparison
- Alert on 20% performance degradation

## Results Analysis

### Interpreting Results
```javascript
// Example k6 output interpretation
{
  "http_req_duration": {
    "avg": 45.2,      // Average response time
    "p(95)": 89.1     // 95th percentile (target: <100ms)
  },
  "session_creation_time": {
    "p(95)": 67.3     // Session creation P95 (target: <100ms)
  },
  "cache_hit_rate": {
    "rate": 0.87      // Cache hit rate (target: >80%)
  }
}
```

### Performance Optimization Guidelines
1. **Session Creation Optimization**:
   - Database connection pooling
   - Redis caching for static data
   - Async/await patterns

2. **Cache Strategy**:
   - Redis-first with memory fallback
   - Appropriate TTL settings
   - Cache warming on startup

3. **Database Performance**:
   - Bulk operations for high-volume data
   - Optimized queries with proper indexing
   - Connection pooling configuration

## Troubleshooting

### Common Issues
1. **High Response Times**:
   - Check database connection pool
   - Verify Redis connectivity
   - Review async operation patterns

2. **Low Cache Hit Rate**:
   - Validate cache key patterns
   - Check TTL configurations
   - Monitor cache eviction policies

3. **Session Creation Timeouts**:
   - Database initialization delays
   - Missing database seeding
   - Network connectivity issues

### Debug Commands
```bash
# Check application health
curl http://localhost:5000/health

# Monitor Redis
redis-cli monitor

# View application logs
dotnet run --verbosity detailed
```

## Contributing

When adding new performance tests:
1. Follow existing naming conventions
2. Include appropriate thresholds
3. Document test scenarios
4. Update this README
5. Validate tests locally before PR

## Performance History

Performance test results are archived as GitHub Actions artifacts for 30 days. Historical trends can be analyzed to identify performance regressions and improvements over time.
