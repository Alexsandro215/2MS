extends Control
##
## GRABADOR DE TEMAS — E.N.M.A.
##
## Marca lo unico de una cancion que no se puede calcular: la MELODIA.
## Y define el MAPA DE TEMPO, que tampoco se adivina cuando la cancion cambia
## de compas a mitad.
##
## POR QUE UN MAPA Y NO UN BPM
##
## La primera version tenia un solo BPM, un solo compas y un solo offset para
## toda la cancion. Con musica de verdad eso se rompe: en cuanto entra un
## compas de 2/4 dentro de un 4/4, todo lo que viene despues se desplaza medio
## compas y ya no vuelve a cuadrar. Es un recurso comun para acortar una frase.
##
## Asi que la rejilla es una LISTA DE TRAMOS, cada uno con su BPM y su compas,
## y cada tramo reinicia la cuenta de compases. Es lo que hacen los DAW.
##
## UNA SOLA PANTALLA, sin modos. Cada tecla hace siempre lo mismo.
## La version con fases costo una sesion entera de trabajo: 621 pulsaciones
## que reajustaban la rejilla en vez de marcar notas, porque ESPACIO hacia dos
## cosas distintas segun un modo que no se veia.
##

const CARPETA_AUDIO := "res://audio"
const CARPETA_TEMAS := "res://temas"

# Calibracion medida en la prueba del reloj de audio. Se resta de cada marca
# para guardarla donde OISTE la nota, no donde pulsaste.
var _offset_calibracion := -0.0276

@onready var _repro: AudioStreamPlayer = $Reproductor
@onready var _metronomo: AudioStreamPlayer = $Metronomo

var _etiqueta: RichTextLabel

# --- datos del tema ---
var _id := ""
## Mapa de tempo: [{desde, bpm, compas}, ...] ordenado por "desde".
var _tramos: Array[Dictionary] = []
var _melodia: Array[float] = []
var _secciones: Array = []

# --- estado ---
var _archivos: Array[String] = []
var _indice_archivo := 0
var _latencia := 0.0
var _metronomo_activo := true
var _ultimo_pulso_sonado := -999999
var _destello_pulso := 0.0
var _destello_marca := 0.0
var _mensaje := ""
var _mensaje_ttl := 0.0
var _taps_bpm: Array[float] = []

## Posicion del cursor, suene o no la cancion. Antes _posicion() devolvia 0
## con la cancion parada, asi que no se podia mirar nada sin reproducir.
var _cursor := 0.0

## Seleccion de rango sobre la barra. -1 = no hay.
var _sel_desde := -1.0
var _sel_hasta := -1.0
var _sel_bpm := 120.0
var _sel_compas := 4
var _arrastrando := false

## Ventana visible de la barra. Con 6 minutos a lo ancho, cada pixel son
## decimas de segundo y es imposible clavar un limite de compas. El zoom es
## lo que hace posible la precision.
var _vista_ini := 0.0
var _vista_fin := -1.0        # -1 = toda la cancion

## Ajuste a rejilla. Un cambio de compas cae SIEMPRE sobre un tiempo del
## tramo anterior, asi que pegarse a la rejilla acierta casi siempre.
var _ajustar_a_rejilla := true

# --- mezclador ---
var _vol_cancion := 0.0
var _vol_metronomo := 0.0
var _clic_fuerte: AudioStreamWAV
var _clic_debil: AudioStreamWAV

const MARGEN_BARRA := 30.0
const ALTO_BARRA := 46.0

## Un color por compas, para que un 2/4 metido en medio se vea sin leer nada.
const COLORES_COMPAS := {
	2: Color(0.95, 0.45, 0.35),
	3: Color(0.55, 0.80, 0.45),
	4: Color(0.35, 0.60, 0.95),
	5: Color(0.85, 0.65, 0.35),
	6: Color(0.70, 0.50, 0.90),
	7: Color(0.40, 0.80, 0.80),
}


func _ready() -> void:
	get_tree().set_auto_accept_quit(false)
	_construir_interfaz()
	_generar_clics()
	_buscar_archivos()
	set_process(true)


func _notification(que: int) -> void:
	if que == NOTIFICATION_WM_CLOSE_REQUEST:
		if _id != "": _guardar(true)
		get_tree().quit()


func _construir_interfaz() -> void:
	# OJO: NADA de un ColorRect de fondo como hijo.
	#
	# En Godot el _draw() de un Control se pinta ANTES que sus hijos, asi que
	# un ColorRect a pantalla completa tapa entero lo que dibuje el nodo. El
	# fondo se pinta dentro de _draw(), antes que la barra.
	mouse_filter = Control.MOUSE_FILTER_STOP

	_etiqueta = RichTextLabel.new()
	_etiqueta.bbcode_enabled = true
	_etiqueta.scroll_active = false
	_etiqueta.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_etiqueta.set_anchors_preset(Control.PRESET_FULL_RECT, true)
	_etiqueta.offset_left = 26
	_etiqueta.offset_top = 16
	_etiqueta.offset_right = -26
	_etiqueta.offset_bottom = -270
	_etiqueta.add_theme_font_size_override("normal_font_size", 15)
	add_child(_etiqueta)


