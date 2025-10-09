Feature: Calidad - criterios mínimos (Gherkin)

  # Servidor de vídeo - multicast UDP
  Scenario: Unirse al grupo multicast y transmitir un frame JPEG
    Given el servidor está iniciado
    When el operador pulsa "Transmitir"
    Then el servidor se une al grupo multicast 224.0.0.1:UDP_PORT
    And cuando se captura un frame, éste se convierte a JPEG y se envía en un datagrama UDP al grupo

  Scenario: Transmisión a tasa configurada (20 FPS)
    Given la cámara está configurada a 20 FPS
    When la transmisión está activa
    Then el servidor envía aproximadamente 20 datagramas UDP por segundo

  Scenario: Manejo de error de envío
    Given la transmisión está activa
    And la red no permite el envío
    When el servidor intenta enviar un datagrama
    Then la aplicación no bloquea la UI
    And se registra un error informativo

  # Cliente de vídeo - recepción y reconstrucción
  Scenario: Recibir y mostrar un JPEG válido
    Given el cliente está unido al grupo multicast en el puerto UDP_PORT
    When el cliente recibe un datagrama UDP con payload JPEG válido
    Then el cliente reconstruye una Image/Bitmap desde los bytes
    And muestra la imagen en `pictureBoxDisplay` sin bloquear la interfaz

  Scenario: Ignorar datagramas corruptos
    Given el cliente recibe datagramas continuamente
    When un datagrama no contiene un JPEG válido
    Then el cliente descarta el datagrama y continúa sin fallar

  # Medición de prestaciones
  Scenario: Calcular latencia desde timestamp
    Given cada paquete incluye imageNumber, seqNumber y timestamp
    When el cliente extrae el timestamp de un paquete recibido
    Then la latencia se calcula como tiempo_de_recepción - timestamp

  Scenario: Detectar paquete perdido por salto de secuencia
    Given los paquetes de una misma imagen llevan seqNumber secuencial
    When el cliente detecta un salto en seqNumber (por ejemplo 1 -> 3)
    Then el cliente registra 1 paquete perdido

  Scenario: Calcular jitter básico
    Given el cliente tiene al menos dos mediciones de latencia
    When se comparan latencias consecutivas
    Then el cliente calcula el jitter como la variación entre ellas

  Scenario: Mostrar métricas en interfaz
    Given el cliente calcula latencia, jitter y paquetes perdidos
    When la aplicación está en ejecución
    Then estas métricas se muestran al usuario (numérico o gráfico)

  # Chat multicast
  Scenario: Enviar mensaje de chat válido
    Given el usuario escribe texto no vacío en el RichTextBox
    When el usuario pulsa "Enviar"
    Then el texto se codifica con Encoding.Unicode y se envía por UDP al grupo
    And el RichTextBox queda vacío

  Scenario: No enviar mensajes vacíos
    Given el RichTextBox está vacío o contiene solo espacios
    When el usuario pulsa "Enviar"
    Then no se envía ningún datagrama

  # Audio - A-law
  Scenario: Capturar y enviar audio codificado A-law
    Given la captura de audio está inicializada (mono, 8000 Hz, 16 bits)
    When WaveIn genera un buffer de audio
    Then el buffer se codifica con A-law y se envía por UDP

  Scenario: Receptor decodifica y reproduce
    Given el receptor tiene WaveOut y BufferedWaveProvider configurados con la misma WaveFormat
    When llega un datagrama de audio A-law
    Then el receptor decodifica A-law a PCM y añade las muestras al buffer para reproducción