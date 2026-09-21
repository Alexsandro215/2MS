extends Control
##
## PRUEBA DEL RELOJ DE AUDIO — E.N.M.A.
##
## Responde a la unica pregunta del proyecto que no se puede contestar leyendo:
## ¿puede este motor mantener un pulso enganchado a la musica durante una
## cancion entera, en esta maquina concreta?
##
## Mide tres relojes a la vez para enseñar por que importa:
##
##   1. RELOJ DE AUDIO   — posicion real dentro del stream, corregida por el
##                          tiempo transcurrido desde la ultima mezcla y por
##                          la latencia de salida. Es el correcto.
##   2. RELOJ DEL SISTEMA— monotono, independiente del audio.
##   3. RELOJ DE FRAMES  — acumular delta cada fotograma. Es el metodo
##                          INCORRECTO, y esta aqui para que veas cuanto deriva.
##
## Ver docs/arquitectura.md  5.
##
## Controles:  ESPACIO = golpear al pulso  ·  R = reiniciar  ·  S = guardar informe
##

const BPM_POR_DEFECTO := 120.0
const PISTA_POR_DEFECTO := "res://audio/clics_120bpm.wav"

# Ventana considerada "acierto" para el recuento, en segundos.
const VENTANA_BUENA := 0.045   # 45 ms, tipico de un juego de ritmo permisivo
const VENTANA_PERFECTA := 0.020 # 20 ms

@onready var _reproductor: AudioStreamPlayer = $Reproductor

var _bpm := BPM_POR_DEFECTO
var _periodo := 60.0 / BPM_POR_DEFECTO

# --- relojes ---
var _sistema_inicio_us := 0
var _reloj_frames := 0.0
var _audio_inicio := 0.0

# --- muestras ---
var _desfases: Array[float] = []      # desfase de cada pulsacion, en segundos
var _muestras_deriva: Array = []      # [segundo_audio, deriva_frames, deriva_sistema]
var _ultimo_registro := 0.0

var _corriendo := false
var _latencia_salida := 0.0

# --- dibujo ---
var _etiqueta: RichTextLabel
var _ultimo_pulso_visto := -1
var _destello := 0.0
var _destello_tap := 0.0


func _ready() -> void:
	_construir_interfaz()
	_cargar_pista(PISTA_POR_DEFECTO)
	set_process(true)


func _construir_interfaz() -> void:
	var fondo := ColorRect.new()
	fondo.color = Color(0.07, 0.07, 0.09)
	fondo.set_anchors_preset(Control.PRESET_FULL_RECT)
	fondo.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(fondo)

	_etiqueta = RichTextLabel.new()
	_etiqueta.bbcode_enabled = true
	_etiqueta.scroll_active = false
	_etiqueta.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_etiqueta.offset_left = 32
	_etiqueta.offset_top = 24
	_etiqueta.offset_right = -32
	_etiqueta.offset_bottom = -260
	_etiqueta.set_anchors_preset(Control.PRESET_FULL_RECT, true)
	_etiqueta.add_theme_font_size_override("normal_font_size", 17)
	_etiqueta.add_theme_font_size_override("mono_font_size", 17)
	add_child(_etiqueta)


func _cargar_pista(ruta: String) -> void:
	if not ResourceLoader.exists(ruta):
		push_error("No se encuentra la pista: %s" % ruta)
		return
	var stream := load(ruta)
	if stream == null:
		push_error("No se pudo cargar: %s" % ruta)
		return
	_reproductor.stream = stream
	_arrancar()


func _arrancar() -> void:
	_desfases.clear()
	_muestras_deriva.clear()
	_reloj_frames = 0.0
	_ultimo_registro = 0.0
	_ultimo_pulso_visto = -1

	# La latencia de salida se consulta UNA vez al arrancar: es propiedad del
	# dispositivo, no del fotograma.
	_latencia_salida = AudioServer.get_output_latency()

	_reproductor.play()
	_sistema_inicio_us = Time.get_ticks_usec()
	_audio_inicio = 0.0
	_corriendo = true


## El reloj bueno. Esta es la receta canonica y es TODO el secreto:
## la posicion que reporta el reproductor esta cuantizada al bloque de mezcla,
## asi que hay que sumarle lo que ha pasado desde esa mezcla y restarle lo que
## el audio tarda en salir por los altavoces.
func _posicion_audio() -> float:
	return _reproductor.get_playback_position() \
		+ AudioServer.get_time_since_last_mix() \
		- _latencia_salida


func _posicion_sistema() -> float:
	return float(Time.get_ticks_usec() - _sistema_inicio_us) / 1_000_000.0


