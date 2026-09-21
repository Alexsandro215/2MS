extends Control
##
## PRUEBA DEL RELOJ DE AUDIO — E.N.M.A.
##
## Responde a la unica pregunta del proyecto que no se puede contestar leyendo:
## ¿puede este motor mantener un pulso enganchado a la musica durante una
## cancion entera, en esta maquina concreta?
##
## Ver docs/arquitectura.md  5.
##
## DOS FASES, porque no necesitan lo mismo:
##
##   FASE 1 — CALIBRACION (16 s).  Pulsas 32 veces. Mide latencia y jitter.
##            Termina sola. Hay contador.
##   FASE 2 — DERIVA (90 s).  NO hay que hacer nada, solo dejarlo sonar.
##            Mide si el reloj de audio se descuelga del tiempo real.
##            Hay cuenta atras y se guarda el informe solo al acabar.
##
## Tres relojes corren a la vez, a proposito:
##   1. Audio   — posicion del stream + tiempo desde la ultima mezcla
##                - latencia de salida. Es el correcto.
##   2. Sistema — monotono, de referencia.
##   3. Frames  — acumular delta. Es el metodo INCORRECTO, y esta aqui
##                para que veas cuanto se separa.
##
## Controles:  ESPACIO golpear · R reiniciar · S guardar ya · ESC salir
##

const BPM_POR_DEFECTO := 120.0
const PISTA_POR_DEFECTO := "res://audio/clics_120bpm.wav"

const GOLPES_CALIBRACION := 32      # 16 s a 120 BPM
const SEGUNDOS_DERIVA := 45.0       # fase pasiva. Si va a derivar, se ve antes.

const VENTANA_BUENA := 0.045
const VENTANA_PERFECTA := 0.020

enum Fase { CALIBRACION, DERIVA, TERMINADO }

@onready var _reproductor: AudioStreamPlayer = $Reproductor

var _bpm := BPM_POR_DEFECTO
var _periodo := 60.0 / BPM_POR_DEFECTO

var _fase := Fase.CALIBRACION
var _t_fin_calibracion := 0.0

var _sistema_inicio_us := 0
var _reloj_frames := 0.0

var _desfases: Array[float] = []
var _muestras_deriva: Array = []
var _ultimo_registro := 0.0

var _corriendo := false
var _latencia_salida := 0.0
var _informe_guardado := ""

var _etiqueta: RichTextLabel
var _ultimo_pulso_visto := -1
var _destello := 0.0
var _destello_tap := 0.0


func _ready() -> void:
	# Interceptamos el cierre de ventana para no perder la medicion si se cierra
	# con la X en vez de con ESC.
	get_tree().set_auto_accept_quit(false)
	_construir_interfaz()
	_cargar_pista(PISTA_POR_DEFECTO)
	set_process(true)


func _notification(que: int) -> void:
	if que == NOTIFICATION_WM_CLOSE_REQUEST:
		if _fase != Fase.TERMINADO and not _desfases.is_empty():
			_terminar()
		get_tree().quit()


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
	_etiqueta.set_anchors_preset(Control.PRESET_FULL_RECT, true)
	_etiqueta.offset_left = 32
	_etiqueta.offset_top = 22
	_etiqueta.offset_right = -32
	_etiqueta.offset_bottom = -250
	_etiqueta.add_theme_font_size_override("normal_font_size", 17)
	add_child(_etiqueta)


func _cargar_pista(ruta: String) -> void:
	if not ResourceLoader.exists(ruta):
		push_error("No se encuentra la pista: %s" % ruta)
		return
	_reproductor.stream = load(ruta)
	_arrancar()


func _arrancar() -> void:
	_desfases.clear()
	_muestras_deriva.clear()
	_reloj_frames = 0.0
	_ultimo_registro = 0.0
	_ultimo_pulso_visto = -1
	_fase = Fase.CALIBRACION
	_t_fin_calibracion = 0.0
	_informe_guardado = ""

	_latencia_salida = AudioServer.get_output_latency()
	_reproductor.play()
	_sistema_inicio_us = Time.get_ticks_usec()
	_corriendo = true


## El reloj bueno. Esta suma de tres terminos es todo el secreto: la posicion
## que reporta el reproductor esta cuantizada al bloque de mezcla, asi que hay
## que sumarle lo transcurrido desde esa mezcla y restarle lo que el sonido
## tarda en salir por los altavoces.
func _posicion_audio() -> float:
	return _reproductor.get_playback_position() \
		+ AudioServer.get_time_since_last_mix() \
		- _latencia_salida


func _posicion_sistema() -> float:
	return float(Time.get_ticks_usec() - _sistema_inicio_us) / 1_000_000.0


