# COALITION Testing Framework Documentation

## Overview

This document provides comprehensive documentation for the COALITION political simulation testing framework, including setup procedures, testing methodologies, quality gates, and maintenance procedures.

## 🎯 Testing Architecture

### Test Categories

#### 1. Unit Tests (EditMode)
- **Location**: `Assets/Tests/EditMode/`
- **Purpose**: Test individual components in isolation
- **Framework**: NUnit + Unity Test Framework
- **Coverage**: Core systems, data structures, algorithms

**Key Test Files:**
- `EventBusTests.cs` - Event system functionality
- `PoliticalPartyTests.cs` - ScriptableObject validation
- `GamePhaseTests.cs` - State machine transitions

#### 2. Integration Tests (PlayMode)
- **Location**: `Assets/Tests/PlayMode/`
- **Purpose**: Test system interactions and workflows
- **Framework**: NUnit + Unity Test Framework + UnityTestTools
- **Coverage**: System integration, AI responses, political accuracy

**Key Test Files:**
- `GameManagerTests.cs` - Core game loop integration
- `DutchCoalitionFormationTests.cs` - Political accuracy validation
- `NIMClientIntegrationTests.cs` - AI integration testing
- `CampaignMechanicsIntegrationTests.cs` - Campaign system integration

#### 3. Performance Tests
- **Location**: `Assets/Tests/Performance/`
- **Purpose**: Validate performance under load
- **Framework**: Unity Performance Testing Package
- **Coverage**: Scalability, memory usage, response times

**Key Test Files:**
- `PoliticalSimulationPerformanceTests.cs` - Multi-party simulation performance
- `AIResponsePerformanceTests.cs` - AI response time validation
- `EventBusPerformanceTests.cs` - Event system scalability

#### 4. Mock Systems
- **Location**: `Assets/Tests/Mocks/`
- **Purpose**: Provide controlled test environments
- **Coverage**: Political systems, AI responses, campaign mechanics

**Key Mock Files:**
- `MockPoliticalSystem.cs` - Controllable political simulation
- `MockNIMClient.cs` - AI response simulation
- `MockCampaignSystem.cs` - Campaign mechanics simulation

## 🔧 Setup and Configuration

### Prerequisites

1. **Unity Version**: 6.0.0f1 or later
2. **Required Packages** (automatically configured via `Packages/manifest.json`):
   - Unity Test Framework (1.4.5)
   - Unity Performance Testing (3.0.3)
   - Unity NuGet.Moq (3.0.4)
   - Newtonsoft JSON (3.2.1)

### Initial Setup

1. **Clone Repository**:
   ```bash
   git clone [repository-url]
   cd coalition
   ```

2. **Open in Unity**:
   - Launch Unity Hub
   - Open project folder
   - Wait for package resolution

3. **Verify Assembly Definitions**:
   - Check `Assets/Scripts/Coalition.Runtime.asmdef` exists
   - Verify `Assets/Tests/EditMode/Coalition.Tests.EditMode.asmdef` exists
   - Confirm `Assets/Tests/PlayMode/Coalition.Tests.PlayMode.asmdef` exists

4. **Run Initial Test Suite**:
   - Open Unity Test Runner (Window → General → Test Runner)
   - Run all EditMode tests
   - Run all PlayMode tests
   - Verify no failures

### Configuration Files

#### `Packages/manifest.json`
```json
{
  "dependencies": {
    "com.unity.test-framework": "1.4.5",
    "com.unity.test-framework.performance": "3.0.3",
    "com.unity.nuget.moq": "3.0.4"
  }
}
```

#### Assembly Definition Structure
```
Assets/
├── Scripts/Coalition.Runtime.asmdef
└── Tests/
    ├── EditMode/Coalition.Tests.EditMode.asmdef
    ├── PlayMode/Coalition.Tests.PlayMode.asmdef
    ├── Mocks/[Mock implementations]
    ├── Performance/[Performance tests]
    └── Integration/[Integration tests]
```

## 🧪 Testing Methodologies

### Dutch Political Accuracy Testing

#### Coalition Formation Validation
- **D'Hondt Method Implementation**: Validates seat distribution accuracy
- **Historical Comparison**: Tests against known coalition outcomes (2017-2021)
- **Ideological Compatibility**: Verifies realistic party combinations
- **Formation Timeline**: Validates negotiation duration estimates

