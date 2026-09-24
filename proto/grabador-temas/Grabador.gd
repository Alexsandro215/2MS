extends Control
##
## GRABADOR DE TEMAS — E.N.M.A.
##
## Marca a mano lo unico de una cancion que no se puede calcular: la MELODIA.
## El pulso y el compas salen solos del BPM.
##
##   FASE 1 · REJILLA   BPM + compas + donde cae el primer tiempo
##   FASE 2 · MELODIA   suena la cancion y pulsas cada nota
##   FASE 3 · GUARDAR   JSON que el nucleo ya sabe leer
##
## Usa el mismo reloj de audio del prototipo anterior, y aplica la calibracion
## medida alli: las marcas se guardan donde OISTE la nota, no donde pulsaste.
##

enum Fase { REJILLA, MELODIA }

const CARPETA_AUDIO := "res://audio"
const CARPETA_TEMAS := "res://temas"

# Offset de calibracion en segundos. El medido en la maquina principal fue
# de +27.6 ms de retraso humano, asi que se resta.
var _offset_calibracion := -0.0276

@onready var _repro: AudioStreamPlayer = $Reproductor
@onready var _metronomo: AudioStreamPlayer = $Metronomo

var _fase := Fase.REJILLA
var _etiqueta: RichTextLabel

# --- datos del tema ---
var _id := ""
var _bpm := 120.0
var _tiempos_por_compas := 4
var _offset_rejilla := 0.0          # donde cae el primer tiempo fuerte
var _melodia: Array[float] = []
var _secciones: Array = []          # [{nombre, inicio, mult}]

# --- estado ---
var _archivos: Array[String] = []
var _indice_archivo := 0
var _latencia := 0.0
var _metronomo_activo := true
var _ultimo_tiempo_sonado := -1
var _destello_pulso := 0.0
var _destello_marca := 0.0
var _mensaje := ""
var _mensaje_ttl := 0.0
var _taps_bpm: Array[float] = []


func _ready() -> void:
	get_tree().set_auto_accept_quit(false)
	_construir_interfaz()
	_generar_clic()
	_buscar_archivos()
	set_process(true)


func _notification(que: int) -> void:
	if que == NOTIFICATION_WM_CLOSE_REQUEST:
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
	_etiqueta.offset_left = 28
	_etiqueta.offset_top = 18
	_etiqueta.offset_right = -28
	_etiqueta.offset_bottom = -210
	_etiqueta.add_theme_font_size_override("normal_font_size", 16)
	add_child(_etiqueta)


## Clic sintetico para el metronomo. Evita depender de un archivo externo.
func _generar_clic() -> void:
	var sr := 44100
	var dur := 0.018
	var n := int(sr * dur)
	var datos := PackedByteArray()
	datos.resize(n * 2)
	for i in n:
		var t := float(i) / sr
		var env: float = exp(-t * 320.0)
		var v: float = sin(TAU * 1600.0 * t) * env * 0.7
		var s := int(clampf(v, -1.0, 1.0) * 32767)
		datos.encode_s16(i * 2, s)

	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = sr
	wav.stereo = false
	wav.data = datos
	_metronomo.stream = wav


func _buscar_archivos() -> void:
	_archivos.clear()
	var d := DirAccess.open(CARPETA_AUDIO)
	if d == null:
		_avisar("No existe la carpeta audio/")
		return
	d.list_dir_begin()
	var f := d.get_next()
	while f != "":
		if not d.current_is_dir():
			var ext := f.get_extension().to_lower()
			if ext in ["mp3", "ogg", "wav"]:
				_archivos.append(f)
		f = d.get_next()
	d.list_dir_end()
	_archivos.sort()

	if _archivos.is_empty():
		_avisar("Deja una cancion en proto/grabador-temas/audio/")
	else:
		_cargar(_indice_archivo)


func _cargar(i: int) -> void:
	if _archivos.is_empty():
		return
	_indice_archivo = wrapi(i, 0, _archivos.size())
	var nombre := _archivos[_indice_archivo]
	var ruta := CARPETA_AUDIO + "/" + nombre

	var stream: AudioStream = null
	match nombre.get_extension().to_lower():
		"ogg":
			stream = AudioStreamOggVorbis.load_from_file(ruta)
		"mp3":
			var mp3 := AudioStreamMP3.new()
			mp3.data = FileAccess.get_file_as_bytes(ruta)
			stream = mp3
		"wav":
			stream = load(ruta)

	if stream == null:
		_avisar("No se pudo cargar %s" % nombre)
		return

	_repro.stream = stream
	_id = nombre.get_basename()
	_melodia.clear()
	_fase = Fase.REJILLA
	_latencia = AudioServer.get_output_latency()
	_avisar("Cargado: %s" % nombre)

	# Si ya hay un tema guardado para esta cancion, se recupera.
	_cargar_tema_si_existe()


