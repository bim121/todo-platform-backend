using TodoPlatform.Api.Tests.Infrastructure;

namespace TodoPlatform.Api.Tests;

[CollectionDefinition(nameof(TodoPlatformWebApplicationFactory))]
public sealed class TodoPlatformWebApplicationCollection : ICollectionFixture<TodoPlatformWebApplicationFactory>;