func _process(delta: float) -> void:
	if not _corriendo:
		return

	# El reloj malo, a proposito: sumar deltas de fotograma.
	_reloj_frames += delta

	var t_audio := _posicion_audio()
	var t_sistema := _posicion_sistema()

	# Muestreo de deriva una vez por segundo
	if t_audio - _ultimo_registro >= 1.0:
		_ultimo_registro = t_audio
		_muestras_deriva.append([
			t_audio,
			(_reloj_frames - t_audio) * 1000.0,
			(t_sistema - t_audio) * 1000.0,
		])

	# Destello visual en cada pulso
	var pulso := int(floor(t_audio / _periodo))
	if pulso != _ultimo_pulso_visto and t_audio >= 0.0:
		_ultimo_pulso_visto = pulso
		_destello = 1.0
	_destello = maxf(0.0, _destello - delta * 6.0)
	_destello_tap = maxf(0.0, _destello_tap - delta * 5.0)

	if not _reproductor.playing:
		_corriendo = false

	_actualizar_texto(t_audio)
	queue_redraw()


func _unhandled_input(evento: InputEvent) -> void:
	if evento.is_action_pressed("tap"):
		_registrar_pulsacion()
	elif evento.is_action_pressed("reiniciar"):
		_arrancar()
	elif evento.is_action_pressed("guardar"):
		_guardar_informe()


func _registrar_pulsacion() -> void:
	if not _corriendo:
		return
	var t := _posicion_audio()
	# Distancia con signo al pulso mas cercano. Negativo = te adelantaste.
	var desfase: float = t - round(t / _periodo) * _periodo
	_desfases.append(desfase)
	_destello_tap = 1.0


# --------------------------------------------------------------------------
#  Estadistica
# --------------------------------------------------------------------------

func _media(v: Array) -> float:
	if v.is_empty():
		return 0.0
	var s := 0.0
	for x in v:
		s += x
	return s / v.size()


func _desviacion(v: Array) -> float:
	if v.size() < 2:
		return 0.0
	var m := _media(v)
	var s := 0.0
	for x in v:
		s += (x - m) * (x - m)
	return sqrt(s / (v.size() - 1))


func _dentro(v: Array, ventana: float) -> int:
	var n := 0
	for x in v:
		if absf(x) <= ventana:
			n += 1
	return n


# --------------------------------------------------------------------------
#  Presentacion
# --------------------------------------------------------------------------

func _actualizar_texto(t_audio: float) -> void:
	var n := _desfases.size()
	var media_ms := _media(_desfases) * 1000.0
	var jitter_ms := _desviacion(_desfases) * 1000.0

	var deriva_frames_ms := 0.0
	var deriva_sistema_ms := 0.0
	if not _muestras_deriva.is_empty():
		var u = _muestras_deriva[-1]
		deriva_frames_ms = u[1]
		deriva_sistema_ms = u[2]

	var lineas := []
	lineas.append("[b]PRUEBA DEL RELOJ DE AUDIO — E.N.M.A.[/b]")
	lineas.append("")
	lineas.append("  BPM %.0f   ·   pulso cada %.1f ms   ·   %s" % [
		_bpm, _periodo * 1000.0,
		"sonando" if _corriendo else "[color=#ff8866]detenido[/color]"])
	lineas.append("")
	lineas.append("[b]HARDWARE[/b]")
	lineas.append("  Latencia de salida reportada . %7.2f ms" % (_latencia_salida * 1000.0))
	lineas.append("  Frecuencia de mezcla ......... %7.0f Hz" % AudioServer.get_mix_rate())
	lineas.append("  Fotogramas por segundo ....... %7.0f  (un frame = %.1f ms)" % [
		Engine.get_frames_per_second(),
		1000.0 / maxf(1.0, Engine.get_frames_per_second())])
	lineas.append("")
	lineas.append("[b]TUS PULSACIONES[/b]   (%d)" % n)
	if n == 0:
		lineas.append("  [color=#888899]Pulsa ESPACIO al ritmo del clic.[/color]")
	else:
		var signo := "tarde" if media_ms > 0 else "pronto"
		lineas.append("  Desfase medio ................ %+7.2f ms   (vas %s)" % [media_ms, signo])
		lineas.append("  Jitter (desviacion tipica) ... %7.2f ms" % jitter_ms)
		lineas.append("  Dentro de %d ms .............. %3d de %d" % [
			int(VENTANA_PERFECTA * 1000), _dentro(_desfases, VENTANA_PERFECTA), n])
		lineas.append("  Dentro de %d ms .............. %3d de %d" % [
			int(VENTANA_BUENA * 1000), _dentro(_desfases, VENTANA_BUENA), n])
		lineas.append("")
		lineas.append("  [color=#88cc88]Offset de calibracion sugerido: %+.1f ms[/color]" % -media_ms)
	lineas.append("")
	lineas.append("[b]DERIVA[/b]   (transcurridos %d s)" % int(t_audio))
	lineas.append("  Reloj de sistema vs audio .... %+7.2f ms" % deriva_sistema_ms)
	lineas.append("  [color=#ff9966]Reloj de FRAMES vs audio ..... %+7.2f ms[/color]" % deriva_frames_ms)
	lineas.append("")
	lineas.append("[color=#888899]  El reloj de frames es el metodo incorrecto, y esta aqui")
	lineas.append("  para que veas cuanto se separa. Si esa cifra crece sin parar,")
	lineas.append("  eso es exactamente lo que sentiria el jugador como 'el juego miente'.[/color]")
	lineas.append("")
	lineas.append("[color=#666677]  ESPACIO golpear  ·  R reiniciar  ·  S guardar informe[/color]")

	_etiqueta.text = "\n".join(lineas)