# ---------------------------------------------------------------- el reloj

## La misma receta del prototipo del reloj de audio.
func _posicion() -> float:
	if not _repro.playing:
		return 0.0
	return _repro.get_playback_position() \
		+ AudioServer.get_time_since_last_mix() \
		- _latencia


func _tiempo_en(t: float) -> float:
	return 60.0 / _bpm


func _indice_pulso(t: float) -> int:
	return int(floor((t - _offset_rejilla) / (60.0 / _bpm)))


func _es_inicio_compas(indice: int) -> bool:
	return posmod(indice, _tiempos_por_compas) == 0


# ---------------------------------------------------------------- proceso

func _process(delta: float) -> void:
	_destello_pulso = maxf(0.0, _destello_pulso - delta * 7.0)
	_destello_marca = maxf(0.0, _destello_marca - delta * 5.0)
	_mensaje_ttl = maxf(0.0, _mensaje_ttl - delta)

	if _repro.playing and _metronomo_activo:
		var t := _posicion()
		var idx := _indice_pulso(t)
		if idx != _ultimo_tiempo_sonado and idx >= 0:
			_ultimo_tiempo_sonado = idx
			_destello_pulso = 1.0
			_metronomo.volume_db = 0.0 if _es_inicio_compas(idx) else -9.0
			_metronomo.play()

	_actualizar_texto()
	queue_redraw()


# ---------------------------------------------------------------- entrada

func _unhandled_input(e: InputEvent) -> void:
	if not (e is InputEventKey and e.pressed and not e.echo):
		return
	var k: int = e.keycode

	# --- comunes ---
	match k:
		KEY_SPACE:
			if _fase == Fase.MELODIA: _marcar_melodia()
			else: _marcar_primer_tiempo()
			return
		KEY_ENTER, KEY_KP_ENTER:
			_cambiar_fase(); return
		KEY_P:
			_reproducir_o_parar(); return
		KEY_M:
			_metronomo_activo = not _metronomo_activo
			_avisar("Metronomo " + ("ON" if _metronomo_activo else "OFF")); return
		KEY_S:
			_guardar(); return
		KEY_BRACKETLEFT:
			_cargar(_indice_archivo - 1); return
		KEY_BRACKETRIGHT:
			_cargar(_indice_archivo + 1); return
		KEY_ESCAPE:
			get_tree().quit(); return

	if _fase == Fase.REJILLA:
		match k:
			KEY_UP: _bpm += 1.0; _avisar("BPM %.1f" % _bpm)
			KEY_DOWN: _bpm = maxf(20.0, _bpm - 1.0); _avisar("BPM %.1f" % _bpm)
			KEY_RIGHT: _bpm += 0.1; _avisar("BPM %.1f" % _bpm)
			KEY_LEFT: _bpm = maxf(20.0, _bpm - 0.1); _avisar("BPM %.1f" % _bpm)
			KEY_C:
				var ciclo: Array[int] = [3, 4, 6, 5, 7]
				var i: int = ciclo.find(_tiempos_por_compas)
				_tiempos_por_compas = ciclo[(i + 1) % ciclo.size()]
				_avisar("Compás %d/4" % _tiempos_por_compas)
			KEY_T: _tap_bpm()
	else:
		match k:
			KEY_BACKSPACE:
				if not _melodia.is_empty():
					_melodia.remove_at(_melodia.size() - 1)
					_avisar("Marca borrada (%d)" % _melodia.size())
			KEY_DELETE:
				_melodia.clear(); _avisar("Melodía vaciada")


func _reproducir_o_parar() -> void:
	if _repro.playing:
		_repro.stop()
	else:
		_latencia = AudioServer.get_output_latency()
		_ultimo_tiempo_sonado = -1
		_repro.play()


func _cambiar_fase() -> void:
	_fase = Fase.MELODIA if _fase == Fase.REJILLA else Fase.REJILLA
	_avisar("FASE: " + ("MELODÍA" if _fase == Fase.MELODIA else "REJILLA"))