## Dos clics de TIMBRE distinto, no el mismo mas bajito: con la cancion encima
## un mismo sonido a dos volumenes se confunde.
func _generar_clics() -> void:
	_clic_fuerte = _sintetizar(660.0, 0.075, 55.0, 1.0, true)     # "DONG"
	_clic_debil = _sintetizar(2400.0, 0.014, 420.0, 0.45, false)  # "tic"
	_metronomo.stream = _clic_fuerte


func _sintetizar(freq: float, dur: float, caida: float, amp: float,
		con_armonico: bool) -> AudioStreamWAV:
	var sr := 44100
	var n := int(sr * dur)
	var datos := PackedByteArray()
	datos.resize(n * 2)
	for i in n:
		var t := float(i) / sr
		var env: float = exp(-t * caida)
		var v: float = sin(TAU * freq * t)
		if con_armonico:
			v = v * 0.75 + sin(TAU * freq * 1.5 * t) * 0.25
		v *= env * amp
		datos.encode_s16(i * 2, int(clampf(v, -1.0, 1.0) * 32767))

	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = sr
	wav.stereo = false
	wav.data = datos
	return wav


# ------------------------------------------------------------------ archivos

func _buscar_archivos() -> void:
	_archivos.clear()
	var d := DirAccess.open(CARPETA_AUDIO)
	if d == null:
		_avisar("No existe la carpeta audio/")
		return
	d.list_dir_begin()
	var f := d.get_next()
	while f != "":
		if not d.current_is_dir() and f.get_extension().to_lower() in ["mp3", "ogg", "wav"]:
			_archivos.append(f)
		f = d.get_next()
	d.list_dir_end()
	_archivos.sort()

	if _archivos.is_empty():
		_avisar("Deja una canción en proto/grabador-temas/audio/")
	else:
		_cargar(_indice_archivo)


func _cargar(i: int) -> void:
	if _archivos.is_empty(): return
	_indice_archivo = wrapi(i, 0, _archivos.size())
	var nombre := _archivos[_indice_archivo]
	var ruta := CARPETA_AUDIO + "/" + nombre

	var stream: AudioStream = null
	match nombre.get_extension().to_lower():
		"ogg": stream = AudioStreamOggVorbis.load_from_file(ruta)
		"mp3":
			var mp3 := AudioStreamMP3.new()
			mp3.data = FileAccess.get_file_as_bytes(ruta)
			stream = mp3
		"wav": stream = load(ruta)

	if stream == null:
		_avisar("No se pudo cargar %s" % nombre)
		return

	_repro.stream = stream
	_id = nombre.get_basename()
	_melodia.clear()
	_tramos.clear()
	_latencia = AudioServer.get_output_latency()
	_cargar_tema_si_existe()

	# Siempre tiene que haber al menos un tramo desde el principio.
	if _tramos.is_empty():
		_tramos.append({"desde": 0.0, "bpm": 120.0, "compas": 4})

	_avisar("Cargado: %s" % nombre)


func _duracion() -> float:
	return _repro.stream.get_length() if _repro.stream != null else 0.0


## Y de la barra de tramos. La comparten el dibujo y el raton, para que
## clicar caiga exactamente donde se ve.
func _y_barra() -> float:
	return size.y - 190.0


func _ancho_barra() -> float:
	return size.x - MARGEN_BARRA * 2.0


func _vista() -> Vector2:
	var dur := _duracion()
	var fin: float = _vista_fin if _vista_fin > 0.0 else dur
	return Vector2(maxf(0.0, _vista_ini), minf(dur, fin))


## Instante de la cancion que corresponde a una x de pantalla.
func _tiempo_en_x(x: float) -> float:
	var v := _vista()
	if v.y <= v.x: return 0.0
	var f := clampf((x - MARGEN_BARRA) / _ancho_barra(), 0.0, 1.0)
	return v.x + f * (v.y - v.x)


## Y al reves.
func _x_de_tiempo(t: float) -> float:
	var v := _vista()
	if v.y <= v.x: return MARGEN_BARRA
	return MARGEN_BARRA + ((t - v.x) / (v.y - v.x)) * _ancho_barra()


## Instante del pulso de rejilla mas cercano. Es lo que convierte un arrastre
## aproximado en un limite exacto.
func _pegar_a_rejilla(t: float) -> float:
	if not _ajustar_a_rejilla or _tramos.is_empty(): return t
	var tr: Dictionary = _tramos[_tramo_en(t)]
	var paso: float = 60.0 / float(tr["bpm"])
	var desde: float = float(tr["desde"])
	return desde + roundf((t - desde) / paso) * paso


## Zoom manteniendo fijo un instante bajo el raton.
func _zoom(factor: float, centro: float) -> void:
	var dur := _duracion()
	if dur <= 0.0: return
	var v := _vista()
	var ancho: float = maxf(0.25, (v.y - v.x) * factor)
	ancho = minf(ancho, dur)
	var f: float = clampf((centro - v.x) / maxf(0.001, v.y - v.x), 0.0, 1.0)
	_vista_ini = clampf(centro - ancho * f, 0.0, dur - ancho)
	_vista_fin = _vista_ini + ancho


