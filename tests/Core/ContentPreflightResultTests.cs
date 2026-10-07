using System;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentPreflightResultTests
{
    [Test]
    public void Success_AllowsCommitWithoutFailure()
    {
        ContentPreflightResult result = ContentPreflightResult.Success();

        Assert.That(result.CanCommit, Is.True);
        Assert.That(result.Failure, Is.Null);
    }

    [Test]
    public void Rejected_StoresFailure()
    {
        ContentFailure failure = ContentFailure.Create(ContentFailureKind.Validation, ContentFailureCodes.ValidationRejected, "Rejected.");

        ContentPreflightResult result = ContentPreflightResult.Rejected(failure);

        Assert.That(result.CanCommit, Is.False);
        Assert.That(result.Failure, Is.SameAs(failure));
    }

    [Test]
    public void Rejected_NullFailureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ContentPreflightResult.Rejected(null!));
    }
}
