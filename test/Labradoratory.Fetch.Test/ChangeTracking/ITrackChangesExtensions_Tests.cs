﻿using Labradoratory.Fetch.ChangeTracking;
using Moq;
using Xunit;

namespace Labradoratory.Fetch.Test.ChangeTracking
{
    public class ITrackChangesExtensions_Tests
    {
        [Fact]
        public void CommitChanges_CallsITracksChangesGetChangeSet()
        {
            var expectedPath = ChangePath.Create("MyPath");
            var expectedChangeSet = new ChangeSet();
            var mock = new Mock<ITracksChanges>(MockBehavior.Strict);
            mock
                .Setup(tc => tc.GetChangeSet(It.IsAny<ChangePath>(), It.IsAny<bool>()))
                .Returns(expectedChangeSet);

            var result = ITracksChangesExtensions.CommitChanges(mock.Object, expectedPath);
            Assert.Same(expectedChangeSet, result);

            mock.Verify(tc => tc.GetChangeSet(
                It.Is<ChangePath>(v => v == expectedPath),
                It.Is<bool>(v => v)),
                Times.Once);
        }
    }
}