func _desplazar(seg: float) -> void:
	var dur := _duracion()
	var v := _vista()
	var ancho: float = v.y - v.x
	_vista_ini = clampf(_vista_ini + seg, 0.0, maxf(0.0, dur - ancho))
	_vista_fin = _vista_ini + ancho


func _ver_todo() -> void:
	_vista_ini = 0.0
	_vista_fin = -1.0


# -------------------------------------------------------------- mapa de tempo

## Indice del tramo que contiene un instante.
func _tramo_en(t: float) -> int:
	var idx := 0
	for i in _tramos.size():
		if float(_tramos[i]["desde"]) <= t: idx = i
		else: break
	return idx


func _bpm_en(t: float) -> float:
	return float(_tramos[_tramo_en(t)]["bpm"])


func _compas_en(t: float) -> int:
	return int(_tramos[_tramo_en(t)]["compas"])


## Numero de pulso dentro de SU tramo. Cada tramo reinicia la cuenta, que es
## lo que hace que un cambio de compas no desplace todo lo que viene despues.
func _pulso_local(t: float) -> int:
	var tr: Dictionary = _tramos[_tramo_en(t)]
	var dur: float = 60.0 / float(tr["bpm"])
	return int(floorf((t - float(tr["desde"])) / dur))


func _es_inicio_compas(t: float) -> bool:
	return posmod(_pulso_local(t), _compas_en(t)) == 0


## Identificador global de pulso, para no repetir el clic.
func _sello_pulso(t: float) -> int:
	return _tramo_en(t) * 1000000 + _pulso_local(t)


## Parte un tramo nuevo aqui. Hereda BPM y compas del anterior.
func _partir_aqui() -> void:
	if not _repro.playing:
		_avisar("Dale a P para reproducir primero")
		return
	var t: float = _posicion() + _offset_calibracion
	var i := _tramo_en(t)

	if absf(float(_tramos[i]["desde"]) - t) < 0.05:
		_avisar("Ya hay un tramo aquí")
		return

	_tramos.insert(i + 1, {
		"desde": t,
		"bpm": float(_tramos[i]["bpm"]),
		"compas": int(_tramos[i]["compas"]),
	})
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Tramo nuevo en %.3f s  ·  ahora hay %d" % [t, _tramos.size()])


## Mete un tramo en un instante, o actualiza el que ya empiece ahi.
func _insertar_tramo(t: float, bpm: float, compas: int) -> void:
	for i in _tramos.size():
		if absf(float(_tramos[i]["desde"]) - t) < 0.02:
			_tramos[i]["bpm"] = bpm
			_tramos[i]["compas"] = compas
			return
	var i2 := _tramo_en(t)
	_tramos.insert(i2 + 1, {"desde": t, "bpm": bpm, "compas": compas})


## Escribe la configuracion de la seleccion en el mapa de tempo.
##
## Parte en los dos extremos: el rango pasa a ser su propio tramo con los
## valores elegidos, y justo despues se restaura lo que habia. Los tramos que
## quedaban dentro se borran, porque los sustituye este.
func _aplicar_seleccion() -> void:
	if not _hay_seleccion():
		_avisar("Arrastra sobre la barra para elegir un rango primero")
		return

	var a := minf(_sel_desde, _sel_hasta)
	var b := maxf(_sel_desde, _sel_hasta)
	var bpm_despues := _bpm_en(b)
	var compas_despues := _compas_en(b)

	for i in range(_tramos.size() - 1, 0, -1):
		var d := float(_tramos[i]["desde"])
		if d > a - 0.02 and d < b + 0.02:
			_tramos.remove_at(i)

	_insertar_tramo(a, _sel_bpm, _sel_compas)
	if b < _duracion() - 0.05:
		_insertar_tramo(b, bpm_despues, compas_despues)

	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Aplicado: %s → %s · %d/4 · %.1f BPM" % [
		_reloj(a), _reloj(b), _sel_compas, _sel_bpm])
	_limpiar_seleccion()


## Quita el tramo bajo el cursor. El primero no se puede quitar.
func _quitar_tramo() -> void:
	var i := _tramo_en(_posicion())
	if i == 0:
		_avisar("El primer tramo no se puede quitar")
		return
	_tramos.remove_at(i)
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Tramo quitado · quedan %d" % _tramos.size())


# ---------------------------------------------------------------- el reloj

## La receta del prototipo del reloj de audio, ya medida y validada.
func _posicion() -> float:
	if _repro.playing:
		_cursor = _repro.get_playback_position() \
			+ AudioServer.get_time_since_last_mix() \
			- _latencia
	return _cursor


# ---------------------------------------------------------------- proceso

