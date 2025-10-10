using System.Drawing;
using System.Drawing.Imaging;

namespace VideoStreaming.UnitTests.Server;

/// <summary>
/// Tests para JpegFrameEncoder - Codificación de frames a JPEG
/// Estado TDD: 🔴 RED - Tests definidos, implementación pendiente
/// </summary>
public class JpegEncoderTests
{
    [Fact]
    public void Encode_WithValidBitmap_ShouldReturnJpegBytes()
    {
        // Arrange
        var encoder = new JpegFrameEncoder();
        var testFrame = CreateTestBitmap(320, 240);

        // Act
        var jpegBytes = encoder.Encode(testFrame);

        // Assert
        jpegBytes.Should().NotBeNull();
        jpegBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Encode_ShouldCompressImage()
    {
        // Arrange
        var encoder = new JpegFrameEncoder(quality: 75);
        var testFrame = CreateTestBitmap(320, 240);
        var uncompressedSize = testFrame.Width * testFrame.Height * 3; // RGB sin comprimir

        // Act
        var jpegBytes = encoder.Encode(testFrame);

        // Assert
        jpegBytes.Length.Should().BeLessThan(uncompressedSize);
    }

    [Fact]
    public void Encode_ProducedBytes_ShouldBeValidJpeg()
    {
        // Arrange
        var encoder = new JpegFrameEncoder();
        var testFrame = CreateTestBitmap(320, 240);

        // Act
        var jpegBytes = encoder.Encode(testFrame);

        // Assert
        // JPEG files start with 0xFF 0xD8 and end with 0xFF 0xD9
        jpegBytes[0].Should().Be(0xFF);
        jpegBytes[1].Should().Be(0xD8);
        jpegBytes[^2].Should().Be(0xFF);
        jpegBytes[^1].Should().Be(0xD9);
    }

    [Theory]
    [InlineData(25)] // Baja calidad
    [InlineData(75)] // Calidad media
    [InlineData(95)] // Alta calidad
    public void Encode_WithDifferentQuality_ShouldProduceDifferentSizes(long quality)
    {
        // Arrange
        var encoder = new JpegFrameEncoder(quality);
        var testFrame = CreateTestBitmap(320, 240);

        // Act
        var jpegBytes = encoder.Encode(testFrame);

        // Assert
        jpegBytes.Should().NotBeNull();
        // Mayor calidad = mayor tamaño (aproximadamente)
        if (quality > 50)
        {
            jpegBytes.Length.Should().BeGreaterThan(1000);
        }
    }

    [Fact]
    public void Encode_WithNullBitmap_ShouldThrowArgumentNullException()
    {
        // Arrange
        var encoder = new JpegFrameEncoder();

        // Act
        Action act = () => encoder.Encode(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private Bitmap CreateTestBitmap(int width, int height)
    {
        var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Blue);
            graphics.DrawRectangle(Pens.Red, 10, 10, width - 20, height - 20);
        }
        return bitmap;
    }
}

/// <summary>
/// Placeholder para JpegFrameEncoder - Estado TDD: 🔴 RED
/// </summary>
public class JpegFrameEncoder
{
    private readonly long _quality;

    public JpegFrameEncoder(long quality = 75)
    {
        _quality = quality;
    }

    public byte[] Encode(Bitmap? frame)
    {
        throw new NotImplementedException("[AGENT] JpegFrameEncoder.Encode not implemented - TDD RED phase");
    }
}
