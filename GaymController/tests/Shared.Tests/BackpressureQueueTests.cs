using System.Threading.Tasks;
using GaymController.Shared;
using Xunit;

namespace Shared.Tests;

public class BackpressureQueueTests
{
    [Fact]
    public async Task BlocksWriterWhenFullUntilRead()
    {
        var queue = new BackpressureQueue<int>(1);
        await queue.EnqueueAsync(1);

        var writeTask = queue.EnqueueAsync(2).AsTask();
        var completed = await Task.WhenAny(writeTask, Task.Delay(100));
        Assert.NotSame(writeTask, completed); // should not complete yet

        var first = await queue.DequeueAsync();
        Assert.Equal(1, first);

        await writeTask; // now completes
        var second = await queue.DequeueAsync();
        Assert.Equal(2, second);
    }
}
