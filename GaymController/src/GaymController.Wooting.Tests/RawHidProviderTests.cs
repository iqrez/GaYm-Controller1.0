using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GaymController.Shared.Mapping;
using Xunit;

namespace GaymController.Wooting.Tests {
    public class RawHidProviderTests {
        [Fact]
        public void MapsBytesToEvents(){
            var reports = new byte[]{0,10,20,0,30,40}; // two reports of 3 bytes
            var opened=false;
            Stream Open(){
                if(opened) throw new IOException("reopen");
                opened=true;
                return new MemoryStream(reports);
            }
            var map = new Dictionary<int,string>{{1,"A"},{2,"B"}};
            var provider = new RawHidProvider(0x1234,0x5678,3,map,Open);
            var events = new List<InputEvent>();
            provider.OnKeyAnalog += (_,e)=>events.Add(e);
            provider.Start();
            SpinWait.SpinUntil(()=>events.Count>=4,1000);
            provider.Stop();
            Assert.Equal(4, events.Count);
            Assert.Equal("A", events[0].Source);
            Assert.Equal(10/255.0, events[0].Value,3);
            Assert.Equal("B", events[1].Source);
            Assert.Equal(20/255.0, events[1].Value,3);
        }
    }
}
