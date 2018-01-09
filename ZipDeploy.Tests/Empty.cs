using FluentAssertions;
using NUnit.Framework;

namespace ZipDeploy.Tests
{
    [TestFixture]
    public class Empty
    {
        [Test]
        public void NoTest()
        {
            "Verifying NUnit tests are wired up correctly".Should().NotBeNullOrWhiteSpace();
        }
    }
}
