# Test-Driven Development (TDD) - Videoconferencia UDP Multicast

**Generado por:** tdd-agent  
**Fecha:** 2025-10-10  
**Basado en:** quality-assurance.md, historias-usuario.md  

---

## 1. Estrategia de Testing

Este documento define los test cases siguiendo metodología TDD (Red-Green-Refactor) para el sistema de videoconferencia UDP Multicast.

### 1.1 Framework de Testing

- **Framework:** xUnit
- **Mocking:** Moq
- **Cobertura:** Coverlet
- **Assertions:** FluentAssertions

### 1.2 Estructura de Tests

```
tests/
├── VideoStreaming.UnitTests/
│   ├── Server/
│   │   ├── MulticastServerTests.cs
│   │   ├── CameraServiceTests.cs
│   │   └── JpegEncoderTests.cs
│   ├── Client/
│   │   ├── MulticastClientTests.cs
│   │   ├── FrameDecoderTests.cs
│   │   └── MetricsCollectorTests.cs
│   └── Shared/
│       ├── PacketHeaderTests.cs
│       └── PacketSerializerTests.cs
├── VideoStreaming.IntegrationTests/
│   ├── UdpMulticastTests.cs
│   └── EndToEndStreamingTests.cs
└── VideoStreaming.PerformanceTests/
    └── ThroughputBenchmarks.cs
```

---

## 2. Test Cases por Funcionalidad

### 2.1 Servidor de Video - Multicast UDP

#### Test 1: Unirse al grupo multicast
**Escenario:** Servidor se une al grupo multicast al iniciar transmisión

```csharp
[Fact]
public void Server_WhenStartingTransmission_ShouldJoinMulticastGroup()
{
    // Arrange
    var multicastAddress = IPAddress.Parse("224.0.0.1");
    var port = 8080;
    var server = new VideoStreamingServer(multicastAddress, port);
    
    // Act
    server.StartTransmission();
    
    // Assert
    server.IsConnectedToMulticastGroup.Should().BeTrue();
    server.MulticastAddress.Should().Be(multicastAddress);
}
```

**Estado:** 🔴 RED (test debe fallar inicialmente)

#### Test 2: Convertir frame a JPEG
**Escenario:** Cada frame capturado se convierte a JPEG antes de enviar

```csharp
[Fact]
public void Server_WhenFrameCaptured_ShouldConvertToJpeg()
{
    // Arrange
    var mockCamera = new Mock<IFrameSource>();
    var testFrame = CreateTestBitmap(320, 240);
    var encoder = new JpegFrameEncoder();
    
    // Act
    var jpegBytes = encoder.Encode(testFrame);
    
    // Assert
    jpegBytes.Should().NotBeNull();
    jpegBytes.Length.Should().BeLessThan(testFrame.Width * testFrame.Height * 3); // Compresión
    IsValidJpeg(jpegBytes).Should().BeTrue();
}
```

**Estado:** 🔴 RED

#### Test 3: Enviar a tasa configurada (20 FPS)
**Escenario:** El servidor envía aproximadamente 20 datagramas/segundo

```csharp
[Fact]
public async Task Server_WhenTransmitting_ShouldSendAtConfiguredFrameRate()
{
    // Arrange
    var server = new VideoStreamingServer();
    server.TargetFps = 20;
    var packetsSent = new List<DateTime>();
    server.OnPacketSent += (sender, args) => packetsSent.Add(DateTime.UtcNow);
    
    // Act
    server.StartTransmission();
    await Task.Delay(5000); // Capturar 5 segundos
    server.StopTransmission();
    
    // Assert
    var actualFps = packetsSent.Count / 5.0;
    actualFps.Should().BeInRange(18, 22); // Tolerancia de ±10%
}
```

**Estado:** 🔴 RED

#### Test 4: Manejo de error de envío
**Escenario:** La aplicación no bloquea la UI si falla el envío