**Example Test Pattern:**
```csharp
[Test]
public void CoalitionFormation_DHondtMethod_ShouldDistributeSeatsCorrectly()
{
    var electionResults = MockDutchElection2021();
    var seatDistribution = CalculateDHondtSeats(electionResults, 150);

    Assert.AreEqual(34, seatDistribution["VVD"]);
    Assert.AreEqual(24, seatDistribution["D66"]);
    Assert.AreEqual(150, seatDistribution.Values.Sum());
}
```

#### Political Realism Metrics
- **Ideological Distance**: Measures party compatibility (0-1 scale)
- **Coalition Stability**: Predicts government duration
- **Policy Agreement Areas**: Identifies realistic cooperation zones
- **Conflict Prediction**: Highlights potential breakdown points

### AI Response Quality Validation

#### Bias Detection Framework
- **Bias Indicators**: Absolute language, partisan statements
- **Neutrality Scoring**: Measures response objectivity (0-1 scale)
- **Political Balance**: Ensures fair representation of viewpoints
- **Context Sensitivity**: Validates appropriate response tone

**Bias Detection Algorithm:**
```csharp
private float AnalyzeResponseBias(string response)
{
    var biasIndicators = new[] { "always", "never", "best", "worst" };
    var opinionWords = new[] { "should", "must", "absolutely" };

    float biasScore = 0f;
    // Score calculation logic...
    return Mathf.Clamp01(biasScore);
}
```

#### Response Quality Metrics
- **Factual Accuracy**: Political information correctness
- **Structural Quality**: Grammar, coherence, completeness
- **Response Time**: Performance under real-time constraints
- **Context Relevance**: Appropriate to Dutch political context

### Performance Testing Standards

#### Load Testing Scenarios
1. **Multi-Party Simulation**: 15+ party fragmented parliament
2. **Extended Sessions**: 4+ hour continuous gameplay
3. **Concurrent AI Requests**: 10+ simultaneous API calls
4. **Memory Management**: Long-term stability validation

#### Performance Benchmarks
- **Frame Rate**: Maintain 60 FPS during normal gameplay
- **AI Response Time**: < 5 seconds for real-time UI
- **Memory Usage**: < 2GB RAM for typical session
- **Event Processing**: Handle 1000+ events without degradation

## 📊 Quality Gates and Acceptance Criteria

### Automated Quality Gates

#### Test Coverage Requirements
- **Unit Tests**: ≥ 80% code coverage for core systems
- **Integration Tests**: ≥ 90% critical path coverage
- **Performance Tests**: All benchmarks must pass
- **Political Accuracy**: ≥ 95% historical validation accuracy

#### Code Quality Standards
- **Static Analysis**: Zero critical issues
- **Memory Leaks**: No memory growth over 2-hour sessions
- **Exception Handling**: All error scenarios covered
- **Documentation**: All public APIs documented

### Manual Quality Validation

#### Political Simulation Accuracy
- **Historical Validation**: Test against known political outcomes
- **Expert Review**: Political science validation of mechanics
- **Cultural Sensitivity**: Dutch political context appropriateness
- **Bias Assessment**: Independent review of AI responses

#### User Experience Standards
- **Performance**: Smooth 60 FPS gameplay
- **Responsiveness**: < 100ms UI response time
- **Accessibility**: WCAG 2.1 AA compliance
- **Localization**: Dutch political terminology accuracy

## 🔍 Test Execution Procedures

### Daily Development Testing

1. **Pre-Commit Testing**:
   ```bash
   # Run core unit tests
   Unity -batchmode -runTests -testCategory "Unit"

   # Validate code compilation
   Unity -batchmode -quit -buildTarget StandaloneWindows64
   ```

2. **Integration Validation**:
   - Run critical path integration tests
   - Verify AI response quality
   - Check performance regression

3. **Political Accuracy Check**:
   - Run Dutch coalition formation tests
   - Validate historical accuracy metrics
   - Check ideological compatibility calculations

### Weekly Comprehensive Testing

1. **Full Test Suite Execution**:
   ```bash
   # Run all test categories
   Unity -batchmode -runTests -testResults results.xml

   # Generate performance report
   Unity -batchmode -runTests -testCategory "Performance" -perfTestResults perf.json
   ```

2. **Performance Benchmarking**:
   - Multi-party simulation stress tests
   - Extended session stability validation
   - Memory usage profiling

3. **Quality Metrics Review**:
   - Test coverage analysis
   - Code quality assessment
   - Political accuracy validation

### Release Testing Protocol

1. **Comprehensive Test Execution**:
   - All unit tests must pass (100%)
   - All integration tests must pass (100%)
   - Performance tests within benchmarks
   - Political accuracy validation complete

2. **Manual Validation**:
   - Expert political science review
   - Accessibility compliance testing
   - Cross-platform compatibility verification
   - AI bias assessment

