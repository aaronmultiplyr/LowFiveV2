
using NUnit.Framework;
using LowFive.Core.Input;
using System;

public class InputStructTests
{
    [Test]
    public void RoundTrip_PackedBytes_Match()
    {
        var src = new InputFrame { tick = 123, data = new LFInputStruct { packed = 0xDEADBEEF_F00DBA5EUL } };

        Span<byte> buf = stackalloc byte[InputFrame.Size];
        src.ToBytes(buf);
        var dst = InputFrame.FromBytes(buf);

        Assert.AreEqual(src.tick, dst.tick);
        Assert.AreEqual(src.data, dst.data);
    }

    [Test]
    public void ButtonBit_SetAndGet_Works()
    {
        var s = new LFInputStruct();
        s.SetButton(3, true);
        Assert.IsTrue(s.GetButton(3));
        s.SetButton(3, false);
        Assert.IsFalse(s.GetButton(3));
    }
}