```csharp
[Fact]
public void Server_WhenNetworkFails_ShouldNotBlockUI()
{
    // Arrange
    var mockUdpClient = new Mock<IUdpClient>();
    mockUdpClient.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
                 .Throws<SocketException>();
    var server = new VideoStreamingServer(mockUdpClient.Object);
    
    // Act
    Action act = () => server.SendFrame(CreateTestFrame());
    
    // Assert
    act.Should().NotThrow();
    server.LastError.Should().NotBeNull();
}
```

**Estado:** 🔴 RED

---

### 2.2 Cliente de Video - Recepción y Reconstrucción

#### Test 5: Recibir y mostrar JPEG válido
**Escenario:** Cliente reconstruye imagen desde bytes JPEG

```csharp
[Fact]
public void Client_WhenReceivingValidJpeg_ShouldReconstructImage()
{
    // Arrange
    var decoder = new JpegFrameDecoder();
    var validJpegBytes = CreateValidJpegBytes();
    
    // Act
    var image = decoder.Decode(validJpegBytes);
    
    // Assert
    image.Should().NotBeNull();
    image.Width.Should().BeGreaterThan(0);
    image.Height.Should().BeGreaterThan(0);
}
```

**Estado:** 🔴 RED

#### Test 6: Ignorar datagramas corruptos
**Escenario:** Cliente descarta datagramas inválidos sin fallar

```csharp
[Fact]
public void Client_WhenReceivingCorruptedData_ShouldDiscardGracefully()
{
    // Arrange
    var decoder = new JpegFrameDecoder();
    var corruptedBytes = new byte[] { 0xFF, 0x00, 0xAB, 0xCD }; // No es JPEG válido
    
    // Act
    Action act = () => decoder.Decode(corruptedBytes);
    
    // Assert
    act.Should().NotThrow();
    decoder.LastDecodedImage.Should().BeNull();
}
```

**Estado:** 🔴 RED

---

### 2.3 Medición de Prestaciones

#### Test 7: Calcular latencia desde timestamp
**Escenario:** Latencia = tiempo_recepción - timestamp

```csharp
[Fact]
public void Metrics_WhenCalculatingLatency_ShouldUseTimestampDifference()
{
    // Arrange
    var metricsCollector = new MetricsCollector();
    var sendTime = DateTime.UtcNow.AddMilliseconds(-150);
    var receiveTime = DateTime.UtcNow;
    var packet = new Packet { Timestamp = sendTime };
    
    // Act
    var latency = metricsCollector.CalculateLatency(packet, receiveTime);
    
    // Assert
    latency.TotalMilliseconds.Should().BeApproximately(150, 10);
}
```

**Estado:** 🔴 RED

#### Test 8: Detectar paquete perdido por salto de secuencia
**Escenario:** Salto en seqNumber indica paquete perdido

```csharp
[Fact]
public void Metrics_WhenSequenceNumberJumps_ShouldDetectPacketLoss()
{
    // Arrange
    var metricsCollector = new MetricsCollector();
    metricsCollector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 1 });
    metricsCollector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 2 });
    
    // Act - Salto de 2 a 5 (perdidos: 3 y 4)
    metricsCollector.ProcessPacket(new Packet { ImageNumber = 1, SequenceNumber = 5 });
    
    // Assert
    metricsCollector.PacketsLost.Should().Be(2);
}
```

**Estado:** 🔴 RED

#### Test 9: Calcular jitter
**Escenario:** Jitter = variación entre latencias consecutivas

```csharp
[Fact]
public void Metrics_WhenComparingLatencies_ShouldCalculateJitter()
{
    // Arrange
    var metricsCollector = new MetricsCollector();
    metricsCollector.RecordLatency(TimeSpan.FromMilliseconds(100));
    metricsCollector.RecordLatency(TimeSpan.FromMilliseconds(150)); // +50ms
    
    // Act
    var jitter = metricsCollector.GetCurrentJitter();
    
    // Assert
    jitter.TotalMilliseconds.Should().BeApproximately(50, 5);
}
```

**Estado:** 🔴 RED

---

### 2.4 Cabecera de Paquete

#### Test 10: Serializar cabecera correctamente
**Escenario:** Cabecera incluye imageNumber, seqNumber, timestamp

