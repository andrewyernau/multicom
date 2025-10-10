namespace VideoStreaming.UnitTests.Client;

/// <summary>
/// Tests para MetricsCollector - Colección de métricas de prestaciones
/// Estado TDD: 🔴 RED - Tests definidos, implementación pendiente
/// </summary>
public class MetricsCollectorTests
{
    [Fact]
    public void CalculateLatency_ShouldReturnDifferenceBetweenTimestamps()
    {
        // Arrange
        var collector = new MetricsCollector();
        var sendTime = DateTime.UtcNow.AddMilliseconds(-150);
        var receiveTime = DateTime.UtcNow;
        var packet = new Packet { Timestamp = sendTime };

        // Act
        var latency = collector.CalculateLatency(packet, receiveTime);

        // Assert
        latency.TotalMilliseconds.Should().BeApproximately(150, 10);
    }

    [Fact]
    public void ProcessPacket_WhenSequenceNumberJumps_ShouldDetectPacketLoss()
    {
        // Arrange
        var collector = new MetricsCollector();
        collector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 1 });
        collector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 2 });

        // Act - Salto de 2 a 5 (perdidos: 3 y 4)
        collector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 5 });

        // Assert
        collector.PacketsLost.Should().Be(2);
    }

    [Fact]
    public void ProcessPacket_WithConsecutiveSequences_ShouldNotDetectLoss()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        for (int i = 1; i <= 10; i++)
        {
            collector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = i });
        }

        // Assert
        collector.PacketsLost.Should().Be(0);
    }

    [Fact]
    public void ProcessPacket_WhenImageNumberChanges_ShouldResetSequenceTracking()
    {
        // Arrange
        var collector = new MetricsCollector();
        collector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 5 });

        // Act - Nueva imagen, puede empezar desde 1 sin ser pérdida
        collector.ProcessPacket(new Packet { ImageNumber = 2, SequenceNumber = 1 });

        // Assert
        collector.PacketsLost.Should().Be(0);
    }

    [Fact]
    public void RecordLatency_ShouldStoreLatencyValue()
    {
        // Arrange
        var collector = new MetricsCollector();
        var latency = TimeSpan.FromMilliseconds(100);

        // Act
        collector.RecordLatency(latency);

        // Assert
        collector.AverageLatency.Should().Be(latency);
    }

    [Fact]
    public void GetCurrentJitter_WithTwoLatencies_ShouldReturnDifference()
    {
        // Arrange
        var collector = new MetricsCollector();
        collector.RecordLatency(TimeSpan.FromMilliseconds(100));
        collector.RecordLatency(TimeSpan.FromMilliseconds(150));

        // Act
        var jitter = collector.GetCurrentJitter();

        // Assert
        jitter.TotalMilliseconds.Should().BeApproximately(50, 1);
    }

    [Fact]
    public void GetCurrentJitter_WithLessThanTwoLatencies_ShouldReturnZero()
    {
        // Arrange
        var collector = new MetricsCollector();
        collector.RecordLatency(TimeSpan.FromMilliseconds(100));

        // Act
        var jitter = collector.GetCurrentJitter();

        // Assert
        jitter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AverageLatency_WithMultipleRecords_ShouldCalculateCorrectAverage()
    {
        // Arrange
        var collector = new MetricsCollector();
        collector.RecordLatency(TimeSpan.FromMilliseconds(100));
        collector.RecordLatency(TimeSpan.FromMilliseconds(200));
        collector.RecordLatency(TimeSpan.FromMilliseconds(150));

        // Act
        var average = collector.AverageLatency;

        // Assert
        average.TotalMilliseconds.Should().BeApproximately(150, 1);
    }
}

/// <summary>
/// Placeholder classes - Estado TDD: 🔴 RED
/// </summary>
public class MetricsCollector
{
    public int PacketsLost { get; private set; }
    public TimeSpan AverageLatency { get; private set; }

    public TimeSpan CalculateLatency(Packet packet, DateTime receiveTime)
    {
        throw new NotImplementedException("[AGENT] MetricsCollector.CalculateLatency not implemented - TDD RED phase");
    }

    public void ProcessPacket(Packet packet)
    {
        throw new NotImplementedException("[AGENT] MetricsCollector.ProcessPacket not implemented - TDD RED phase");
    }

    public void RecordLatency(TimeSpan latency)
    {
        throw new NotImplementedException("[AGENT] MetricsCollector.RecordLatency not implemented - TDD RED phase");
    }

    public TimeSpan GetCurrentJitter()
    {
        throw new NotImplementedException("[AGENT] MetricsCollector.GetCurrentJitter not implemented - TDD RED phase");
    }
}

public class Packet
{
    public int ImageNumber { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
