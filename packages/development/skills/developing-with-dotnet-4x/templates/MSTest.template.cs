// Template: MSTest unit test class with Moq — .NET Framework 4.x
// Package baseline: MSTest.TestAdapter 1.2.0, MSTest.TestFramework 1.2.0, Moq 4.8.x (net461)
// Placeholders: {{Namespace}} {{Sut}} {{Dependency}}
//   {{Sut}}        -> ProductService          (system under test)
//   {{Dependency}} -> IProductRepository       (collaborator to mock)
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace {{Namespace}}.Tests
{
    [TestClass]
    public class {{Sut}}Tests
    {
        private Mock<{{Dependency}}> _dependency;
        private {{Sut}} _sut;

        [TestInitialize]
        public void Setup()
        {
            _dependency = new Mock<{{Dependency}}>();
            _sut = new {{Sut}}(_dependency.Object);
        }

        [TestMethod]
        public void Find_ReturnsNull_WhenMissing()
        {
            _dependency.Setup(d => d.Get(42)).Returns((object)null);

            var result = _sut.Find(42);

            Assert.IsNull(result);
            _dependency.Verify(d => d.Get(42), Times.Once);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Find_Throws_OnNonPositiveId(int id)
        {
            _sut.Find(id);
        }
    }
}