func _process(delta: float) -> void:
	_destello_pulso = maxf(0.0, _destello_pulso - delta * 7.0)
	_destello_marca = maxf(0.0, _destello_marca - delta * 5.0)
	_mensaje_ttl = maxf(0.0, _mensaje_ttl - delta)

	if _repro.playing and _metronomo_activo and not _tramos.is_empty():
		var t := _posicion()
		var sello := _sello_pulso(t)
		if sello != _ultimo_pulso_sonado:
			_ultimo_pulso_sonado = sello
			var fuerte := _es_inicio_compas(t)
			_destello_pulso = 1.0 if fuerte else 0.55
			_metronomo.stream = _clic_fuerte if fuerte else _clic_debil
			_metronomo.volume_db = _vol_metronomo + (0.0 if fuerte else -6.0)
			_metronomo.play()

	_actualizar_texto()
	queue_redraw()


# ---------------------------------------------------------------- entrada

func _gui_input(e: InputEvent) -> void:
	# Clicar en la barra salta a ese punto. Imprescindible para configurar una
	# cancion entera sin escucharla del tiron.
	if e is InputEventMouseButton:
		var b := e as InputEventMouseButton
		if not _sobre_la_barra(b.position.y): return

		if b.button_index == MOUSE_BUTTON_WHEEL_UP and b.pressed:
			_zoom(0.7, _tiempo_en_x(b.position.x)); return
		elif b.button_index == MOUSE_BUTTON_WHEEL_DOWN and b.pressed:
			_zoom(1.0 / 0.7, _tiempo_en_x(b.position.x)); return

		if b.button_index == MOUSE_BUTTON_LEFT:
			if b.pressed:
				_arrastrando = true
				_sel_desde = _pegar_a_rejilla(_tiempo_en_x(b.position.x))
				_sel_hasta = _sel_desde
				_sel_hasta = _sel_desde
				_saltar_a(_sel_desde)
				# La seleccion empieza con lo que ya hay ahi
				_sel_bpm = _bpm_en(_sel_desde)
				_sel_compas = _compas_en(_sel_desde)
			else:
				_arrastrando = false
				if absf(_sel_hasta - _sel_desde) < 0.25:
					_limpiar_seleccion()      # fue un clic, no un arrastre
				else:
					_avisar("Rango %s → %s · ajusta y pulsa ENTER" % [
						_reloj(minf(_sel_desde, _sel_hasta)),
						_reloj(maxf(_sel_desde, _sel_hasta))])

		elif b.button_index == MOUSE_BUTTON_RIGHT and b.pressed:
			_limpiar_seleccion()
			_avisar("Selección quitada")

	elif e is InputEventMouseMotion and _arrastrando:
		var m := e as InputEventMouseMotion
		_sel_hasta = _pegar_a_rejilla(_tiempo_en_x(m.position.x))
		_saltar_a(_sel_hasta)


## Mueve el borde mas cercano al cursor un tiempo de rejilla.
func _empujar_borde(dir: int) -> void:
	if not _hay_seleccion():
		_avisar("No hay selección que empujar")
		return
	var paso: float = 60.0 / _bpm_en(_sel_desde)
	var c := _posicion()
	if absf(_sel_desde - c) <= absf(_sel_hasta - c):
		_sel_desde += paso * dir
		_avisar("Inicio → %s" % _reloj(_sel_desde))
	else:
		_sel_hasta += paso * dir
		_avisar("Final → %s" % _reloj(_sel_hasta))


func _limpiar_seleccion() -> void:
	_sel_desde = -1.0
	_sel_hasta = -1.0
	_arrastrando = false


func _hay_seleccion() -> bool:
	return _sel_desde >= 0.0 and absf(_sel_hasta - _sel_desde) >= 0.25


func _sobre_la_barra(y: float) -> bool:
	return y >= _y_barra() - 20.0 and y <= _y_barra() + ALTO_BARRA + 26.0


## Mueve el cursor. NO arranca la cancion: solo suena si tu lo pides con P.
func _saltar_a(t: float) -> void:
	if _repro.stream == null: return
	_cursor = clampf(t, 0.0, _duracion())
	if _repro.playing:
		_repro.seek(_cursor)
	_ultimo_pulso_sonado = -999999


func _unhandled_input(e: InputEvent) -> void:
	if not (e is InputEventKey and e.pressed and not e.echo): return

	match e.keycode:
		# --- lo que se graba ---
		KEY_SPACE: _marcar_melodia()
		KEY_BACKSPACE: _borrar_ultima()
		KEY_DELETE: _vaciar_melodia()

		# --- precision ---
		KEY_N:
			_ajustar_a_rejilla = not _ajustar_a_rejilla
			_avisar("Ajuste a rejilla " + ("ON" if _ajustar_a_rejilla else "OFF"))
		KEY_COMMA: _empujar_borde(-1)
		KEY_PERIOD: _empujar_borde(1)
		KEY_Z: _zoom(0.7, _posicion())
		KEY_A: _zoom(1.0 / 0.7, _posicion())
		KEY_V: _ver_todo()

		# --- mapa de tempo ---
		KEY_ENTER, KEY_KP_ENTER: _aplicar_seleccion()
		KEY_G: _partir_aqui()
		KEY_X: _quitar_tramo()
		KEY_T: _tap_bpm()
		KEY_UP: _ajustar_bpm(1.0)
		KEY_DOWN: _ajustar_bpm(-1.0)
		KEY_RIGHT: _ajustar_bpm(0.1)
		KEY_LEFT: _ajustar_bpm(-0.1)
		KEY_C: _siguiente_compas()

		# --- mezclador ---
		KEY_1: _ajustar_volumen(true, -2.0)
		KEY_2: _ajustar_volumen(true, 2.0)
		KEY_3: _ajustar_volumen(false, -2.0)
		KEY_4: _ajustar_volumen(false, 2.0)

		# --- transporte ---
		KEY_P: _reproducir_o_parar()
		KEY_M:
			_metronomo_activo = not _metronomo_activo
			_avisar("Metrónomo " + ("ON" if _metronomo_activo else "OFF"))
		KEY_S: _guardar()
		KEY_BRACKETLEFT:
			if _id != "": _guardar(true)
			_cargar(_indice_archivo - 1)
		KEY_BRACKETRIGHT:
			if _id != "": _guardar(true)
			_cargar(_indice_archivo + 1)
		KEY_ESCAPE:
			if _id != "": _guardar(true)
			get_tree().quit()