func _process(delta: float) -> void:
	if not _corriendo:
		_actualizar_texto(_t_fin_calibracion)
		return

	_reloj_frames += delta   # el reloj malo, a proposito

	var t_audio := _posicion_audio()
	var t_sistema := _posicion_sistema()

	if t_audio - _ultimo_registro >= 1.0:
		_ultimo_registro = t_audio
		_muestras_deriva.append([
			t_audio,
			(_reloj_frames - t_audio) * 1000.0,
			(t_sistema - t_audio) * 1000.0,
		])

	# Fin de la fase pasiva: se guarda solo.
	if _fase == Fase.DERIVA and t_audio - _t_fin_calibracion >= SEGUNDOS_DERIVA:
		_terminar()

	var pulso := int(floor(t_audio / _periodo))
	if pulso != _ultimo_pulso_visto and t_audio >= 0.0:
		_ultimo_pulso_visto = pulso
		_destello = 1.0
	_destello = maxf(0.0, _destello - delta * 6.0)
	_destello_tap = maxf(0.0, _destello_tap - delta * 5.0)

	if not _reproductor.playing:
		_terminar()

	_actualizar_texto(t_audio)
	queue_redraw()


func _terminar() -> void:
	if _fase == Fase.TERMINADO:
		return
	_fase = Fase.TERMINADO
	_corriendo = false
	_reproductor.stop()
	_guardar_informe()


func _unhandled_input(evento: InputEvent) -> void:
	if evento.is_action_pressed("tap"):
		_registrar_pulsacion()
	elif evento.is_action_pressed("reiniciar"):
		_arrancar()
	elif evento.is_action_pressed("guardar"):
		_guardar_informe()
	elif evento is InputEventKey and evento.pressed and evento.keycode == KEY_ESCAPE:
		if _fase != Fase.TERMINADO:
			_terminar()
		else:
			get_tree().quit()


func _registrar_pulsacion() -> void:
	if not _corriendo or _fase != Fase.CALIBRACION:
		return
	var t := _posicion_audio()
	# Distancia con signo al pulso mas cercano. Negativo = te adelantaste.
	_desfases.append(t - round(t / _periodo) * _periodo)
	_destello_tap = 1.0

	if _desfases.size() >= GOLPES_CALIBRACION:
		_fase = Fase.DERIVA
		_t_fin_calibracion = t


# ---------------------------------------------------------------- estadistica

func _media(v: Array) -> float:
	if v.is_empty(): return 0.0
	var s := 0.0
	for x in v: s += x
	return s / v.size()


func _desviacion(v: Array) -> float:
	if v.size() < 2: return 0.0
	var m := _media(v)
	var s := 0.0
	for x in v: s += (x - m) * (x - m)
	return sqrt(s / (v.size() - 1))


func _dentro(v: Array, ventana: float) -> int:
	var n := 0
	for x in v:
		if absf(x) <= ventana: n += 1
	return n


func _deriva_actual() -> Array:
	if _muestras_deriva.is_empty(): return [0.0, 0.0]
	var u = _muestras_deriva[-1]
	return [u[1], u[2]]


# --------------------------------------------------------------- presentacion

## Medidor de nivel de la salida. Sirve para distinguir "no suena porque el
## juego no reproduce" de "no suena porque el dispositivo de salida es otro".
func _medidor_nivel() -> String:
	var db := AudioServer.get_bus_peak_volume_left_db(0, 0)
	if db < -60.0 or is_inf(db) or is_nan(db):
		return "[color=#555566]" + "░".repeat(24) + "[/color]  silencio"
	var f := clampf((db + 60.0) / 60.0, 0.0, 1.0)
	var lleno := int(f * 24)
	return "[color=#88ff88]" + "█".repeat(lleno) + "[/color]" \
		+ "[color=#333344]" + "░".repeat(24 - lleno) + "[/color]  %.0f dB" % db


func _barra(fraccion: float, ancho: int = 34) -> String:
	var lleno := int(clampf(fraccion, 0.0, 1.0) * ancho)
	return "[color=#66aaff]" + "█".repeat(lleno) + "[/color]" \
		+ "[color=#333344]" + "░".repeat(ancho - lleno) + "[/color]"


