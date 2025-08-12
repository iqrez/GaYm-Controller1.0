using System;
using Xunit;
using GaymController.Shared.Contracts;

namespace WireTests {
    public class WirePackTests {
        [Fact]
        public void HelloFrameMatchesGolden() {
            Span<byte> buf = stackalloc byte[12];
            var len = Wire.PackHello(buf);
            Assert.Equal(12, len);
            var exp = new byte[]{0x0C,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x00,0x00,0x00};
            Assert.True(buf.Slice(0,len).SequenceEqual(exp));
        }
        [Fact]
        public void SetStateFrameMatchesGolden() {
            Span<byte> buf = stackalloc byte[32];
            var state = GamepadState.Neutral;
            var len = Wire.PackSetState(buf, 0x1122334455667788, state);
            var exp = new byte[]{
                0x20,0x00,0x00,0x00,0x14,0x00,0x00,0x00,
                0x88,0x77,0x66,0x55,0x44,0x33,0x22,0x11,
                0xFF,0x7F,0xFF,0x7F,0xFF,0x7F,0xFF,0x7F,
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00
            };
            Assert.Equal(exp.Length, len);
            Assert.True(buf.Slice(0,len).SequenceEqual(exp));
        }
        [Fact]
        public void RumbleEventFrameMatchesGolden() {
            Span<byte> buf = stackalloc byte[20];
            var len = Wire.PackRumbleEvent(buf, 0x0102030405060708, 0x0A0B, 0x0C0D);
            var exp = new byte[]{
                0x14,0x00,0x00,0x00,0x29,0x00,0x00,0x00,
                0x08,0x07,0x06,0x05,0x04,0x03,0x02,0x01,
                0x0B,0x0A,0x0D,0x0C
            };
            Assert.Equal(exp.Length, len);
            Assert.True(buf.Slice(0,len).SequenceEqual(exp));
        }
    }
}