## En fase rejilla, ESPACIO fija donde cae el primer tiempo fuerte.
func _marcar_primer_tiempo() -> void:
	if not _repro.playing:
		_avisar("Dale a P para reproducir primero")
		return
	var t := _posicion() + _offset_calibracion
	# Se guarda el resto, para que la rejilla arranque en el compas correcto.
	_offset_rejilla = fposmod(t, 60.0 / _bpm * _tiempos_por_compas)
	_ultimo_tiempo_sonado = -1
	_avisar("Primer tiempo en %.3f s" % _offset_rejilla)


## Golpea T varias veces al ritmo y deduce el BPM.
func _tap_bpm() -> void:
	var ahora := float(Time.get_ticks_usec()) / 1_000_000.0
	if not _taps_bpm.is_empty() and ahora - _taps_bpm[-1] > 2.0:
		_taps_bpm.clear()
	_taps_bpm.append(ahora)
	if _taps_bpm.size() >= 4:
		var total: float = _taps_bpm[-1] - _taps_bpm[0]
		var n: int = _taps_bpm.size() - 1
		_bpm = 60.0 / (total / n)
		_avisar("BPM deducido: %.1f  (%d golpes)" % [_bpm, _taps_bpm.size()])
	else:
		_avisar("Sigue golpeando T… (%d)" % _taps_bpm.size())


func _marcar_melodia() -> void:
	if not _repro.playing:
		_avisar("Dale a P para reproducir primero")
		return
	var t := _posicion() + _offset_calibracion
	_melodia.append(t)
	_melodia.sort()
	_destello_marca = 1.0


func _avisar(txt: String) -> void:
	_mensaje = txt
	_mensaje_ttl = 2.5


# ---------------------------------------------------------------- guardado

func _ruta_tema() -> String:
	return CARPETA_TEMAS + "/" + _id + ".json"


func _guardar() -> void:
	if _id == "":
		_avisar("No hay canción cargada")
		return

	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CARPETA_TEMAS))

	var datos := {
		"id": _id,
		"archivo": _archivos[_indice_archivo] if not _archivos.is_empty() else "",
		"bpm": snappedf(_bpm, 0.01),
		"tiemposPorCompas": _tiempos_por_compas,
		"offset": snappedf(_offset_rejilla, 0.0001),
		"secciones": _secciones,
		"melodia": _melodia.map(func(x): return snappedf(x, 0.0001)),
	}

	var f := FileAccess.open(_ruta_tema(), FileAccess.WRITE)
	if f == null:
		_avisar("No se pudo escribir el tema")
		return
	f.store_string(JSON.stringify(datos, "  "))
	f.close()
	_avisar("Guardado: %s  (%d notas)" % [ProjectSettings.globalize_path(_ruta_tema()), _melodia.size()])
	print("GUARDADO ", ProjectSettings.globalize_path(_ruta_tema()))


func _cargar_tema_si_existe() -> void:
	if not FileAccess.file_exists(_ruta_tema()):
		return
	var txt := FileAccess.get_file_as_string(_ruta_tema())
	var datos = JSON.parse_string(txt)
	if typeof(datos) != TYPE_DICTIONARY:
		return

	_bpm = float(datos.get("bpm", _bpm))
	_tiempos_por_compas = int(datos.get("tiemposPorCompas", _tiempos_por_compas))
	_offset_rejilla = float(datos.get("offset", 0.0))
	_secciones = datos.get("secciones", [])
	_melodia.clear()
	for x in datos.get("melodia", []):
		_melodia.append(float(x))
	_avisar("Tema recuperado: %d notas" % _melodia.size())


# ---------------------------------------------------------------- pintado