func _actualizar_texto(t_audio: float) -> void:
	var n := _desfases.size()
	var media_ms := _media(_desfases) * 1000.0
	var jitter_ms := _desviacion(_desfases) * 1000.0
	var d := _deriva_actual()

	var L := []
	L.append("[b]PRUEBA DEL RELOJ DE AUDIO — E.N.M.A.[/b]")
	L.append("")

	match _fase:
		Fase.CALIBRACION:
			L.append("[b][color=#ffcc66]FASE 1 de 2 — CALIBRACION[/color][/b]   ·   unos 16 segundos")
			L.append("")
			L.append("  [b]Pulsa ESPACIO a cada clic.[/b]")
			L.append("")
			L.append("  %s  [b]%d[/b] de %d" % [
				_barra(float(n) / GOLPES_CALIBRACION), n, GOLPES_CALIBRACION])
			L.append("")
			L.append("  [color=#888899]Termina sola al llegar a %d. No hace falta clavarlo:" % GOLPES_CALIBRACION)
			L.append("  lo que se mide es tu ritmo natural.[/color]")

		Fase.DERIVA:
			var restan: float = SEGUNDOS_DERIVA - (t_audio - _t_fin_calibracion)
			L.append("[b][color=#88dd88]FASE 2 de 2 — DERIVA[/color][/b]   ·   [b]no tienes que hacer nada[/b]")
			L.append("")
			L.append("  %s  quedan [b]%d s[/b]" % [
				_barra(1.0 - restan / SEGUNDOS_DERIVA), int(ceil(restan))])
			L.append("")
			L.append("  [color=#888899]Dejalo sonar. Puedes minimizar la ventana.")
			L.append("  El informe se guarda solo al terminar.[/color]")

		Fase.TERMINADO:
			L.append("[b][color=#88ff88]TERMINADO[/color][/b]")
			L.append("")
			if _informe_guardado != "":
				L.append("  Informe guardado en:")
				L.append("  [color=#66aaff]%s[/color]" % _informe_guardado)
			L.append("")
			L.append("  [color=#888899]R para repetir  ·  ESC para salir[/color]")

	L.append("")
	L.append("[b]SALIDA DE AUDIO[/b]")
	var pos := _reproductor.get_playback_position()
	var sonando := _reproductor.playing
	L.append("  Reproduciendo ................ %s" % (
		"[color=#88ff88]SI[/color]" if sonando else "[color=#ff7766]NO[/color]"))
	L.append("  Posicion en la pista ......... %7.2f s" % pos)
	L.append("  Nivel ........................ %s" % _medidor_nivel())
	L.append("")
	L.append("[b]HARDWARE[/b]")
	L.append("  Latencia de salida ........... %7.2f ms" % (_latencia_salida * 1000.0))
	L.append("  Frecuencia de mezcla ......... %7.0f Hz" % AudioServer.get_mix_rate())
	L.append("  Fotogramas ................... %7.0f fps  (un frame = %.1f ms)" % [
		Engine.get_frames_per_second(), 1000.0 / maxf(1.0, Engine.get_frames_per_second())])
	L.append("")

	L.append("[b]CALIBRACION[/b]   (%d golpes)" % n)
	if n < 4:
		L.append("  [color=#888899]faltan datos[/color]")
	else:
		L.append("  Desfase medio ................ %+7.2f ms   (vas %s)" % [
			media_ms, "tarde" if media_ms > 0 else "pronto"])
		L.append("  Jitter ....................... %7.2f ms" % jitter_ms)
		L.append("  Dentro de %d ms .............. %3d de %d" % [
			int(VENTANA_PERFECTA * 1000), _dentro(_desfases, VENTANA_PERFECTA), n])
		L.append("  Dentro de %d ms .............. %3d de %d" % [
			int(VENTANA_BUENA * 1000), _dentro(_desfases, VENTANA_BUENA), n])
		L.append("  [color=#88cc88]Offset sugerido .............. %+7.1f ms[/color]" % -media_ms)
	L.append("")

	L.append("[b]DERIVA[/b]   (%d s de audio)" % int(t_audio))
	L.append("  [color=#88ddff]Sistema vs audio ............. %+7.2f ms   <- la que decide[/color]" % d[1])
	L.append("  [color=#ff9966]FRAMES vs audio .............. %+7.2f ms   <- el metodo malo[/color]" % d[0])
	L.append("")
	L.append("[color=#666677]  ESPACIO golpear  ·  R repetir  ·  S guardar ya  ·  ESC salir[/color]")

	_etiqueta.text = "\n".join(L)


