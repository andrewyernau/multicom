using System;
using MultiCom.Shared.Audio;

namespace VideoStreaming.UnitTests.Shared;

public class AudioFrameTests
{
    [Fact]
    public void ToPacket_ShouldIncludeSenderId()
    {
        var sender = Guid.NewGuid();
        var payload = new byte[] { 1, 2, 3, 4 };
        var frame = new AudioFrame(sender, 5, payload, DateTime.UtcNow.Ticks);

        var packet = frame.ToPacket();
        AudioFrame.TryParse(packet, packet.Length, out var parsed).Should().BeTrue();
        parsed.Should().NotBeNull();
        parsed!.SenderId.Should().Be(sender);
        parsed.SequenceNumber.Should().Be(5);
        parsed.Payload.Should().Equal(payload);
    }

    [Fact]
    public void HeaderSize_ShouldExposeExpectedValue()
    {
        AudioFrame.HEADER_SIZE.Should().Be(36);
    }
}