## Las flechas y C editan el TRAMO BAJO EL CURSOR, no la cancion entera.
func _ajustar_bpm(d: float) -> void:
	if _hay_seleccion():
		_sel_bpm = maxf(20.0, _sel_bpm + d)
		_avisar("Selección · BPM %.1f  (ENTER para aplicar)" % _sel_bpm)
		return
	if _tramos.is_empty(): return
	var i := _tramo_en(_posicion())
	_tramos[i]["bpm"] = maxf(20.0, float(_tramos[i]["bpm"]) + d)
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Tramo %d · BPM %.1f" % [i + 1, float(_tramos[i]["bpm"])])


func _siguiente_compas() -> void:
	var ciclo0: Array[int] = [4, 2, 3, 5, 6, 7]
	if _hay_seleccion():
		var j0: int = ciclo0.find(_sel_compas)
		_sel_compas = ciclo0[(j0 + 1) % ciclo0.size()]
		_avisar("Selección · compás %d/4  (ENTER para aplicar)" % _sel_compas)
		return
	if _tramos.is_empty(): return
	var ciclo: Array[int] = ciclo0
	var i := _tramo_en(_posicion())
	var j: int = ciclo.find(int(_tramos[i]["compas"]))
	_tramos[i]["compas"] = ciclo[(j + 1) % ciclo.size()]
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Tramo %d · compás %d/4" % [i + 1, int(_tramos[i]["compas"])])


func _tap_bpm() -> void:
	var ahora := float(Time.get_ticks_usec()) / 1000000.0
	if not _taps_bpm.is_empty() and ahora - _taps_bpm[-1] > 2.0:
		_taps_bpm.clear()
	_taps_bpm.append(ahora)
	if _taps_bpm.size() >= 4:
		var total: float = _taps_bpm[-1] - _taps_bpm[0]
		var n: int = _taps_bpm.size() - 1
		if _hay_seleccion():
			_sel_bpm = 60.0 / (total / n)
			_avisar("Selección · BPM deducido %.1f  (ENTER para aplicar)" % _sel_bpm)
			return
		var i := _tramo_en(_posicion())
		_tramos[i]["bpm"] = 60.0 / (total / n)
		_ultimo_pulso_sonado = -999999
		_guardar(true)
		_avisar("Tramo %d · BPM deducido %.1f  (%d golpes)" % [
			i + 1, float(_tramos[i]["bpm"]), _taps_bpm.size()])
	else:
		_avisar("Sigue golpeando T… (%d)" % _taps_bpm.size())


func _ajustar_volumen(es_cancion: bool, d: float) -> void:
	if es_cancion:
		_vol_cancion = clampf(_vol_cancion + d, -40.0, 6.0)
		_repro.volume_db = _vol_cancion
		_avisar("Canción %+.0f dB" % _vol_cancion)
	else:
		_vol_metronomo = clampf(_vol_metronomo + d, -40.0, 6.0)
		_avisar("Metrónomo %+.0f dB" % _vol_metronomo)


func _reproducir_o_parar() -> void:
	if _repro.playing:
		_repro.stop()
	else:
		_latencia = AudioServer.get_output_latency()
		_ultimo_pulso_sonado = -999999
		_repro.volume_db = _vol_cancion
		_repro.play(_cursor)     # arranca donde este el cursor, no desde cero


func _marcar_melodia() -> void:
	if not _repro.playing:
		_avisar("Dale a P para reproducir primero")
		return
	_melodia.append(_posicion() + _offset_calibracion)
	_melodia.sort()
	_destello_marca = 1.0
	_guardar(true)


func _borrar_ultima() -> void:
	if _melodia.is_empty():
		_avisar("No hay marcas que borrar")
		return
	_melodia.remove_at(_melodia.size() - 1)
	_guardar(true)
	_avisar("Borrada · quedan %d" % _melodia.size())


func _vaciar_melodia() -> void:
	_melodia.clear()
	_guardar(true)
	_avisar("Melodía vaciada")


func _avisar(txt: String) -> void:
	_mensaje = txt
	_mensaje_ttl = 2.5


