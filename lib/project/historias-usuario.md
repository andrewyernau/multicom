
## Historias de Usuario — Servicio de Videoconferencia

A continuación se recogen las historias de usuario organizadas por subsistema. Cada fila sigue el formato "Como... / Quiero... / Para...".

## Servidor de Video Streaming

| Como... | Quiero... | Para... |
|---|---|---|
| Servidor de Vídeo | capturar el stream de mi webcam | tener una fuente de imágenes en tiempo real para la transmisión. |
| Servidor de Vídeo | detectar las cámaras de vídeo disponibles | poder seleccionar la cámara que usaré para la transmisión. |
| Servidor de Vídeo | inicializar la captura de vídeo con un tamaño de frame y FPS específicos (p. ej., 320x240 a 20 FPS) | optimizar el rendimiento y ancho de banda de la transmisión. |
| Servidor de Vídeo | convertir cada frame capturado a formato JPEG | reducir el tamaño del dato a transmitir. |
| Servidor de Vídeo | unirme/crear un grupo Multicast UDP y enviar los frames JPEG | difundir el vídeo simultáneamente a múltiples clientes de forma eficiente. |
| Servidor de Vídeo | invocar el envío por multicast inmediatamente después de dibujar el frame en la pantalla | mantener la latencia de la transmisión al mínimo. |

## Cliente de Video Streaming

| Como... | Quiero... | Para... |
|---|---|---|
| Cliente de Vídeo | unirme al grupo Multicast UDP del servidor | poder recibir los datagramas de vídeo enviados por el servidor. |
| Cliente de Vídeo | recibir los datagramas UDP en un hilo dedicado (Task/Thread) | evitar bloquear la interfaz de usuario mientras espero y proceso los datos de la red. |
| Cliente de Vídeo | reconstruir una imagen Bitmap a partir de los bytes recibidos (JPEG) | poder visualizar el frame enviado por el servidor. |
| Cliente de Vídeo | mostrar la imagen reconstruida en mi interfaz de usuario | ver el vídeo en tiempo real. |

## Evaluación de Prestaciones

| Como... | Quiero... | Para... |
|---|---|---|
| Servidor de Vídeo | incluir una cabecera con número de imagen, número de secuencia y timestamp en cada paquete enviado | permitir al cliente la reconstrucción de imagen y la medición de calidad de servicio. |
| Servidor de Vídeo | elegir un tamaño óptimo de payload (fragmento JPEG) | garantizar una visualización fluida del vídeo. |
| Cliente de Vídeo | leer la información de la cabecera de cada paquete recibido | poder calcular las métricas de calidad de servicio. |
| Cliente de Vídeo | recomponer la imagen JPEG a partir de múltiples payloads secuenciados | mostrar la imagen completa cuando esta viene fragmentada en varios paquetes. |
| Cliente de Vídeo | medir la latencia, el jitter y los paquetes perdidos | evaluar el rendimiento del servicio de videoconferencia. |
| Cliente de Vídeo | mostrar gráficamente en tiempo real o mediante logs las métricas de calidad (latencia, jitter, paquetes perdidos) | tener una visión clara y continua del rendimiento de la red. |

## Funcionalidad Opcional (Anexos)

| Como... | Quiero... | Para... |
|---|---|---|
| Usuario del Chat | poder enviar un mensaje de texto al grupo multicast | comunicarme con otros usuarios de forma simultánea. |
| Usuario del Chat | recibir los mensajes enviados al grupo multicast | ver la conversación en tiempo real. |
| Servidor/Cliente de Chat | recibir datos en un hilo dedicado (Thread o Task) | evitar bloquear la interfaz de usuario mientras se esperan nuevos mensajes. |
| Emisor de Audio | capturar el audio de mi micrófono | transmitir mi voz al grupo multicast. |
| Emisor de Audio | codificar el audio capturado con el algoritmo A-law | reducir el tamaño del buffer y optimizar el envío por UDP. |
| Receptor de Audio | decodificar el audio recibido (payload) | obtener el formato PCM necesario para la reproducción. |
| Receptor de Audio | agregar las muestras decodificadas a un proveedor de buffer (BufferedWaveProvider) | reproducir el audio de forma continua y fluida por mis altavoces. |

---