# Contributing to Agent System

We welcome contributions to the Agent System! This document provides guidelines for contributing to the project.

## 🚀 Getting Started

### Prerequisites

- .NET 6.0 or later
- Visual Studio 2022 or VS Code with C# extension
- Git for version control
- Docker (optional, for containerization)

### Setting Up Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/your-username/agent-system.git
   cd agent-system
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Solution**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

## 📝 Coding Standards

### C# Conventions

- Follow the [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use PascalCase for classes, methods, and public properties
- Use camelCase for local variables and parameters
- Use _camelCase for private fields
- Prefix interfaces with "I"

### Code Organization

```csharp
// Example class structure
namespace AgentSystem.YourFeature
{
    public interface IYourInterface
    {
        Task<Result> ProcessAsync(Input input);
    }

    public class YourImplementation : IYourInterface
    {
        private readonly IDependency _dependency;
        private readonly ILogger _logger;

        public YourImplementation(IDependency dependency, ILogger logger)
        {
            _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> ProcessAsync(Input input)
        {
            _logger.Log($"Processing {input.Id}");
            
            try
            {
                return await _dependency.ExecuteAsync(input);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {input.Id}", ex);
                throw;
            }
        }
    }
}
```

## 🔧 Adding New Features

### 1. Adding a New Agent Type

Create a new agent class inheriting from `BaseAgent`:

```csharp
public class YourAgent : BaseAgent
{
    public override AgentType Type => AgentType.YourType;

    public YourAgent(
        string name,
        IMemoryService memory,
        ISafetyValidator safetyValidator,
        ILogger logger,
        IMessageBus messageBus)
        : base(memory, safetyValidator, logger, messageBus)
    {
        Name = name;
    }

    public override async Task<IAction> DecideAction(EnvironmentState state)
    {
        // Your decision logic here
        return new YourAction();
    }
}
```

### 2. Adding a New Guardrail

Implement the `IGuardrail` interface:

```csharp
public class YourGuardrail : IGuardrail
{
    public string Name => "Your Guardrail";

    public async Task<ValidationResult> Validate(IAction action)
    {
        // Your validation logic
        if (IsInvalid(action))
        {
            return ValidationResult.Failure("Reason for failure");
        }
        
        return ValidationResult.Success();
    }
}
```

### 3. Adding a New Sensor

Implement the `ISensor<T>` interface:

```csharp
public class YourSensor : ISensor<YourDataType>
{
    public string Name => "Your Sensor";

    public async Task<YourDataType> Perceive(IEnvironment environment)
    {
        // Your sensing logic
        return new YourDataType();
    }
}
```

### 4. Adding a New Memory Service

Implement the appropriate memory interface:

```csharp
public class YourMemoryService : ILongTermMemory
{
    public async Task Initialize(string agentId)
    {
        // Initialize your storage
    }

    public async Task StoreContext(string key, object value, TimeSpan? ttl = null)
    {
        // Your storage logic
    }

    public async Task<T> RetrieveContext<T>(string key)
    {
        // Your retrieval logic
    }

    // Implement other required methods
}
```

## 🧪 Testing Guidelines

### Unit Tests

Every new feature should include unit tests:

```csharp
[TestClass]
public class YourAgentTests
{
    private Mock<IMemoryService> _mockMemory;
    private Mock<ISafetyValidator> _mockSafetyValidator;
    private Mock<ILogger> _mockLogger;
    private Mock<IMessageBus> _mockMessageBus;
    private YourAgent _agent;

    [TestInitialize]
    public void Setup()
    {
        _mockMemory = new Mock<IMemoryService>();
        _mockSafetyValidator = new Mock<ISafetyValidator>();
        _mockLogger = new Mock<ILogger>();
        _mockMessageBus = new Mock<IMessageBus>();

        _agent = new YourAgent(
            "TestAgent",
            _mockMemory.Object,
            _mockSafetyValidator.Object,
            _mockLogger.Object,
            _mockMessageBus.Object);
    }

    [TestMethod]
    public async Task DecideAction_ShouldReturnValidAction()
    {
        // Arrange
        var state = new EnvironmentState();
        _mockSafetyValidator
            .Setup(x => x.ValidateAction(It.IsAny<IAction>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var action = await _agent.DecideAction(state);

        // Assert
        Assert.IsNotNull(action);
        Assert.AreEqual("ExpectedActionType", action.ActionType);
    }
}
```

### Integration Tests

For features that interact with external systems:

```csharp
[TestClass]
public class YourIntegrationTests
{
    [TestMethod]
    public async Task YourFeature_ShouldWorkEndToEnd()
    {
        // Use test containers or in-memory implementations
        var services = new ServiceCollection();
        services.AddAgentSystem(TestConfiguration);
        
        var serviceProvider = services.BuildServiceProvider();
        var agent = serviceProvider.GetRequiredService<IAgent>();
        
        // Test end-to-end functionality
    }
}
```

### Performance Tests

For performance-critical components:

```csharp
[TestClass]
public class YourPerformanceTests
{
    [TestMethod]
    public async Task YourOperation_ShouldCompleteWithinTimeLimit()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Execute operation
        await YourOperation();
        
        stopwatch.Stop();
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
            $"Operation took {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## 📖 Documentation

### Code Documentation

Use XML documentation for public APIs:

```csharp
/// <summary>
/// Processes the input and returns a result.
/// </summary>
/// <param name="input">The input to process.</param>
/// <returns>The processing result.</returns>
/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
public async Task<Result> ProcessAsync(Input input)
{
    // Implementation
}
```

### README Updates

Update relevant documentation when adding features:

1. Add to feature list if significant
2. Update configuration examples
3. Add usage examples
4. Update API reference

## 🔄 Pull Request Process

### Before Submitting

1. **Update Documentation**: Ensure all documentation is updated
2. **Add Tests**: Include unit and integration tests
3. **Run All Tests**: `dotnet test`
4. **Check Code Coverage**: Aim for >80% coverage
5. **Format Code**: Use .editorconfig settings
6. **Update CHANGELOG**: Add your changes

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Performance tests pass (if applicable)

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] All tests passing
- [ ] No breaking changes (or documented)
```

### Review Process

1. **Automated Checks**: CI/CD pipeline runs tests
2. **Code Review**: At least one maintainer reviews
3. **Discussion**: Address feedback and questions
4. **Approval**: Receive approval from maintainer
5. **Merge**: Squash and merge to main branch

## 🐛 Bug Reports

### Template

```markdown
## Bug Description
Clear description of the bug

## Steps to Reproduce
1. Step one
2. Step two
3. ...

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Environment
- OS: [e.g., Windows 10]
- .NET Version: [e.g., 6.0.5]
- Agent System Version: [e.g., 1.2.3]

## Additional Context
Any other relevant information
```

## 💡 Feature Requests

### Template

```markdown
## Feature Description
Clear description of the proposed feature

## Use Case
Why is this feature needed?

## Proposed Solution
How might this be implemented?

## Alternatives Considered
Other approaches you've thought about

## Additional Context
Any other relevant information
```

## 🏗️ Architecture Decisions

### ADR Template

When making significant architectural changes, create an Architecture Decision Record (ADR):

```markdown
# Title

## Status
Proposed/Accepted/Deprecated

## Context
Background and problem statement

## Decision
What decision was made

## Consequences
Positive and negative outcomes

## Alternatives Considered
Other options that were evaluated
```

## 🔒 Security

### Reporting Security Issues

- **DO NOT** create public issues for security vulnerabilities
- Email security@agentsystem.dev with details
- Include steps to reproduce if possible
- We'll respond within 48 hours

### Security Guidelines

1. Never commit secrets or credentials
2. Use secure communication protocols
3. Validate all inputs
4. Follow OWASP guidelines
5. Use parameterized queries
6. Implement proper authentication
7. Use encryption for sensitive data

## 📦 Release Process

### Version Numbering

We follow [Semantic Versioning](https://semver.org/):
- MAJOR: Breaking changes
- MINOR: New features (backwards compatible)
- PATCH: Bug fixes (backwards compatible)

### Release Checklist

1. Update version numbers
2. Update CHANGELOG.md
3. Create release branch
4. Run full test suite
5. Create GitHub release
6. Publish NuGet packages
7. Update documentation

## 🤝 Community

### Communication Channels

- **Discord**: [Join our server](https://discord.gg/agent-system)
- **GitHub Discussions**: For general questions
- **Stack Overflow**: Tag with `agent-system`

### Code of Conduct

We follow the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). Please read and adhere to it.

## 📚 Resources

### Learning Resources

- [C# Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

### Tools

- [Visual Studio](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [ReSharper](https://www.jetbrains.com/resharper/)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## 🙏 Acknowledgments

Thank you for contributing to Agent System! Your efforts help make this project better for everyone.