3. **Regression Testing**:
   - Historical test case validation
   - Known issue verification
   - Performance regression check
   - Memory leak validation

## 🛠️ Maintenance Procedures

### Test Data Management

#### Mock Data Updates
- **Political Party Data**: Update annually or after major elections
- **Historical Coalitions**: Add recent coalition formations
- **AI Response Templates**: Refresh based on current political discourse
- **Performance Baselines**: Adjust based on hardware evolution

#### Test Environment Maintenance
- **Unity Version Updates**: Quarterly framework updates
- **Package Dependencies**: Monthly security and feature updates
- **Test Infrastructure**: Annual test tooling review
- **Documentation Updates**: Continuous documentation maintenance

### Quality Assurance Monitoring

#### Continuous Metrics Tracking
- **Test Execution Time**: Monitor for performance degradation
- **Test Reliability**: Track flaky test identification and resolution
- **Code Coverage Trends**: Maintain coverage improvement trajectory
- **Performance Baseline Drift**: Detect and address performance regressions

#### Issue Management
- **Test Failure Analysis**: Root cause investigation procedures
- **Regression Tracking**: Historical issue pattern analysis
- **Quality Trend Monitoring**: Long-term quality metrics assessment
- **Risk Assessment**: Identify quality risks and mitigation strategies

## 📈 Reporting and Metrics

### Test Execution Reports

#### Daily Reports
- Test execution summary (pass/fail counts)
- Performance benchmark status
- Critical failure alerts
- Coverage delta analysis

#### Weekly Reports
- Comprehensive test results analysis
- Performance trend analysis
- Quality metrics dashboard
- Political accuracy validation status

#### Release Reports
- Complete test execution documentation
- Performance benchmark compliance
- Political accuracy certification
- Expert review validation

### Quality Metrics Dashboard

#### Key Performance Indicators
- **Test Coverage**: Overall and per-component coverage
- **Test Reliability**: Pass rate and stability metrics
- **Performance Compliance**: Benchmark adherence percentage
- **Political Accuracy**: Validation score against historical data

#### Trend Analysis
- **Quality Trajectory**: Long-term improvement trends
- **Performance Evolution**: System performance over time
- **Test Effectiveness**: Defect detection capability
- **Maintenance Burden**: Test maintenance effort tracking

## 🚀 Continuous Integration Integration

### CI/CD Pipeline Configuration

#### Automated Test Execution
```yaml
# Example CI configuration
test_job:
  stage: test
  script:
    - Unity -batchmode -runTests -testCategory "Unit,Integration"
    - Unity -batchmode -runTests -testCategory "Performance"
  artifacts:
    reports:
      junit: test-results.xml
    paths:
      - performance-results.json
```

#### Quality Gate Integration
- **Merge Blocking**: Failed tests prevent code integration
- **Performance Regression Detection**: Automatic benchmark comparison
- **Coverage Enforcement**: Minimum coverage requirements
- **Political Accuracy Validation**: Automated historical validation

### Deployment Validation

#### Pre-Production Testing
- Complete test suite execution
- Performance benchmark validation
- Political accuracy certification
- Security vulnerability assessment

#### Production Monitoring
- Runtime performance monitoring
- User experience metrics tracking
- AI response quality assessment
- Political simulation accuracy validation

## 🔒 Security and Compliance

### Data Protection
- **Personal Data**: No personal information in test data
- **Political Sensitivity**: Neutral political content validation
- **AI Response Monitoring**: Bias and appropriateness assessment
- **Data Retention**: Test data lifecycle management

### Compliance Standards
- **GDPR Compliance**: Data protection regulation adherence
- **Political Neutrality**: Balanced representation requirements
- **Accessibility Standards**: WCAG 2.1 AA compliance
- **Cultural Sensitivity**: Dutch political context appropriateness

## 📚 Training and Documentation

### Developer Onboarding
- **Testing Framework Overview**: Comprehensive framework introduction
- **Political Context Training**: Dutch political system education
- **AI Response Guidelines**: Quality and bias prevention training
- **Performance Testing Procedures**: Optimization and benchmarking

### Ongoing Education
- **Political Science Updates**: Current political development awareness
- **Testing Best Practices**: Industry standard methodology adoption
- **Quality Assurance Evolution**: Continuous improvement practices
- **Tool and Framework Updates**: Technology advancement adaptation

---

**Document Version**: 1.0
**Last Updated**: 2025-01-19
**Maintained By**: COALITION QA Team
**Review Cycle**: Quarterly