# ---------------------------------------------------------------- guardado

## Ruta ABSOLUTA, no res://.
##
## res:// no es el disco: es el sistema de recursos del motor. Escribir ahi
## funciona, pero file_exists() consulta la base de datos de recursos, y un
## archivo creado en tiempo de ejecucion no esta en ella. Antes guardaba bien
## y luego no se encontraba a si mismo al reabrir.
func _ruta_tema() -> String:
	return ProjectSettings.globalize_path(CARPETA_TEMAS) + "/" + _id + ".json"


func _guardar(silencioso: bool = false) -> void:
	if _id == "": return
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CARPETA_TEMAS))

	var rejilla := []
	for x in _tramos:
		rejilla.append({
			"desde": snappedf(float(x["desde"]), 0.0001),
			"bpm": snappedf(float(x["bpm"]), 0.01),
			"compas": int(x["compas"]),
		})

	var notas := []
	for x in _melodia:
		notas.append(snappedf(x, 0.0001))

	var datos := {
		"id": _id,
		"archivo": _archivos[_indice_archivo] if not _archivos.is_empty() else "",
		"rejilla": rejilla,
		"secciones": _secciones,
		"melodia": notas,
	}

	var f := FileAccess.open(_ruta_tema(), FileAccess.WRITE)
	if f == null:
		if not silencioso: _avisar("No se pudo escribir")
		return
	f.store_string(JSON.stringify(datos, "  "))
	f.close()

	if not silencioso:
		_avisar("Guardado · %d notas · %d tramos" % [_melodia.size(), _tramos.size()])
	print("GUARDADO  %d notas  ·  %d tramos" % [_melodia.size(), _tramos.size()])


func _cargar_tema_si_existe() -> void:
	if not FileAccess.file_exists(_ruta_tema()):
		print("SIN TEMA PREVIO · se empieza de cero")
		return
	var datos = JSON.parse_string(FileAccess.get_file_as_string(_ruta_tema()))
	if typeof(datos) != TYPE_DICTIONARY: return

	_secciones = datos.get("secciones", [])

	_tramos.clear()
	for x in datos.get("rejilla", []):
		_tramos.append({
			"desde": float(x.get("desde", 0.0)),
			"bpm": float(x.get("bpm", 120.0)),
			"compas": int(x.get("compas", 4)),
		})

	# Compatibilidad con el formato antiguo, de un solo BPM para toda la cancion.
	if _tramos.is_empty() and datos.has("bpm"):
		_tramos.append({
			"desde": float(datos.get("offset", 0.0)),
			"bpm": float(datos.get("bpm", 120.0)),
			"compas": int(datos.get("tiemposPorCompas", 4)),
		})

	_melodia.clear()
	for x in datos.get("melodia", []):
		_melodia.append(float(x))

	print("CARGADO  %d notas  ·  %d tramos" % [_melodia.size(), _tramos.size()])


# ---------------------------------------------------------------- pintado

func _barra_db(db: float) -> String:
	if db <= -40.0:
		return "[color=#664444]" + "░".repeat(14) + "[/color]  mudo"
	var f := clampf((db + 40.0) / 46.0, 0.0, 1.0)
	var lleno := int(f * 14)
	return "[color=#66aaff]" + "█".repeat(lleno) + "[/color]" \
		+ "[color=#333344]" + "░".repeat(14 - lleno) + "[/color]  %+.0f dB" % db


func _reloj(s: float) -> String:
	return "%d:%02d" % [int(s) / 60, int(s) % 60]