```csharp
[Fact]
public void PacketHeader_WhenSerialized_ShouldContainAllFields()
{
    // Arrange
    var header = new PacketHeader
    {
        ImageNumber = 42,
        SequenceNumber = 7,
        Timestamp = DateTime.UtcNow,
        PayloadLength = 1024
    };
    
    // Act
    var serialized = PacketSerializer.Serialize(header);
    var deserialized = PacketSerializer.DeserializeHeader(serialized);
    
    // Assert
    deserialized.ImageNumber.Should().Be(42);
    deserialized.SequenceNumber.Should().Be(7);
    deserialized.PayloadLength.Should().Be(1024);
    deserialized.Timestamp.Should().BeCloseTo(header.Timestamp, TimeSpan.FromMilliseconds(1));
}
```

**Estado:** 🔴 RED

#### Test 11: Tamaño de cabecera fijo
**Escenario:** La cabecera debe tener tamaño predecible

```csharp
[Fact]
public void PacketHeader_WhenSerialized_ShouldHaveFixedSize()
{
    // Arrange
    var header1 = new PacketHeader { ImageNumber = 1, SequenceNumber = 1 };
    var header2 = new PacketHeader { ImageNumber = 999, SequenceNumber = 999 };
    
    // Act
    var bytes1 = PacketSerializer.Serialize(header1);
    var bytes2 = PacketSerializer.Serialize(header2);
    
    // Assert
    bytes1.Length.Should().Be(bytes2.Length);
    PacketHeader.HEADER_SIZE.Should().Be(bytes1.Length);
}
```

**Estado:** 🔴 RED

---

### 2.5 Chat Multicast

#### Test 12: Enviar mensaje válido
**Escenario:** Mensaje no vacío se envía por UDP

```csharp
[Fact]
public void Chat_WhenSendingNonEmptyMessage_ShouldSendUdpPacket()
{
    // Arrange
    var mockUdpClient = new Mock<IUdpClient>();
    var chatService = new ChatService(mockUdpClient.Object);
    var message = "Hola mundo";
    
    // Act
    chatService.SendMessage(message);
    
    // Assert
    mockUdpClient.Verify(x => x.Send(
        It.Is<byte[]>(b => Encoding.Unicode.GetString(b) == message),
        It.IsAny<int>(),
        It.IsAny<IPEndPoint>()
    ), Times.Once);
}
```

**Estado:** 🔴 RED

#### Test 13: No enviar mensajes vacíos
**Escenario:** Mensajes vacíos o solo espacios no se envían

```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData("\t\n")]
public void Chat_WhenSendingEmptyMessage_ShouldNotSendPacket(string emptyMessage)
{
    // Arrange
    var mockUdpClient = new Mock<IUdpClient>();
    var chatService = new ChatService(mockUdpClient.Object);
    
    // Act
    chatService.SendMessage(emptyMessage);
    
    // Assert
    mockUdpClient.Verify(x => x.Send(
        It.IsAny<byte[]>(),
        It.IsAny<int>(),
        It.IsAny<IPEndPoint>()
    ), Times.Never);
}
```

**Estado:** 🔴 RED

---

### 2.6 Audio con A-law

#### Test 14: Codificar audio con A-law
**Escenario:** Buffer PCM se codifica a A-law reduciendo tamaño

```csharp
[Fact]
public void Audio_WhenEncodingWithALaw_ShouldReduceBufferSize()
{
    // Arrange
    var pcmBuffer = new byte[1600]; // 100ms @ 8000Hz, 16-bit mono
    new Random().NextBytes(pcmBuffer);
    
    // Act
    var encoded = ALawEncoder.Encode(pcmBuffer);
    
    // Assert
    encoded.Length.Should().Be(pcmBuffer.Length / 2); // A-law reduce a la mitad
}
```

**Estado:** 🔴 RED

#### Test 15: Decodificar audio A-law
**Escenario:** Audio A-law se decodifica a PCM para reproducción

