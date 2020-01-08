using EnvDTE;
using Typewriter.Metadata.Providers;
using Xunit;

namespace Typewriter.Tests.TestInfrastructure
{
    public class NSwagFixture : ITestFixture
    {
        public NSwagFixture()
        {
            Dte = TestInfrastructure.Dte.GetInstance("Typewriter.sln");
            Provider = new RoslynMetadataProviderStub(Dte);

            // Handle threading errors when calling into Visual Studio.
            MessageFilter.Register();
        }

        public DTE Dte { get; }
        public IMetadataProvider Provider { get; }

        public void Dispose()
        {
            MessageFilter.Revoke();
        }
    }

    [CollectionDefinition(nameof(NSwagFixture))]
    public class NSwagCollection : ICollectionFixture<NSwagFixture>
    {
    }
}