func _actualizar_texto() -> void:
	var L := []
	L.append("[b]GRABADOR DE TEMAS — E.N.M.A.[/b]")
	L.append("")

	if _archivos.is_empty():
		L.append("[color=#ff8866]No hay canciones.[/color]")
		L.append("")
		L.append("  Deja un .mp3, .ogg o .wav en [color=#66aaff]proto/grabador-temas/audio/[/color] y reinicia.")
		_etiqueta.text = "\n".join(L)
		return

	var t := _posicion()
	L.append("  [b]%s[/b]   [color=#666677](%d de %d · [ ] cambia)[/color]" % [
		_archivos[_indice_archivo], _indice_archivo + 1, _archivos.size()])
	L.append("  %s / %s   %s" % [_reloj(t), _reloj(_duracion()),
		"[color=#88ff88]▶[/color]" if _repro.playing else "[color=#777788]■[/color]"])
	L.append("")

	L.append("[b][color=#88ff88]███  MELODÍA · %d notas  ███[/color][/b]" % _melodia.size())
	L.append("     [b]ESPACIO[/b] marca    RETROCESO borra    SUPR vacía")
	L.append("")

	if not _tramos.is_empty():
		var i := _tramo_en(t)
		var tr: Dictionary = _tramos[i]
		var compas := int(tr["compas"])
		var col: Color = COLORES_COMPAS.get(compas, Color.WHITE)
		L.append("[color=#ffcc66]MAPA DE TEMPO[/color]   %d tramo%s" % [
			_tramos.size(), "" if _tramos.size() == 1 else "s"])
		L.append("  Vas por el tramo [b]%d[/b] · [color=#%s][b]%d/4[/b][/color] · [b]%.1f[/b] BPM · desde %s" % [
			i + 1, col.to_html(false), compas, float(tr["bpm"]), _reloj(float(tr["desde"]))])
		L.append("")
		L.append("     [b]G[/b] parte aquí un tramo nuevo       [b]X[/b] quita este tramo")
		L.append("     [b]T[/b] golpea al ritmo     [b]↑↓←→[/b] BPM     [b]C[/b] compás")
		if _hay_seleccion():
			var sa: float = minf(_sel_desde, _sel_hasta)
			var sb: float = maxf(_sel_desde, _sel_hasta)
			var csel: Color = COLORES_COMPAS.get(_sel_compas, Color.WHITE)
			L.append("  [b][color=#ffffff]SELECCIÓN[/color][/b]  %s → %s   ·   [color=#%s][b]%d/4[/b][/color] · [b]%.1f[/b] BPM" % [
				_reloj(sa), _reloj(sb), csel.to_html(false), _sel_compas, _sel_bpm])
			L.append("  [b][color=#88ff88]ENTER[/color][/b] aplica al rango   ·   clic derecho lo quita")
		else:
			L.append("     [color=#666677]arrastra sobre la barra para elegir un rango · clic simple mueve el cursor[/color]")
		L.append("     [color=#666677]las teclas editan la selección si la hay, y si no el tramo del cursor[/color]")
		L.append("")
		L.append("[color=#ffcc66]PRECISIÓN[/color]   ventana visible: [b]%.1f s[/b]   ·   %.0f ms por píxel" % [
			_vista().y - _vista().x,
			((_vista().y - _vista().x) / maxf(1.0, _ancho_barra())) * 1000.0])
		L.append("     [b]rueda[/b] o [b]Z[/b]/[b]A[/b] zoom    [b]V[/b] ver todo    [b], .[/b] empujan el borde un tiempo")
		L.append("     [b]N[/b] ajuste a rejilla: %s  [color=#666677](pega los bordes al pulso más cercano)[/color]" % (
			"[color=#88ff88]ON[/color]" if _ajustar_a_rejilla else "[color=#ff8866]OFF[/color]"))
		L.append("")

	L.append("[color=#ffcc66]MEZCLADOR[/color]")
	L.append("  Canción ..... %s  [color=#666677]1 − 2 +[/color]" % _barra_db(_vol_cancion))
	L.append("  Metrónomo ... %s  [color=#666677]3 − 4 +[/color]" % _barra_db(_vol_metronomo))

	if _mensaje_ttl > 0.0:
		L.append("")
		L.append("  [color=#66aaff]%s[/color]" % _mensaje)

	_etiqueta.text = "\n".join(L)