func _actualizar_texto() -> void:
	var L := []
	L.append("[b]GRABADOR DE TEMAS — E.N.M.A.[/b]")
	L.append("")

	if _archivos.is_empty():
		L.append("[color=#ff8866]No hay canciones.[/color]")
		L.append("")
		L.append("  Deja un .mp3, .ogg o .wav en:")
		L.append("  [color=#66aaff]proto/grabador-temas/audio/[/color]")
		L.append("  y reinicia.")
		_etiqueta.text = "\n".join(L)
		return

	var t := _posicion()
	L.append("  Canción .... [b]%s[/b]   (%d de %d)   [color=#666677][ ] para cambiar[/color]" % [
		_archivos[_indice_archivo], _indice_archivo + 1, _archivos.size()])
	L.append("  Posición ... %7.3f s   %s" % [
		t, "[color=#88ff88]▶ sonando[/color]" if _repro.playing else "[color=#777788]■ parado[/color]"])
	L.append("")

	if _fase == Fase.REJILLA:
		L.append("[b][color=#ffcc66]FASE 1 · REJILLA[/color][/b]  —  esto se CALCULA, no se graba")
		L.append("")
		L.append("  BPM ................ [b]%.1f[/b]      [color=#666677]↑↓ ±1   ←→ ±0.1   T golpear al ritmo[/color]" % _bpm)
		L.append("  Compás ............. [b]%d/4[/b]        [color=#666677]C para cambiar[/color]" % _tiempos_por_compas)
		L.append("  Primer tiempo en ... [b]%.3f s[/b]   [color=#666677]ESPACIO sobre el golpe fuerte[/color]" % _offset_rejilla)
		L.append("")
		L.append("  Un tiempo dura %.4f s · un compás %.4f s" % [
			60.0 / _bpm, 60.0 / _bpm * _tiempos_por_compas])
		L.append("")
		L.append("  [color=#888899]Ajusta hasta que el metrónomo encaje con la canción.")
		L.append("  Cuando encaje, ENTER para pasar a grabar la melodía.[/color]")
	else:
		L.append("[b][color=#88dd88]FASE 2 · MELODÍA[/color][/b]  —  esto hay que GRABARLO")
		L.append("")
		L.append("  Notas marcadas ..... [b]%d[/b]" % _melodia.size())
		if not _melodia.is_empty():
			L.append("  Última ............. %.3f s" % _melodia[-1])
		L.append("")
		L.append("  [b]Pulsa ESPACIO en cada nota de la melodía.[/b]")
		L.append("")
		L.append("  [color=#888899]RETROCESO borra la última · SUPR vacía todo")
		L.append("  ENTER vuelve a la rejilla · S guarda[/color]")

	L.append("")
	L.append("  Calibración aplicada: %+.1f ms   ·   latencia de salida %.1f ms" % [
		_offset_calibracion * 1000.0, _latencia * 1000.0])

	if _mensaje_ttl > 0.0:
		L.append("")
		L.append("  [color=#66aaff]%s[/color]" % _mensaje)

	L.append("")
	L.append("[color=#666677]  P sonar/parar · M metrónomo · S guardar · ENTER cambiar fase · ESC salir[/color]")

	_etiqueta.text = "\n".join(L)


func _draw() -> void:
	if _archivos.is_empty():
		return

	var w := size.x
	var h := size.y
	var y := h - 120.0
	var cx := w * 0.5

	# Circulo que late con el pulso
	var r := 26.0 + _destello_pulso * 18.0
	var idx := _indice_pulso(_posicion())
	var fuerte := _es_inicio_compas(idx)
	var col: Color = Color(1.0, 0.8, 0.4) if fuerte else Color(0.45, 0.7, 1.0)
	draw_circle(Vector2(cx, y), r, col.lerp(Color.WHITE, _destello_pulso * 0.6))

	if _destello_marca > 0.0:
		draw_arc(Vector2(cx, y), r + 20.0, 0.0, TAU, 40,
			Color(0.5, 1.0, 0.5, _destello_marca), 3.0)

	# Linea de tiempo: 4 segundos de ventana alrededor del ahora
	var ty := h - 45.0
	var ventana := 4.0
	var px_por_s := w / ventana
	var t0 := _posicion() - ventana * 0.5

	draw_line(Vector2(0, ty), Vector2(w, ty), Color(1, 1, 1, 0.12), 1.0)

	# Rejilla de pulsos y compases
	var paso := 60.0 / _bpm
	# floorf y no floor: la global es polimorfica y devuelve Variant.
	var primer := floorf((t0 - _offset_rejilla) / paso)
	for i in range(int(ventana / paso) + 3):
		var n: int = int(primer) + i
		var tt: float = _offset_rejilla + n * paso
		var x: float = (tt - t0) * px_por_s
		if x < 0 or x > w:
			continue
		if _es_inicio_compas(n):
			draw_line(Vector2(x, ty - 26), Vector2(x, ty + 26), Color(1.0, 0.8, 0.4, 0.55), 2.0)
		else:
			draw_line(Vector2(x, ty - 14), Vector2(x, ty + 14), Color(0.5, 0.7, 1.0, 0.30), 1.0)

	# Marcas de melodia
	for m in _melodia:
		var x: float = (m - t0) * px_por_s
		if x < 0 or x > w:
			continue
		draw_circle(Vector2(x, ty), 4.0, Color(0.5, 1.0, 0.6, 0.9))

	# El ahora
	draw_line(Vector2(cx, ty - 34), Vector2(cx, ty + 34), Color(1, 1, 1, 0.85), 2.0)
