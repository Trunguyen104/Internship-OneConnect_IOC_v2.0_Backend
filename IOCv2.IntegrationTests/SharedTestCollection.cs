using IOCv2.IntegrationTests.Factories;
using Xunit;

namespace IOCv2.IntegrationTests;

[CollectionDefinition("Integration collection")]
public class SharedTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.

    // Using a shared collection prevents Xunit from starting a new API instance per test class,
    // thereby saving significant memory and execution time.
}
