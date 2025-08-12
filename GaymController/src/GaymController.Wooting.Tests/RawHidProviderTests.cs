using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using GaymController.Wooting;
using GaymController.Shared.Mapping;
using Xunit;

sealed class QueueStream : System.IO.Stream {
    private readonly BlockingCollection<byte[]> _q = new();
    public void Enqueue(byte[] data) => _q.Add(data);
    public override int Read(byte[] buffer, int offset, int count) {
        if(!_q.TryTake(out var data, 1000)) return 0;
        var n = Math.Min(count, data.Length);
        Array.Copy(data,0,buffer,offset,n);
        return n;
    }
    public override void Write(byte[] buffer, int offset, int count) {
        var arr = new byte[count];
        Array.Copy(buffer,offset,arr,0,count);
        _q.Add(arr);
    }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing){ _q.CompleteAdding(); base.Dispose(disposing); }
}

public class RawHidProviderTests {
    [Fact]
    public void EmitsMappedEvents(){
        using var stream = new QueueStream();
        var map = new Dictionary<int,string>{{0,"Key0"}};
        using var provider = new RawHidProvider(() => stream, map, 1);
        var received = new List<InputEvent>();
        var evt = new ManualResetEvent(false);
        provider.OnKeyAnalog += (_, e) => { received.Add(e); evt.Set(); };
        provider.Start();
        stream.Write(new byte[]{255},0,1);
        evt.WaitOne(1000);
        provider.Stop();
        Assert.Single(received);
        Assert.Equal("Key0", received[0].Source);
        Assert.Equal(1.0, received[0].Value, 3);
    }
}
