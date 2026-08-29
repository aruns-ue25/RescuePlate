using Xunit;

namespace UserService.IntegrationTests.Helpers;

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class TestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