```csharp
[Fact]
public void Audio_WhenDecodingALaw_ShouldRestorePCM()
{
    // Arrange
    var originalPcm = new short[800];
    for (int i = 0; i < originalPcm.Length; i++)
        originalPcm[i] = (short)(i % 1000);
    
    var encoded = ALawEncoder.Encode(originalPcm);
    
    // Act
    var decoded = ALawDecoder.Decode(encoded);
    
    // Assert
    decoded.Length.Should().Be(originalPcm.Length);
    // Nota: A-law es lossy, tolerancia necesaria
    for (int i = 0; i < 10; i++)
        decoded[i].Should().BeCloseTo(originalPcm[i], 100);
}
```

**Estado:** 🔴 RED

---

## 3. Implementación de Clases Base (TDD - Green Phase)

### 3.1 PacketHeader.cs

```csharp
public class PacketHeader
{
    public const int HEADER_SIZE = 24; // 4+4+8+4+4 bytes
    
    public int ImageNumber { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public int PayloadLength { get; set; }
    
    public byte[] Serialize()
    {
        var buffer = new byte[HEADER_SIZE];
        using (var ms = new MemoryStream(buffer))
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(ImageNumber);
            writer.Write(SequenceNumber);
            writer.Write(Timestamp.ToBinary());
            writer.Write(PayloadLength);
        }
        return buffer;
    }
    
    public static PacketHeader Deserialize(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            return new PacketHeader
            {
                ImageNumber = reader.ReadInt32(),
                SequenceNumber = reader.ReadInt32(),
                Timestamp = DateTime.FromBinary(reader.ReadInt64()),
                PayloadLength = reader.ReadInt32()
            };
        }
    }
}
```

### 3.2 MetricsCollector.cs

```csharp
public class MetricsCollector
{
    private readonly List<TimeSpan> _latencies = new();
    private int _lastSequenceNumber = -1;
    private int _currentImageNumber = -1;
    
    public int PacketsLost { get; private set; }
    public TimeSpan AverageLatency => 
        _latencies.Any() ? TimeSpan.FromMilliseconds(_latencies.Average(l => l.TotalMilliseconds)) : TimeSpan.Zero;
    
    public TimeSpan CalculateLatency(Packet packet, DateTime receiveTime)
    {
        return receiveTime - packet.Timestamp;
    }
    
    public void ProcessPacket(Packet packet)
    {
        if (packet.ImageNumber != _currentImageNumber)
        {
            _currentImageNumber = packet.ImageNumber;
            _lastSequenceNumber = -1;
        }
        
        if (_lastSequenceNumber >= 0)
        {
            var expectedSeq = _lastSequenceNumber + 1;
            if (packet.SequenceNumber > expectedSeq)
            {
                PacketsLost += (packet.SequenceNumber - expectedSeq);
            }
        }
        
        _lastSequenceNumber = packet.SequenceNumber;
    }
    
    public void RecordLatency(TimeSpan latency)
    {
        _latencies.Add(latency);
    }
    
    public TimeSpan GetCurrentJitter()
    {
        if (_latencies.Count < 2) return TimeSpan.Zero;
        
        var last = _latencies[_latencies.Count - 1];
        var previous = _latencies[_latencies.Count - 2];
        return TimeSpan.FromMilliseconds(Math.Abs(last.TotalMilliseconds - previous.TotalMilliseconds));
    }
}
```

### 3.3 JpegFrameEncoder.cs

```csharp
public class JpegFrameEncoder
{
    private readonly long _quality;
    
    public JpegFrameEncoder(long quality = 75L)
    {
        _quality = quality;
    }
    
    public byte[] Encode(Bitmap frame)
    {
        using (var ms = new MemoryStream())
        {
            var encoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, _quality);
            
            frame.Save(ms, encoder, encoderParams);
            return ms.ToArray();
        }
    }
    
    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
    }
}
```

### 3.4 JpegFrameDecoder.cs

```csharp
public class JpegFrameDecoder
{
    public Bitmap LastDecodedImage { get; private set; }
    
    public Bitmap Decode(byte[] jpegBytes)
    {
        try
        {
            using (var ms = new MemoryStream(jpegBytes))
            {
                LastDecodedImage = new Bitmap(Image.FromStream(ms));
                return LastDecodedImage;
            }
        }
        catch (ArgumentException)
        {
            // Datos corruptos o no es JPEG válido
            LastDecodedImage = null;
            return null;
        }
    }
}
```

