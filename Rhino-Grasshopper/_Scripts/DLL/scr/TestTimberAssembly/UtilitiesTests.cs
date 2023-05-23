
using Moq;

namespace TestTimberAssembly
{
    [TestFixture]
    public class UtilitiesTests
    {
        private Agent _target;
        private Agent _agent1;
        private Agent _agent2;
        private double _tolerance;

        [SetUp]
        public void Setup()
        {
            _tolerance = 0.1;
            _target = new Agent
            {
                Dimension = new Dimension(20, 10, 5)
            };

            _agent1 = new Agent
            {
                Dimension = new Dimension(20, 10, 2)
            };

            _agent2 = new Agent
            {
                Dimension = new Dimension(20, 10, 3)
            };
        }

        [Test]
        public void IsAgentExactMatched_ReturnsTrue_WhenDimensionsMatchWithinTolerance()
        {
            // Arrange
            _agent1 = new Agent { Dimension = new Dimension(10, 10, 10)};
            _agent2 = new Agent { Dimension = new Dimension(10.05, 9.95, 10)};
            var tolerance = 0.1;

            // Act
            var result = Utilities.IsAgentExactMatched(_agent1, _agent2, tolerance);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsAgentExactMatched_ReturnsFalse_WhenDimensionsDoNotMatchWithinTolerance()
        {
            // Arrange
            _agent1 = new Agent { Dimension = new Dimension(10, 10, 10)};
            _agent2 = new Agent { Dimension = new Dimension(10.2, 9.8, 10)};
            var tolerance = 0.1;

            // Act
            var result = Utilities.IsAgentExactMatched(_agent1, _agent2, tolerance);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_IsAgentSecondMatched_ReturnsFalse_AgentMismatchOneDimension()
        {
            _agent1 = new Agent
            {
                Dimension = new Dimension(20, 5, 2)
            };

            _agent2 = new Agent
            {
                Dimension = new Dimension(20, 5, 3)
            };

            var result = Utilities.IsAgentSecondMatched(_target, _agent1, _agent2, _tolerance);

            Assert.False(result);
        }

        [Test]
        public void Test_IsAgentSecondMatched_ReturnsTrue_AgentMatchOneDimension()
        {
            _agent1 = new Agent
            {
                Dimension = new Dimension(20, 10, 2)
            };

            _agent2 = new Agent
            {
                Dimension = new Dimension(20, 10, 3)
            };

            var result = Utilities.IsAgentSecondMatched(_target, _agent1, _agent2, _tolerance);

            Assert.True(result);
        }

        [Test]
        public void Test_IsAgentSecondMatched_ReturnsFalse_IfTargetsDimensionDoesNotMatchSumOfTwoAgentsDimensions()
        {
            _agent1 = new Agent
            {
                Dimension = new Dimension(15, 4, 4)
            };

            _agent2 = new Agent
            {
                Dimension = new Dimension(12, 6, 6)
            };

            var result = Utilities.IsAgentSecondMatched(_target, _agent1, _agent2, _tolerance);

            Assert.False(result);
        }

        [Test]
        public void Test_IsAgentSecondMatched_ReturnsFalse_MatchFirstDimensionMismatchSecondDimension()
        {
            _agent1 = new Agent
            {
                Dimension = new Dimension(20, 10, 4)
            };

            _agent2 = new Agent
            {
                Dimension = new Dimension(12, 6, 1)
            };

            var result = Utilities.IsAgentSecondMatched(_target, _agent1, _agent2, _tolerance);

            Assert.False(result);
        }
    }
}