func _draw() -> void:
	var w := size.x
	var h := size.y
	var base_y := h - 150.0

	# Circulo que late con el pulso
	var cx := w * 0.5
	var r := 34.0 + _destello * 26.0
	var col := Color(0.45, 0.75, 1.0).lerp(Color(1, 1, 1), _destello)
	draw_circle(Vector2(cx, base_y), r, col)

	# Marca de tu pulsacion
	if _destello_tap > 0.0:
		draw_arc(Vector2(cx, base_y), r + 22.0, 0.0, TAU, 48,
			Color(0.55, 1.0, 0.55, _destello_tap), 3.0)

	# Dispersion de las ultimas pulsaciones: cada punto es un golpe
	var y := h - 58.0
	var escala := 1400.0   # px por segundo de desfase
	draw_line(Vector2(w * 0.5, y - 26), Vector2(w * 0.5, y + 26), Color(1, 1, 1, 0.55), 2.0)

	# Bandas de ventana
	draw_rect(Rect2(w * 0.5 - VENTANA_BUENA * escala, y - 20,
		VENTANA_BUENA * 2 * escala, 40), Color(0.4, 0.8, 0.4, 0.10))
	draw_rect(Rect2(w * 0.5 - VENTANA_PERFECTA * escala, y - 20,
		VENTANA_PERFECTA * 2 * escala, 40), Color(0.4, 0.9, 0.5, 0.18))

	var desde: int = maxi(0, _desfases.size() - 120)
	for i in range(desde, _desfases.size()):
		var d: float = _desfases[i]
		var alfa: float = 0.25 + 0.75 * float(i - desde) / maxf(1.0, float(_desfases.size() - desde))
		draw_circle(Vector2(w * 0.5 + d * escala, y), 3.5, Color(0.6, 0.9, 1.0, alfa))


# --------------------------------------------------------------------------
#  Informe
# --------------------------------------------------------------------------

func _guardar_informe() -> void:
	var n := _desfases.size()
	var lineas := []
	lineas.append("INFORME DEL RELOJ DE AUDIO — E.N.M.A.")
	lineas.append("Fecha: %s" % Time.get_datetime_string_from_system())
	lineas.append("Motor: Godot %s" % Engine.get_version_info().get("string", "?"))
	lineas.append("SO   : %s" % OS.get_name())
	lineas.append("CPU  : %s" % OS.get_processor_name())
	lineas.append("Audio: driver %s" % ProjectSettings.get_setting("audio/driver/driver", "por defecto"))
	lineas.append("")
	lineas.append("BPM de la pista        : %.2f" % _bpm)
	lineas.append("Latencia de salida     : %.3f ms" % (_latencia_salida * 1000.0))
	lineas.append("Frecuencia de mezcla   : %.0f Hz" % AudioServer.get_mix_rate())
	lineas.append("FPS medio              : %.0f" % Engine.get_frames_per_second())
	lineas.append("")
	lineas.append("Pulsaciones            : %d" % n)
	if n > 0:
		lineas.append("Desfase medio          : %+.3f ms" % (_media(_desfases) * 1000.0))
		lineas.append("Jitter (desv. tipica)  : %.3f ms" % (_desviacion(_desfases) * 1000.0))
		lineas.append("Dentro de %d ms        : %d (%.1f%%)" % [
			int(VENTANA_PERFECTA * 1000), _dentro(_desfases, VENTANA_PERFECTA),
			100.0 * _dentro(_desfases, VENTANA_PERFECTA) / n])
		lineas.append("Dentro de %d ms        : %d (%.1f%%)" % [
			int(VENTANA_BUENA * 1000), _dentro(_desfases, VENTANA_BUENA),
			100.0 * _dentro(_desfases, VENTANA_BUENA) / n])
		lineas.append("Offset sugerido        : %+.2f ms" % (-_media(_desfases) * 1000.0))
	lineas.append("")
	lineas.append("DERIVA  (segundo, frames_vs_audio_ms, sistema_vs_audio_ms)")
	for m in _muestras_deriva:
		lineas.append("  %6.1f  %+9.3f  %+9.3f" % [m[0], m[1], m[2]])
	lineas.append("")
	lineas.append("DESFASES INDIVIDUALES (ms)")
	var fila := []
	for d in _desfases:
		fila.append("%+.1f" % (d * 1000.0))
		if fila.size() == 12:
			lineas.append("  " + "  ".join(fila))
			fila.clear()
	if not fila.is_empty():
		lineas.append("  " + "  ".join(fila))

	var ruta := "user://informe_reloj_%s.txt" % Time.get_datetime_string_from_system().replace(":", "-")
	var f := FileAccess.open(ruta, FileAccess.WRITE)
	if f:
		f.store_string("\n".join(lineas))
		f.close()
		print("Informe guardado en: ", ProjectSettings.globalize_path(ruta))
	else:
		push_error("No se pudo escribir el informe")