---

## 4. Tests de Integración

### 4.1 Test End-to-End

```csharp
[Fact]
public async Task Integration_ServerToClient_ShouldTransmitFrameSuccessfully()
{
    // Arrange
    var multicastAddress = IPAddress.Parse("224.0.0.1");
    var port = 18080;
    
    var server = new VideoStreamingServer(multicastAddress, port);
    var client = new VideoStreamingClient(multicastAddress, port);
    
    Bitmap receivedFrame = null;
    client.OnFrameReceived += (sender, frame) => receivedFrame = frame;
    
    // Act
    server.StartTransmission();
    client.StartReceiving();
    
    var testFrame = CreateTestBitmap(320, 240);
    server.SendFrame(testFrame);
    
    await Task.Delay(500); // Esperar recepción
    
    // Assert
    receivedFrame.Should().NotBeNull();
    receivedFrame.Width.Should().Be(320);
    receivedFrame.Height.Should().Be(240);
    
    // Cleanup
    server.StopTransmission();
    client.StopReceiving();
}
```

---

## 5. Configuración de Coverage

### 5.1 coverlet.runsettings

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <Exclude>[*.Tests]*,[*.Mock]*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### 5.2 Objetivo de Cobertura

- **Capa de Dominio:** >= 90%
- **Servicios:** >= 80%
- **UI (Forms):** >= 50% (principalmente eventos)

---

## 6. Mocks y Stubs

### 6.1 IUdpClient (Interface para mocking)

```csharp
public interface IUdpClient : IDisposable
{
    void JoinMulticastGroup(IPAddress multicastAddress);
    int Send(byte[] dgram, int bytes, IPEndPoint endPoint);
    byte[] Receive(ref IPEndPoint remoteEP);
}
```

### 6.2 IFrameSource (Mock para cámara)

```csharp
public class MockFrameSource : IFrameSource
{
    public event Action<IFrameSource, Frame, double> NewFrame;
    
    public void SimulateFrame(Bitmap image)
    {
        var frame = new Frame { Image = image, Timestamp = DateTime.UtcNow };
        NewFrame?.Invoke(this, frame, 20.0);
    }
    
    public void StartFrameCapture() { }
    public void StopFrameCapture() { }
}
```

---

## 7. Comandos de Ejecución

### 7.1 Ejecutar todos los tests

```bash
dotnet test
```

### 7.2 Ejecutar con cobertura

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### 7.3 Generar reporte HTML de cobertura

```bash
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

### 7.4 Ejecutar solo tests unitarios

```bash
dotnet test --filter "FullyQualifiedName~UnitTests"
```

---

## 8. Métricas de Éxito

| Métrica | Objetivo | Estado Actual |
|---------|----------|---------------|
| Tests Unitarios | >= 50 tests | 🔴 15 tests definidos |
| Cobertura Dominio | >= 90% | 🔴 0% (no implementado) |
| Tests Integración | >= 5 tests | 🔴 1 test definido |
| Tests Performance | >= 2 benchmarks | 🔴 0 tests |
| Todos los tests pasan | 100% | 🔴 N/A (fase RED) |

---

## 9. Roadmap TDD

### Fase 1: RED (Semana 1) ✅ COMPLETADO
- [x] Definir todos los test cases
- [x] Escribir tests que fallen
- [x] Documentar expectativas

### Fase 2: GREEN (Semana 2-3) 🟡 EN PROGRESO
- [ ] Implementar clases mínimas para pasar tests
- [ ] Ejecutar tests y verificar que pasan
- [ ] Alcanzar 80% cobertura

### Fase 3: REFACTOR (Semana 4)
- [ ] Optimizar código
- [ ] Eliminar duplicación
- [ ] Mejorar nombres y estructura
- [ ] Verificar que tests siguen pasando

### Fase 4: CONTINUOUS (Ongoing)
- [ ] Añadir nuevos tests para nuevas features
- [ ] Mantener cobertura > 80%
- [ ] Ejecutar tests en CI/CD

---

**Documento generado por tdd-agent**  
**Modelo: sonnet**  
**Versión: 1.0**