func _draw() -> void:
	var w := size.x
	var h := size.y

	# El fondo va aqui y no en un hijo, o taparia todo lo demas.
	draw_rect(Rect2(0, 0, w, h), Color(0.07, 0.07, 0.09))

	if _archivos.is_empty() or _tramos.is_empty(): return
	var dur := _duracion()
	if dur <= 0.0: return

	var t := _posicion()
	var margen := MARGEN_BARRA
	var ancho := _ancho_barra()
	var v := _vista()
	var visible: float = maxf(0.001, v.y - v.x)

	# ---- barra de tramos ----
	var by := _y_barra()
	var alto := ALTO_BARRA
	var actual := _tramo_en(t)

	for i in _tramos.size():
		var desde: float = float(_tramos[i]["desde"])
		var hasta: float = float(_tramos[i + 1]["desde"]) if i + 1 < _tramos.size() else dur
		var x0: float = clampf(_x_de_tiempo(desde), margen, margen + ancho)
		var x1: float = clampf(_x_de_tiempo(hasta), margen, margen + ancho)
		if x1 <= x0: continue
		var compas: int = int(_tramos[i]["compas"])
		var c: Color = COLORES_COMPAS.get(compas, Color.WHITE)
		var es_actual := (i == actual)

		draw_rect(Rect2(x0, by, maxf(2.0, x1 - x0), alto),
			c if es_actual else c.darkened(0.45))

		if es_actual:
			draw_rect(Rect2(x0, by, maxf(2.0, x1 - x0), alto), Color.WHITE, false, 2.0)

		if x1 - x0 > 62.0:
			draw_string(ThemeDB.fallback_font, Vector2(x0 + 6, by + alto * 0.62),
				"%d/4  %.0f" % [compas, float(_tramos[i]["bpm"])],
				HORIZONTAL_ALIGNMENT_LEFT, x1 - x0 - 10, 13, Color(0, 0, 0, 0.85))

	# ---- seleccion de rango ----
	if _hay_seleccion():
		var sa: float = minf(_sel_desde, _sel_hasta)
		var sb: float = maxf(_sel_desde, _sel_hasta)
		var xa: float = clampf(_x_de_tiempo(sa), margen, margen + ancho)
		var xb: float = clampf(_x_de_tiempo(sb), margen, margen + ancho)
		var csel: Color = COLORES_COMPAS.get(_sel_compas, Color.WHITE)
		draw_rect(Rect2(xa, by - 6, xb - xa, alto + 12), Color(1, 1, 1, 0.18))
		draw_rect(Rect2(xa, by - 6, xb - xa, alto + 12), csel, false, 2.0)
		draw_string(ThemeDB.fallback_font, Vector2(xa + 6, by - 12),
			"%d/4  %.1f BPM  ·  ENTER aplica" % [_sel_compas, _sel_bpm],
			HORIZONTAL_ALIGNMENT_LEFT, 320, 12, csel)

	draw_rect(Rect2(margen, by, ancho, alto), Color(1, 1, 1, 0.22), false, 1.0)

	# Reglilla de tiempos cada 30 s, para orientarse en canciones largas
	# Reglilla de tiempo, con paso segun el zoom
	var paso_s := 30.0
	for cand in [0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0]:
		if visible / cand <= 14.0:
			paso_s = cand
			break
	var marca := int(v.x / paso_s)
	while marca * paso_s <= v.y:
		var xr: float = _x_de_tiempo(marca * paso_s)
		if xr >= margen and xr <= margen + ancho:
			draw_line(Vector2(xr, by - 6), Vector2(xr, by), Color(1, 1, 1, 0.25), 1.0)
			draw_string(ThemeDB.fallback_font, Vector2(xr + 3, by - 9),
				_reloj(marca * paso_s) if paso_s >= 1.0 else "%.1f" % (marca * paso_s),
				HORIZONTAL_ALIGNMENT_LEFT, 60, 11, Color(1, 1, 1, 0.35))
		marca += 1

	# ---- lineas de rejilla, solo si el zoom deja verlas ----
	if visible < 40.0:
		var tt := v.x
		var guardia := 0
		while tt <= v.y and guardia < 2000:
			guardia += 1
			var tr2: Dictionary = _tramos[_tramo_en(tt)]
			var paso: float = 60.0 / float(tr2["bpm"])
			var desde2: float = float(tr2["desde"])
			var n_pulso: int = int(ceilf((tt - desde2) / paso))
			var tp: float = desde2 + n_pulso * paso
			if tp > v.y: break
			var xg: float = _x_de_tiempo(tp)
			var fuerte_g: bool = posmod(n_pulso, int(tr2["compas"])) == 0
			draw_line(Vector2(xg, by), Vector2(xg, by + alto),
				Color(1, 1, 1, 0.55 if fuerte_g else 0.18),
				2.0 if fuerte_g else 1.0)
			tt = tp + paso * 0.5

	# ---- marcas de melodia, debajo ----
	var my := by + alto + 16.0
	draw_line(Vector2(margen, my), Vector2(margen + ancho, my), Color(1, 1, 1, 0.10), 1.0)
	for m in _melodia:
		var x: float = _x_de_tiempo(m)
		if x < margen or x > margen + ancho: continue
		draw_line(Vector2(x, my - 7), Vector2(x, my + 7), Color(0.5, 1.0, 0.6, 0.8), 1.0)

	# ---- minimapa: la cancion entera, con la ventana visible marcada ----
	if visible < dur - 0.01:
		var mh := 10.0
		var mmy := by - 34.0
		draw_rect(Rect2(margen, mmy, ancho, mh), Color(1, 1, 1, 0.07))
		var mx0: float = margen + (v.x / dur) * ancho
		var mx1: float = margen + (v.y / dur) * ancho
		draw_rect(Rect2(mx0, mmy, maxf(3.0, mx1 - mx0), mh), Color(1, 1, 1, 0.35))
		draw_rect(Rect2(margen, mmy, ancho, mh), Color(1, 1, 1, 0.20), false, 1.0)

	# ---- cursor ----
	var xc: float = _x_de_tiempo(t)
	draw_line(Vector2(xc, by - 12), Vector2(xc, my + 12), Color.WHITE, 2.0)
	draw_circle(Vector2(xc, by - 16), 4.0, Color.WHITE)

	# ---- pulso, en grande ----
	var cy := h - 95.0
	var cx := w * 0.5
	var fuerte := _es_inicio_compas(t)
	var cc: Color = COLORES_COMPAS.get(_compas_en(t), Color.WHITE)
	if not fuerte: cc = cc.darkened(0.35)
	var r := (22.0 if not fuerte else 30.0) + _destello_pulso * 16.0
	draw_circle(Vector2(cx, cy), r, cc.lerp(Color.WHITE, _destello_pulso * 0.55))

	if _destello_marca > 0.0:
		draw_arc(Vector2(cx, cy), r + 18.0, 0.0, TAU, 40,
			Color(0.5, 1.0, 0.5, _destello_marca), 3.0)

	# ---- puntos del compas: cuantos tiempos tiene y en cual vas ----
	var n := _compas_en(t)
	var tiempo_actual := posmod(_pulso_local(t), n)
	for k in n:
		var px := cx - (n - 1) * 11.0 + k * 22.0
		var activo := (k == tiempo_actual)
		draw_circle(Vector2(px, cy + 48.0), 6.0 if activo else 4.0,
			Color.WHITE if activo else Color(1, 1, 1, 0.25))
