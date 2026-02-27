using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Expensify.Common.Infrastructure.RateLimiting;

namespace Expensify.Modules.Users.UnitTests.API.Middleware;

[TestFixture]
internal sealed class RateLimitingRequestPartitionerTests
{
    [Test]
    public void IsWriteRequest_WhenVersionedWriteRequest_ShouldReturnTrue()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/expenses";
        context.Request.Method = HttpMethods.Post;

        bool result = RateLimitingRequestPartitioner.IsWriteRequest(context);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsWriteRequest_WhenVersionedReadRequest_ShouldReturnFalse()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/expenses";
        context.Request.Method = HttpMethods.Get;

        bool result = RateLimitingRequestPartitioner.IsWriteRequest(context);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsWriteRequest_WhenNonApiPath_ShouldReturnFalse()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/health";
        context.Request.Method = HttpMethods.Post;

        bool result = RateLimitingRequestPartitioner.IsWriteRequest(context);

        Assert.That(result, Is.False);
    }

    [Test]
    public void GetWritePartitionKey_WhenUserClaimExists_ShouldUseUserPartition()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("userid", userId.ToString())]))
        };
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        string partition = RateLimitingRequestPartitioner.GetWritePartitionKey(context);

        Assert.That(partition, Is.EqualTo($"user:{userId:N}"));
    }

    [Test]
    public void GetWritePartitionKey_WhenUserClaimMissing_ShouldFallbackToIpPartition()
    {
        DefaultHttpContext context = new();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        string partition = RateLimitingRequestPartitioner.GetWritePartitionKey(context);

        Assert.That(partition, Is.EqualTo("ip:127.0.0.1"));
    }

    [Test]
    public void GetAuthPartitionKey_WhenForwardedHeaderExists_ShouldUseRemoteIp()
    {
        DefaultHttpContext context = new();
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.5, 203.0.113.9";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        string partition = RateLimitingRequestPartitioner.GetAuthPartitionKey(context);

        Assert.That(partition, Is.EqualTo("ip:127.0.0.1"));
    }
}