func _draw() -> void:
	var w := size.x
	var h := size.y
	var base_y := h - 148.0
	var cx := w * 0.5

	var r := 34.0 + _destello * 26.0
	var col := Color(0.45, 0.75, 1.0).lerp(Color(1, 1, 1), _destello)
	if _fase == Fase.DERIVA:
		col = Color(0.45, 0.85, 0.55).lerp(Color(1, 1, 1), _destello)
	elif _fase == Fase.TERMINADO:
		col = Color(0.35, 0.4, 0.45)
	draw_circle(Vector2(cx, base_y), r, col)

	if _destello_tap > 0.0:
		draw_arc(Vector2(cx, base_y), r + 22.0, 0.0, TAU, 48,
			Color(0.55, 1.0, 0.55, _destello_tap), 3.0)

	var y := h - 56.0
	var escala := 1400.0
	draw_rect(Rect2(cx - VENTANA_BUENA * escala, y - 20,
		VENTANA_BUENA * 2 * escala, 40), Color(0.4, 0.8, 0.4, 0.10))
	draw_rect(Rect2(cx - VENTANA_PERFECTA * escala, y - 20,
		VENTANA_PERFECTA * 2 * escala, 40), Color(0.4, 0.9, 0.5, 0.18))
	draw_line(Vector2(cx, y - 26), Vector2(cx, y + 26), Color(1, 1, 1, 0.55), 2.0)

	var desde: int = maxi(0, _desfases.size() - 120)
	for i in range(desde, _desfases.size()):
		var dd: float = _desfases[i]
		var alfa: float = 0.25 + 0.75 * float(i - desde) / maxf(1.0, float(_desfases.size() - desde))
		draw_circle(Vector2(cx + dd * escala, y), 3.5, Color(0.6, 0.9, 1.0, alfa))


# -------------------------------------------------------------------- informe

func _guardar_informe() -> void:
	var n := _desfases.size()
	var L := []
	L.append("INFORME DEL RELOJ DE AUDIO — E.N.M.A.")
	L.append("Fecha : %s" % Time.get_datetime_string_from_system())
	L.append("Motor : Godot %s" % Engine.get_version_info().get("string", "?"))
	L.append("SO    : %s" % OS.get_name())
	L.append("CPU   : %s" % OS.get_processor_name())
	L.append("")
	L.append("BPM de la pista        : %.2f" % _bpm)
	L.append("Latencia de salida     : %.3f ms" % (_latencia_salida * 1000.0))
	L.append("Frecuencia de mezcla   : %.0f Hz" % AudioServer.get_mix_rate())
	L.append("FPS                    : %.0f" % Engine.get_frames_per_second())
	L.append("")
	L.append("Pulsaciones            : %d" % n)
	if n > 0:
		L.append("Desfase medio          : %+.3f ms" % (_media(_desfases) * 1000.0))
		L.append("Jitter (desv. tipica)  : %.3f ms" % (_desviacion(_desfases) * 1000.0))
		L.append("Dentro de %d ms        : %d (%.1f%%)" % [
			int(VENTANA_PERFECTA * 1000), _dentro(_desfases, VENTANA_PERFECTA),
			100.0 * _dentro(_desfases, VENTANA_PERFECTA) / n])
		L.append("Dentro de %d ms        : %d (%.1f%%)" % [
			int(VENTANA_BUENA * 1000), _dentro(_desfases, VENTANA_BUENA),
			100.0 * _dentro(_desfases, VENTANA_BUENA) / n])
		L.append("OFFSET SUGERIDO        : %+.2f ms" % (-_media(_desfases) * 1000.0))
	L.append("")
	L.append("DERIVA  (segundo, frames_vs_audio_ms, sistema_vs_audio_ms)")
	for m in _muestras_deriva:
		L.append("  %6.1f  %+9.3f  %+9.3f" % [m[0], m[1], m[2]])
	if _muestras_deriva.size() >= 2:
		var pri = _muestras_deriva[0]
		var ult = _muestras_deriva[-1]
		L.append("")
		L.append("VEREDICTO")
		L.append("  Deriva sistema-audio en %.0f s : %+.3f ms" % [
			ult[0] - pri[0], ult[2] - pri[2]])
		L.append("  Deriva frames-audio  en %.0f s : %+.3f ms" % [
			ult[0] - pri[0], ult[1] - pri[1]])
	L.append("")
	L.append("DESFASES INDIVIDUALES (ms)")
	var fila := []
	for x in _desfases:
		fila.append("%+.1f" % (x * 1000.0))
		if fila.size() == 12:
			L.append("  " + "  ".join(fila)); fila.clear()
	if not fila.is_empty():
		L.append("  " + "  ".join(fila))

	var nombre := "informe_reloj_%s.txt" % Time.get_datetime_string_from_system().replace(":", "-")
	var f := FileAccess.open("user://" + nombre, FileAccess.WRITE)
	if f:
		f.store_string("\n".join(L))
		f.close()
		_informe_guardado = ProjectSettings.globalize_path("user://" + nombre)
		print("INFORME GUARDADO: ", _informe_guardado)
		print("")
		for linea in L:
			print(linea)
	else:
		push_error("No se pudo escribir el informe")
