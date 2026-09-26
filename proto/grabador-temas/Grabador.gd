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
@onready var _sfx: AudioStreamPlayer = $Golpes

var _etiqueta: RichTextLabel

# --- datos del tema ---
var _id := ""
## Mapa de tempo: [{desde, bpm, compas}, ...] ordenado por "desde".
var _tramos: Array[Dictionary] = []
var _melodia: Array[float] = []

## LA MELODIA COMO FIGURAS.
##
## En vez de grabarla pulsando —que deja las notas donde caiga tu dedo— se
## COLOCAN figuras encajadas en la rejilla del tramo. Asi la melodia queda
## cuadriculada por construccion y la separacion minima entre notas la fija la
## propia figura, no el pulso de nadie.
##
## Cada entrada: {"t": segundos, "fig": denominador, "p": puntillo}
##   1 redonda · 2 blanca · 4 negra · 8 corchea · 16 semicorchea
##   p = true suma la mitad: una negra con puntillo dura tiempo y medio
##
## ⚠️ Es dato del EDITOR. Al JSON van tambien los instantes sueltos en
## "melodia", que es lo unico que consume el nucleo.
var _notas: Array[Dictionary] = []
var _figura := 4                     # negra
var _puntillo := false

## DOS VOCES. Muchas canciones llevan una melodia principal y otra de fondo
## que solo aparece en algunos tramos. Cada nota lleva su voz en "v", y se
## edita una cada vez: lo que no es de la voz activa se ve, pero no se toca.
## La segunda voz NO existe hasta que la pides: la mayoria de temas tienen una
## sola linea, y partir el carril en dos por si acaso es quitarle la mitad del
## sitio a la que si usas.
const MAX_VOCES := 2
const NOMBRE_VOZ: Array[String] = ["principal", "de fondo"]
var _voces := 1
var _voz := 0               # el puntillo suma la mitad: negra. = 1.5 tiempos

## Pintar notas arrastrando. Se recuerda la ultima casilla escrita para no
## reescribir cien veces la misma mientras el raton tiembla dentro de ella.
## SELECCION DE NOTAS. Se guarda como RANGO DE TIEMPO, no como indices: las
## notas se reordenan en cuanto tocas una, y unos indices guardados apuntarian
## a otras. Un rango sigue valiendo pase lo que pase.
var _sel_n_a := -1.0
var _sel_n_b := -1.0
var _portapapeles_notas: Array[Dictionary] = []

## Arrastre de la seleccion.
var _moviendo_sel := false
var _mov_agarre := 0.0
var _mov_base := 0.0

var _pintando := 0                   # 0 nada · 1 poniendo · -1 borrando
var _ultima_casilla := -99999.0
var _pintadas := 0
var _tolerancia_ms := 120.0          # zona de ayuda, en milisegundos

const FIGURAS: Array[int] = [1, 2, 4, 8, 16, 32]
const NOMBRE_FIGURA := {
	1: "redonda", 2: "blanca", 4: "negra", 8: "corchea", 16: "semicorchea",
	32: "fusa",
}
const COLOR_FIGURA := {
	1: Color(0.95, 0.75, 0.35),
	2: Color(0.55, 0.85, 0.55),
	4: Color(0.45, 0.70, 0.95),
	8: Color(0.80, 0.55, 0.95),
	16: Color(0.95, 0.50, 0.55),
	32: Color(0.40, 0.88, 0.88),
}
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

## Estado del guardado, SIEMPRE a la vista.
##
## El autoguardado silencioso es comodo hasta que falla: si no se ve, no te
## enteras de que llevas media hora trabajando sin que se escriba nada. Aqui
## se guarda cuando fue el ultimo, con que contenido, y si fallo.
var _guardado_hora := ""
var _guardado_hace := 99999.0
var _guardado_notas := 0
var _guardado_tramos := 0
var _guardado_error := ""
var _destello_guardado := 0.0

## La chuleta completa, detras de H. Por defecto escondida: el sitio es para
## la linea de tiempo.
var _ayuda_visible := false
var _texto_ttl := 0.0

## Fecha del archivo cuando lo cargamos.
##
## ⚠️ POR QUE EXISTE: con autoguardado constante, dos ventanas abiertas sobre la
## misma cancion se pisan y GANA LA ULTIMA EN CERRARSE. Una ventana vieja, con
## un estado de hace una hora, puede machacar el trabajo de toda la tarde al
## salir. Paso de verdad: 980 notas sustituidas por 8.
##
## Si el archivo cambio por fuera desde que lo cargamos, NO se sobrescribe: se
## escribe al lado con otro nombre y se avisa. Asi no se pierde ninguna de las
## dos versiones.
var _tema_mtime := 0
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
var _arrastrando_mini := false

## Indice del tramo cuyo borde se esta arrastrando. -1 = ninguno.
## Un borde es el "desde" de un tramo, y es COMPARTIDO: moverlo encoge el
## anterior y estira este, o al reves. Por eso se toca un solo valor.
var _arrastrando_borde := -1

## Origen real de la rejilla de cada tramo. Ver _recalcular_origenes().
var _origenes: Array[float] = []

## Rejilla continua: cada tramo arranca donde acabo el compas anterior, no
## donde soltaste el raton. Ver _recalcular_origenes().
var _rejilla_continua := true

## Tramos marcados con MAYUS+clic, para copiar un patron.
var _marcados: Array[int] = []

## Patron copiado: [{dur, bpm, compas}, ...] con duraciones RELATIVAS, para
## poder pegarlo en cualquier punto de la cancion.
var _portapapeles: Array[Dictionary] = []

## Pegado a la espera de confirmacion con ENTER.
var _pegando := false
var _pegar_en := 0.0

## Arrastre del bloque marcado. Se guarda donde estaba el raton y donde
## empezaba el bloque al agarrarlo, para mover en relativo y que el bloque no
## salte al punto del cursor.
var _arrastrando_bloque := false
var _bloque_agarre := 0.0
var _bloque_ini := 0.0

## Ventana visible de la barra. Con 6 minutos a lo ancho, cada pixel son
## decimas de segundo y es imposible clavar un limite de compas. El zoom es
## lo que hace posible la precision.
var _vista_ini := 0.0
var _vista_fin := -1.0        # -1 = toda la cancion

## Ajuste a rejilla. Un cambio de compas cae SIEMPRE sobre un tiempo del
## tramo anterior, asi que pegarse a la rejilla acierta casi siempre.
var _ajustar_a_rejilla := true

## La vista persigue al cursor mientras suena. Sin esto hay que ir arrastrando
## el minimapa a mano, que es imposible si estas tocando.
var _seguir := true

# --- mezclador ---
var _vol_cancion := 0.0
var _vol_metronomo := 0.0
var _clic_fuerte: AudioStreamWAV
var _clic_debil: AudioStreamWAV

const MARGEN_BARRA := 30.0
# Con la ayuda escondida sobra sitio, y va donde se trabaja: el carril de
# notas es ahora casi el triple de alto.
const ALTO_BARRA := 64.0
const ALTO_NOTAS := 96.0
const ALTO_MINIMAPA := 22.0
const ALTO_ACIERTOS := 26.0
const ALTO_PAD := 76.0

## Un color por compas, para que un 2/4 metido en medio se vea sin leer nada.
const COLORES_COMPAS := {
	2: Color(0.95, 0.45, 0.35),
	3: Color(0.55, 0.80, 0.45),
	4: Color(0.35, 0.60, 0.95),
	5: Color(0.85, 0.65, 0.35),
	6: Color(0.70, 0.50, 0.90),
	7: Color(0.40, 0.80, 0.80),
}

## Ambar: todo lo que tiene que ver con copiar y pegar un patron.
const COLOR_PATRON := Color(1.0, 0.82, 0.25)

# ------------------------------------------------------------- deshacer
#
# Una pila de instantaneas de lo unico que es trabajo: el mapa de tempo y la
# melodia. Todo lo demas (zoom, cursor, volumen) se rehace en un segundo y no
# tiene sentido guardarlo.
#
# Se apila ANTES de cada cambio, no despues, para que deshacer devuelva el
# estado de justo antes.

const MAX_DESHACER := 80

var _pila: Array[Dictionary] = []

## Lo deshecho, esperando a que CTRL+Y lo devuelva. Cualquier cambio NUEVO la
## vacia: a partir de ahi esa rama ya no existe.
var _rehacer_pila: Array[Dictionary] = []

var _ultimo_motivo := ""
var _ultimo_recuerdo := -999.0


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
	_etiqueta.offset_right = -(ANCHO_COLUMNA + 26.0)
	_etiqueta.offset_bottom = -296.0
	_etiqueta.add_theme_font_size_override("normal_font_size", 15)
	add_child(_etiqueta)


## Dos clics de TIMBRE distinto, no el mismo mas bajito: con la cancion encima
## un mismo sonido a dos volumenes se confunde.
func _generar_clics() -> void:
	_clic_fuerte = _sintetizar(660.0, 0.075, 55.0, 1.0, true)     # "DONG"
	_clic_debil = _sintetizar(2400.0, 0.014, 420.0, 0.45, false)  # "tic"
	_metronomo.stream = _clic_fuerte

	# Ocho grados de una pentatonica: suenan bien en cualquier orden, asi que
	# una racha larga sale melodica en vez de estridente.
	var grados: Array[int] = [0, 2, 4, 7, 9, 12, 14, 16]
	_tonos.clear()
	for i in grados.size():
		var f: float = 523.25 * pow(2.0, float(grados[i]) / 12.0)
		_tonos.append(_sintetizar(f, 0.11, 26.0, 0.55, true))
	_tono_fallo = _sintetizar(130.0, 0.09, 40.0, 0.40, false)


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
	_notas.clear()
	_tramos.clear()
	# Las pilas eran de la cancion anterior: deshacer aqui mezclaria las dos.
	_pila.clear()
	_rehacer_pila.clear()
	_ultimo_motivo = ""
	_portapapeles.clear()
	_marcados.clear()
	_pegando = false
	_pintando = 0
	_moviendo_sel = false
	_limpiar_sel_notas()
	_portapapeles_notas.clear()
	_voz = 0
	_voces = 1
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
## REPARTO VERTICAL. De abajo hacia arriba, con huecos de verdad:
##
##     size.y -  40   fondo de la linea de notas
##     size.y - 136   linea de notas          (96)
##     size.y - 156   fondo de la barra           ← hueco de 20
##     size.y - 220   barra de tramos         (64)
##     size.y - 246   fondo del minimapa          ← hueco de 26
##     size.y - 268   minimapa                (22)
##     size.y - 296   aqui acaba el texto
##
## El indicador de pulso YA NO va abajo en el centro: se comia la linea de
## notas entera. Vive en su propia columna a la derecha.
## Lo que el ensayo le roba a la pila: la barra de aciertos y el pad.
func _extra_ensayo() -> float:
	return (ALTO_ACIERTOS + 8.0 + ALTO_PAD + 10.0) if _ensayo else 0.0


func _y_barra() -> float:
	return size.y - 220.0 - _extra_ensayo()


## El PAD. Aqui es donde se golpea, y se ve.
func _y_pad() -> float:
	return size.y - ALTO_PAD - 12.0


func _sobre_el_pad(y: float) -> bool:
	return _ensayo and y >= _y_pad() - 2.0


func _ancho_barra() -> float:
	return size.x - MARGEN_BARRA * 2.0


## La barra de desplazamiento: toda la cancion, siempre, con la ventana
## visible marcada. Se arrastra para moverse.
## La linea de figuras, justo debajo de la barra de tramos.
func _y_notas() -> float:
	return _y_barra() + ALTO_BARRA + 20.0


## Ancho reservado a la columna del pulso, a la derecha del texto.
const ANCHO_COLUMNA := 230.0


func _sobre_las_notas(y: float) -> bool:
	return y >= _y_notas() - 2.0 and y <= _y_notas() + ALTO_NOTAS + 2.0


## La barra de aciertos, debajo de las notas. Solo existe en ensayo.
func _y_aciertos() -> float:
	return _y_notas() + ALTO_NOTAS + 8.0


## Cuanto dura una figura en el tramo que la contiene.
## Una negra es un tiempo; la redonda son cuatro; la corchea, medio.
func _dur_figura(fig: int, t: float, con_puntillo: bool = false) -> float:
	if _tramos.is_empty(): return 0.0
	var d: float = (60.0 / _bpm_en(t)) * (4.0 / float(fig))
	return d * 1.5 if con_puntillo else d


## Duracion de una nota ya colocada.
func _dur_nota(n: Dictionary) -> float:
	return _dur_figura(int(n["fig"]), float(n["t"]), bool(n.get("p", false)))


func _nombre_figura_actual() -> String:
	var base: String = NOMBRE_FIGURA.get(_figura, "?")
	return base + " con puntillo" if _puntillo else base


## Paso de la rejilla de colocacion.
##
## ⚠️ LA REJILLA NO ES EL ANCHO DE LA FIGURA. Son cosas distintas, igual que en
## una partitura: el valor dice cuanto DURA la nota, no donde puede ir.
##
## Antes el paso era la duracion de la figura, y eso dejaba huecos imposibles:
## una blanca solo caia cada 2 tiempos desde el origen, asi que no se podia
## poner detras de tres negras —el sitio donde acaban, a 3 tiempos, no existia
## en esa rejilla—. Y en un 3/4 la rejilla de blancas se iba desfasando de las
## barras de compas, porque 2 tiempos no caben un numero entero de veces en 3.
##
## Ahora el paso se limita a UN TIEMPO: cualquier figura, por larga que sea,
## se puede poner en cualquier pulso. Las mas cortas que el tiempo mantienen su
## propia resolucion, y el puntillo parte el paso por la mitad porque ahi es
## donde caen los ritmos con puntillo.
func _paso_rejilla(t: float) -> float:
	var tiempo: float = 60.0 / _bpm_en(t)
	var paso: float = minf(_dur_figura(_figura, t), tiempo)
	return paso * 0.5 if _puntillo else paso


func _encajar_figura(t: float) -> float:
	if _tramos.is_empty(): return t
	var i := _tramo_en(t)
	var paso: float = _paso_rejilla(t)
	if paso <= 0.0: return t
	var org: float = _origen_de(i)
	return org + roundf((t - org) / paso) * paso


# ------------------------------------------------- seleccion de notas

func _hay_sel_notas() -> bool:
	return _sel_n_a >= 0.0 and _sel_n_b >= 0.0


func _sel_ini() -> float: return minf(_sel_n_a, _sel_n_b)
func _sel_fin() -> float: return maxf(_sel_n_a, _sel_n_b)


func _nota_seleccionada(i: int) -> bool:
	if not _hay_sel_notas(): return false
	if _voz_de(_notas[i]) != _voz: return false
	var t: float = float(_notas[i]["t"])
	return t >= _sel_ini() - 0.001 and t <= _sel_fin() + 0.001


func _indices_seleccionados() -> Array[int]:
	var r: Array[int] = []
	for i in _notas.size():
		if _nota_seleccionada(i): r.append(i)
	return r


func _limpiar_sel_notas() -> void:
	_sel_n_a = -1.0
	_sel_n_b = -1.0


## MAYUS+clic. El primero ancla; el segundo estira hasta ahi y coge todo lo que
## haya en medio.
func _marcar_rango_notas(t: float) -> void:
	var i := _nota_en(t)
	if i < 0:
		_avisar("MAYÚS+clic sobre una nota para empezar la selección")
		return

	var n: float = float(_notas[i]["t"])
	if not _hay_sel_notas():
		_sel_n_a = n
		_sel_n_b = n
		_avisar("Ancla puesta en %s · MAYÚS+clic en otra para coger el tramo" % _reloj(n))
		return

	_sel_n_b = n
	var cuantas := _indices_seleccionados().size()
	_avisar("%d nota%s seleccionada%s · arrastra para mover · SUPR borra · CTRL+C copia" % [
		cuantas, "" if cuantas == 1 else "s", "" if cuantas == 1 else "s"])


## Mueve la seleccion entera. El desplazamiento se encaja a la rejilla, asi
## que las notas siguen cuadradas despues de moverlas.
func _mover_sel_notas(delta: float) -> void:
	var idx := _indices_seleccionados()
	if idx.is_empty() or absf(delta) < 0.0005: return

	# Que la primera no se salga por el principio.
	var primera: float = float(_notas[idx[0]]["t"])
	if primera + delta < 0.0: delta = -primera

	# Lo que haya en el destino se quita. Mover un grupo encima de otras notas
	# las SUSTITUYE; si no, quedarian dos en el mismo instante y la melodia
	# tendria notas duplicadas invisibles.
	var d_ini: float = _sel_ini() + delta
	var ultima: Dictionary = _notas[idx[-1]]
	var d_fin: float = float(ultima["t"]) + delta + _dur_nota(ultima)
	for k in range(_notas.size() - 1, -1, -1):
		if _nota_seleccionada(k) or _voz_de(_notas[k]) != _voz: continue
		var tk: float = float(_notas[k]["t"])
		if tk > d_ini - 0.001 and tk < d_fin - 0.001:
			_notas.remove_at(k)

	# Los indices han podido correrse al quitar: se recalculan.
	idx = _indices_seleccionados()
	for i in idx:
		_notas[i]["t"] = float(_notas[i]["t"]) + delta
	_sel_n_a += delta
	_sel_n_b += delta
	_notas.sort_custom(func(a, b): return float(a["t"]) < float(b["t"]))


func _borrar_sel_notas() -> void:
	var idx := _indices_seleccionados()
	if idx.is_empty(): return
	_recordar("borrar %d notas" % idx.size(), false)
	for k in range(idx.size() - 1, -1, -1):
		_notas.remove_at(idx[k])
	_limpiar_sel_notas()
	_sincronizar_melodia()
	_guardar(true)
	_avisar("%d notas borradas" % idx.size())


## Se copia en RELATIVO al inicio de la seleccion, para poder pegarlo donde sea.
func _copiar_sel_notas() -> void:
	var idx := _indices_seleccionados()
	if idx.is_empty():
		_avisar("No hay notas seleccionadas")
		return
	_portapapeles_notas.clear()
	var cero: float = float(_notas[idx[0]]["t"])
	for i in idx:
		_portapapeles_notas.append({
			"dt": float(_notas[i]["t"]) - cero,
			"fig": int(_notas[i]["fig"]),
			"p": bool(_notas[i].get("p", false)),
		})
	_avisar("%d notas copiadas · CTRL+V las pega en el cursor" % _portapapeles_notas.size())


func _pegar_notas() -> void:
	if _portapapeles_notas.is_empty():
		_avisar("No hay notas copiadas")
		return

	var destino := _encajar_figura(_posicion())
	_recordar("pegar %d notas" % _portapapeles_notas.size(), false)

	# ⚠️ NO se pega nota a nota con _escribir_nota: esa funcion sustituye lo que
	# encuentra, y con notas pegadas muy juntas —o con una larga que cubre a la
	# siguiente— se comia LAS SUYAS PROPIAS. El pegado tiene que dejar
	# exactamente lo que copiaste.
	#
	# Se hace en dos pasos: primero se vacia el hueco de destino, y luego se
	# meten las copiadas tal cual, sin que se toquen entre ellas.
	var ultima: Dictionary = _portapapeles_notas[-1]
	var largo: float = float(ultima["dt"]) 		+ _dur_figura(int(ultima["fig"]), destino, bool(ultima["p"]))

	for k in range(_notas.size() - 1, -1, -1):
		if _voz_de(_notas[k]) != _voz: continue
		var tk: float = float(_notas[k]["t"])
		if tk > destino - 0.001 and tk < destino + largo - 0.001:
			_notas.remove_at(k)

	for x in _portapapeles_notas:
		_notas.append({
			"t": destino + float(x["dt"]),
			"fig": int(x["fig"]),
			"p": bool(x["p"]),
			"v": _voz,
		})
	_notas.sort_custom(func(a, b): return float(a["t"]) < float(b["t"]))

	_sel_n_a = destino
	_sel_n_b = destino + float(_portapapeles_notas[-1]["dt"])
	_sincronizar_melodia()
	_guardar(true)
	_avisar("%d notas pegadas en %s" % [_portapapeles_notas.size(), _reloj(destino)])


## Nota que ARRANCA en esta casilla, con un milisegundo de holgura.
##
## Hace falta porque la casilla se recalcula en cada clic y puede caer unos
## microsegundos antes o despues de donde quedo guardada la nota. Comparando
## sin holgura, una casilla que cae 7 µs por delante de su nota no la encuentra
## y se cuela una DUPLICADA separada por millonesimas — que ademas rompe el
## calculo del hueco minimo y dispara el aviso de solape sin motivo.
## Voz de una nota. Las de antes de que existieran las voces son la principal.
func _voz_de(n: Dictionary) -> int:
	return int(n.get("v", 0))


## TAB. Con una sola voz, la anade. Con dos, alterna. Y si vuelves a la
## principal dejando vacia la de fondo, se quita sola: no se queda un carril
## medio vacio ocupando sitio.
func _cambiar_voz() -> void:
	if _voces == 1:
		_voces = 2
		_voz = 1
		_limpiar_sel_notas()
		if _ensayo: _reiniciar_ensayo()
		_avisar("Voz de fondo añadida · TAB vuelve a la principal")
		return

	_voz = (_voz + 1) % _voces
	_limpiar_sel_notas()
	if _ensayo: _reiniciar_ensayo()      # otra melodia, otras cuentas

	if _voz == 0 and _notas_de_voz(1) == 0:
		_voces = 1
		_avisar("Voz principal · la de fondo estaba vacía y se quitó")
		return
	_avisar("Voz %s · %d notas" % [NOMBRE_VOZ[_voz], _notas_de_voz(_voz)])


func _notas_de_voz(v: int) -> int:
	var c := 0
	for n in _notas:
		if _voz_de(n) == v: c += 1
	return c


## Casilla donde va a caer una nota nueva.
##
## ⚠️ No es lo mismo que encajar a la rejilla. Una redonda ocupa CUATRO
## casillas: si clicas en la segunda para poner la nota de al lado, el clic cae
## DENTRO de ella y la sustituye. Pasaba todo el rato con figuras largas.
##
## La regla: la CABEZA de una nota es la nota; su COLA es «la de después». Así
## poner algo detrás de una redonda es clicar en cualquier parte de su cuerpo,
## sin miedo a borrarla.
func _casilla_para_poner(t: float) -> float:
	var enc := _encajar_figura(t)
	var i := _nota_en(t)
	if i < 0: return enc

	var n0: float = float(_notas[i]["t"])
	if absf(n0 - enc) < 0.001: return enc      # su cabeza: es sobre ella

	# Y la casilla resultante NUNCA puede caer antes del final de esa nota.
	var fin: float = n0 + _dur_nota(_notas[i])
	var c := _encajar_figura(fin)
	while c < fin - 0.002:
		c += _paso_rejilla(fin)
	return maxf(c, fin)


func _nota_que_arranca_en(enc: float) -> int:
	for i in _notas.size():
		if _voz_de(_notas[i]) != _voz: continue
		if absf(float(_notas[i]["t"]) - enc) < 0.001: return i
	return -1


## Solo mira la voz activa: asi editar la de fondo no toca la principal.
##
## ⚠️ El final NO cuenta como «dentro», con 2 ms de holgura. Las notas se
## guardan redondeadas a 4 decimales, asi que al recargar el final de una puede
## quedar una decima de milisegundo MAS ALLA de la casilla siguiente. Sin esta
## holgura, poner una nota justo detras de otra se comia la de delante: la
## casilla caia «dentro» por microsegundos.
func _nota_en(t: float) -> int:
	for i in _notas.size():
		if _voz_de(_notas[i]) != _voz: continue
		var n0: float = float(_notas[i]["t"])
		if t >= n0 and t < n0 + _dur_nota(_notas[i]) - 0.002: return i
	return -1


## Escribe una figura en su casilla. NO apila deshacer ni guarda: de eso se
## encarga quien empieza y termina el trazo, para que un arrastre entero sea
## UN paso y UN guardado, no ochenta.
##
## Si ya hay algo en esa casilla lo sustituye: colocar encima es corregir.
func _escribir_nota(enc: float) -> void:
	var ya := _nota_que_arranca_en(enc)
	if ya < 0: ya = _nota_en(enc)
	if ya >= 0:
		if _es_la_misma(_notas[ya], enc):
			return                              # ya estaba puesta igual
		_notas.remove_at(ya)

	# Y fuera tambien lo que la nota nueva se TRAGUE. Una blanca puesta encima
	# de dos negras ocupa el sitio de las dos: si no se quitan, quedan notas
	# solapadas, y con dos notas pisandose deja de estar definido a cual
	# golpeo el jugador.
	var fin: float = enc + _dur_figura(_figura, enc, _puntillo)
	for k in range(_notas.size() - 1, -1, -1):
		if _voz_de(_notas[k]) != _voz: continue
		var tk: float = float(_notas[k]["t"])
		if tk > enc + 0.001 and tk < fin - 0.001:
			_notas.remove_at(k)

	_notas.append({"t": enc, "fig": _figura, "p": _puntillo, "v": _voz})
	_notas.sort_custom(func(a, b): return float(a["t"]) < float(b["t"]))
	_pintadas += 1


## Que va a pasar si pulsas aqui. Lo usan el raton Y la vista previa, para que
## lo que ves sea exactamente lo que va a ocurrir.
##
## ⚠️ Antes esto se decidia en dos sitios distintos y la previa mentia: te
## enseñaba un fantasma de "aqui va a ir" cuando el clic iba a BORRAR.
enum Accion { PONER, SUSTITUIR, BORRAR, IGUAL }

func _accion_en(t: float) -> int:
	var enc := _encajar_figura(t)
	var i := _nota_en(t)

	# Sobre la CABEZA de una nota: la acción es sobre ESA nota.
	if i >= 0 and absf(float(_notas[i]["t"]) - enc) < 0.001:
		if int(_notas[i]["fig"]) == _figura 				and bool(_notas[i].get("p", false)) == _puntillo:
			return Accion.BORRAR
		return Accion.SUSTITUIR

	# Si no, se pone en la casilla que toque — que si estabas sobre la cola de
	# una nota es la de DESPUÉS de ella, no encima.
	var destino := _casilla_para_poner(t)
	var j := _nota_que_arranca_en(destino)
	if j >= 0:
		return Accion.IGUAL if _es_la_misma(_notas[j], destino) else Accion.SUSTITUIR
	return Accion.SUSTITUIR if _nota_en(destino) >= 0 else Accion.PONER


## Misma casilla, misma figura y mismo puntillo. Si algo cambia, se sustituye.
func _es_la_misma(n: Dictionary, enc: float) -> bool:
	return absf(float(n["t"]) - enc) < 0.001 		and int(n["fig"]) == _figura 		and bool(n.get("p", false)) == _puntillo


## Un clic suelto: una nota, un paso de deshacer.
func _poner_nota(t: float) -> void:
	if _tramos.is_empty(): return
	var enc := _casilla_para_poner(t)
	_recordar("poner una %s" % _nombre_figura_actual(), false)
	_pintadas = 0
	_escribir_nota(enc)
	_sincronizar_melodia()
	_guardar(true)
	_avisar("%s en %s" % [_nombre_figura_actual(), _reloj(enc)])


## Empieza un trazo. Apila UN paso de deshacer para todo el arrastre.
##
## Con el boton izquierdo el modo lo decide LO QUE HAY DEBAJO:
##
##   casilla vacia            → pinta
##   nota de OTRA figura      → sustituye  (cambiar de valor es lo normal)
##   nota de la MISMA figura  → borra      (volver a pulsar lo mismo lo apaga)
##
## Y una vez decidido, el trazo entero va en ese modo: arrastras y sigue
## borrando aunque pases por encima de notas distintas. Si no fuera asi,
## quitar una fila de corcheas se cortaria en la primera negra.
func _empezar_trazo(t: float, forzar_borrado: bool) -> void:
	if _tramos.is_empty(): return

	var borrando := forzar_borrado
	if not forzar_borrado:
		var acc := _accion_en(t)
		if acc == Accion.IGUAL:
			_avisar("Ahí ya hay una %s igual" % _nombre_figura_actual())
			return
		borrando = acc == Accion.BORRAR

	_pintando = -1 if borrando else 1
	_pintadas = 0
	_ultima_casilla = -99999.0
	_recordar("borrar notas arrastrando" if borrando else "pintar notas", false)
	_pintar_en(t)


## Escribe o borra en la casilla bajo el raton, si es distinta de la anterior.
func _pintar_en(t: float) -> void:
	if _pintando == 0 or _tramos.is_empty(): return

	if _pintando > 0:
		# Poner SI encaja: para eso estan las figuras.
		var enc := _casilla_para_poner(t)
		if is_equal_approx(enc, _ultima_casilla): return
		_ultima_casilla = enc
		_escribir_nota(enc)
		return

	# Borrar NO encaja. Se quita lo que haya bajo el raton, sea la figura que
	# sea. Si se encajara con la figura activa, con una redonda seleccionada
	# seria imposible borrar una corchea: el encaje te manda a otra casilla.
	var i := _nota_en(t)
	if i < 0: return
	var suya: float = float(_notas[i]["t"])
	if is_equal_approx(suya, _ultima_casilla): return
	_ultima_casilla = suya
	_notas.remove_at(i)
	_pintadas += 1


## Cierra el trazo: un solo guardado al final.
func _terminar_trazo() -> void:
	if _pintando == 0: return
	var borraba := _pintando < 0
	_pintando = 0
	if _pintadas == 0:
		_avisar("Nada que cambiar ahí")
		return
	_sincronizar_melodia()
	_guardar(true)
	_avisar("%d nota%s %s" % [_pintadas, "" if _pintadas == 1 else "s",
		"borrada" + ("" if _pintadas == 1 else "s") if borraba
		else "puesta" + ("" if _pintadas == 1 else "s")])


func _quitar_nota(i: int) -> void:
	if i < 0 or i >= _notas.size(): return
	_recordar("quitar una nota", false)
	_notas.remove_at(i)
	_sincronizar_melodia()
	_guardar(true)
	_avisar("Nota quitada · quedan %d" % _notas.size())


## La lista plana de instantes es lo UNICO que consume el nucleo. Las figuras
## son del editor. Se regenera entera para que no puedan desincronizarse.
## _melodia es el contrato con el nucleo: SOLO la voz principal.
func _sincronizar_melodia() -> void:
	_melodia.clear()
	for n in _notas:
		if _voz_de(n) == 0: _melodia.append(float(n["t"]))
	_melodia.sort()


func _ajustar_tolerancia(d: float) -> void:
	_tolerancia_ms = clampf(_tolerancia_ms + d, 10.0, 300.0)
	_guardar(true)
	var ap := _notas_apretadas()
	if ap > 0:
		_avisar("Techo ±%.0f ms · %d notas lo llevan recortado por ir pegadas" % [
			_tolerancia_ms, ap])
	else:
		_avisar("Techo ±%.0f ms · ninguna nota lo necesita recortar" % _tolerancia_ms)


## El puntillo suma la mitad del valor: negra. = tiempo y medio.
func _alternar_puntillo() -> void:
	_puntillo = not _puntillo
	_avisar("Figura: %s" % _nombre_figura_actual())


func _siguiente_figura() -> void:
	var j: int = FIGURAS.find(_figura)
	_figura = FIGURAS[(j + 1) % FIGURAS.size()]
	_avisar("Figura: %s" % _nombre_figura_actual())


## El hueco mas corto entre dos notas seguidas. Es el que manda: si la
## tolerancia pasa de su mitad, dos zonas de ayuda se solapan y deja de estar
## definido a que nota golpeo el jugador.
func _hueco_minimo() -> float:
	var m := 9999.0
	for v in MAX_VOCES:
		var ant := -1.0
		for n in _notas:
			if _voz_de(n) != v: continue
			if ant >= 0.0: m = minf(m, float(n["t"]) - ant)
			ant = float(n["t"])
	return m


func _tolerancia_maxima_ms() -> float:
	var h := _hueco_minimo()
	return 9999.0 if h >= 9999.0 else h * 1000.0 * 0.5


## Zona de ayuda EFECTIVA de una nota, en segundos.
##
## El valor de configuracion es un TECHO. Cada nota lo recorta a la mitad de su
## hueco con la vecina mas cercana, asi que dos zonas pueden tocarse pero nunca
## solaparse — y siempre esta definido a que nota golpeo el jugador.
##
## Con esto un solo numero sirve para toda la cancion: las negras y corcheas
## usan el techo entero, y los pasajes de fusas se aprietan solos.
func _zona_de(i: int) -> float:
	if i < 0 or i >= _notas.size(): return 0.0
	var techo: float = _tolerancia_ms / 1000.0
	# Los vecinos que cuentan son los de SU MISMA voz: dos lineas melodicas
	# distintas pueden sonar a la vez sin que eso sea un solape.
	# ⚠️ La lista esta ORDENADA, asi que basta con salir andando a los lados
	# hasta topar con la primera nota de la misma voz. Recorrerlas todas
	# costaba 980x980 iteraciones POR FOTOGRAMA y el programa iba a tirones.
	var v := _voz_de(_notas[i])
	var t0: float = float(_notas[i]["t"])
	var hueco := 9999.0

	var k := i - 1
	while k >= 0:
		if _voz_de(_notas[k]) == v:
			hueco = minf(hueco, t0 - float(_notas[k]["t"]))
			break
		k -= 1

	k = i + 1
	while k < _notas.size():
		if _voz_de(_notas[k]) == v:
			hueco = minf(hueco, float(_notas[k]["t"]) - t0)
			break
		k += 1

	return techo if hueco >= 9999.0 else minf(techo, hueco * 0.5)


## Cuantas notas tienen la zona recortada por estar pegadas a su vecina.
func _notas_apretadas() -> int:
	var n := 0
	var techo: float = _tolerancia_ms / 1000.0
	for i in _notas.size():
		if _zona_de(i) < techo - 0.0001: n += 1
	return n


func _y_minimapa() -> float:
	return _y_barra() - 48.0


func _sobre_el_minimapa(y: float) -> bool:
	return y >= _y_minimapa() - 4.0 and y <= _y_minimapa() + ALTO_MINIMAPA + 4.0


## Centra la ventana visible en un punto de la cancion.
func _centrar_vista_en(t: float) -> void:
	var dur := _duracion()
	if dur <= 0.0: return
	var v := _vista()
	var ancho: float = v.y - v.x
	if ancho >= dur: return
	_vista_ini = clampf(t - ancho * 0.5, 0.0, dur - ancho)
	_vista_fin = _vista_ini + ancho


## Instante que corresponde a una x sobre el minimapa, que siempre abarca
## la cancion entera.
func _tiempo_en_x_mini(x: float) -> float:
	var dur := _duracion()
	if dur <= 0.0: return 0.0
	return clampf((x - MARGEN_BARRA) / _ancho_barra(), 0.0, 1.0) * dur


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


## Indice del tramo cuyo borde cae a menos de unos pixeles de una x.
## El primero no cuenta: siempre empieza en 0 y no se mueve.
func _borde_cercano(x: float) -> int:
	var mejor := -1
	var mejor_d := 9999.0
	for i in range(1, _tramos.size()):
		var d: float = absf(_x_de_tiempo(float(_tramos[i]["desde"])) - x)
		if d < mejor_d:
			mejor_d = d
			mejor = i
	return mejor if mejor_d <= 7.0 else -1


## Borde mas cercano al cursor, para las teclas.
func _borde_cercano_al_cursor() -> int:
	var c := _posicion()
	var mejor := -1
	var mejor_d := 9999.0
	for i in range(1, _tramos.size()):
		var d: float = absf(float(_tramos[i]["desde"]) - c)
		if d < mejor_d:
			mejor_d = d
			mejor = i
	return mejor


## Mueve un borde, sin dejar que se cruce con sus vecinos.
func _mover_borde(i: int, nuevo: float) -> void:
	if i <= 0 or i >= _tramos.size(): return
	var minimo: float = float(_tramos[i - 1]["desde"]) + 0.05
	var maximo: float = float(_tramos[i + 1]["desde"]) - 0.05 if i + 1 < _tramos.size() 		else _duracion() - 0.05
	_tramos[i]["desde"] = clampf(nuevo, minimo, maxf(minimo, maximo))
	_ultimo_pulso_sonado = -999999


## Instante del pulso de rejilla mas cercano. Es lo que convierte un arrastre
## aproximado en un limite exacto.
func _pegar_a_rejilla(t: float) -> float:
	if not _ajustar_a_rejilla or _tramos.is_empty(): return t
	var i := _tramo_en(t)
	var paso: float = 60.0 / float(_tramos[i]["bpm"])
	var org: float = _origen_de(i)
	return org + roundf((t - org) / paso) * paso


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


## Mantiene el cursor a la vista mientras suena.
##
## Salta cuando llega al 70% del ancho y lo deja al 25%, asi que siempre tienes
## tres cuartos de ventana de lo que VIENE. Desplazar poco a poco marearia;
## saltar de pagina deja leer.
func _seguir_cursor() -> void:
	if not _seguir or not _repro.playing or _arrastrando_mini: return
	var dur := _duracion()
	if dur <= 0.0: return

	var v := _vista()
	var ancho: float = v.y - v.x
	if ancho >= dur - 0.01: return          # se ve la cancion entera

	var t := _posicion()
	if t < v.x or t > v.x + ancho * 0.70:
		_vista_ini = clampf(t - ancho * 0.25, 0.0, dur - ancho)
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


## ORIGEN REAL de la rejilla de cada tramo.
##
## El borde que colocas dice QUE compas cambia, no exactamente cuando. Si lo
## pusiste 20 ms antes o despues del sitio, la rejilla saltaria ahi y el
## metronomo tropezaria.
##
## Con la rejilla continua, cada tramo arranca en la primera linea de compas
## del tramo anterior que caiga en su borde o despues. Es lo que pasa en la
## musica de verdad: el compas en curso se termina, y el nuevo empieza pegado.
## Asi no hay saltos por construccion, y colocar el borde a ojo basta.
func _recalcular_origenes() -> void:
	_origenes.clear()
	if _tramos.is_empty(): return
	_origenes.append(float(_tramos[0]["desde"]))

	for i in range(1, _tramos.size()):
		var puesto: float = float(_tramos[i]["desde"])
		if not _rejilla_continua:
			_origenes.append(puesto)
			continue
		var compas_ant: float = 60.0 / float(_tramos[i - 1]["bpm"]) 			* float(int(_tramos[i - 1]["compas"]))
		var org_ant: float = _origenes[i - 1]
		var n: float = ceilf((puesto - org_ant) / compas_ant - 0.001)
		_origenes.append(org_ant + maxf(0.0, n) * compas_ant)


## Cuantos compases enteros cabe un tramo. Si NO sale un numero entero, el
## tramo siguiente no puede empezar donde lo pusiste: tiene que irse a la
## siguiente linea de compas de este. Es la causa de casi todo el destempo.
func _compases_del_tramo(i: int) -> float:
	if i < 0 or i >= _tramos.size(): return 0.0
	var fin: float = _duracion()
	if i + 1 < _tramos.size(): fin = float(_tramos[i + 1]["desde"])
	var largo: float = 60.0 / float(_tramos[i]["bpm"]) * float(int(_tramos[i]["compas"]))
	if largo <= 0.0: return 0.0
	return (fin - _origen_de(i)) / largo


## Lo que se desplaza un tramo entre donde lo pusiste y donde arranca de verdad.
func _deriva_de(i: int) -> float:
	if i <= 0 or i >= _tramos.size(): return 0.0
	return _origen_de(i) - float(_tramos[i]["desde"])


func _origen_de(i: int) -> float:
	if i < 0 or i >= _origenes.size():
		return float(_tramos[i]["desde"]) if i < _tramos.size() else 0.0
	return _origenes[i]


## Numero de pulso dentro de SU tramo, contado desde el origen real.
func _pulso_local(t: float) -> int:
	var i := _tramo_en(t)
	var dur: float = 60.0 / float(_tramos[i]["bpm"])
	return int(floorf((t - _origen_de(i)) / dur))


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

	_recordar("partir el tramo", false)
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

	_recordar("aplicar el rango", false)
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

	_compactar()
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Aplicado: %s → %s · %d/4 · %.1f BPM" % [
		_reloj(a), _reloj(b), _sel_compas, _sel_bpm])
	_limpiar_seleccion()


## Junta tramos contiguos que digan lo mismo Y ADEMAS empiecen justo en una
## linea de compas del anterior.
##
## Esa segunda condicion es la importante. Dos tramos con el mismo BPM y el
## mismo compas NO son siempre equivalentes a uno solo, porque cada tramo
## REINICIA la cuenta de compases: si el corte cae a mitad de compas, esta
## recolocando el "uno" a proposito y hay que respetarlo.
##
## Pero si cae justo en una linea de compas, el corte no hace nada y solo
## estorba. Eso es lo que pasa al borrar un tramo: el anterior se come su
## hueco y queda un corte fantasma que parece inofensivo y desplaza el
## metronomo.
func _compactar() -> void:
	var i := 1
	while i < _tramos.size():
		var ant: Dictionary = _tramos[i - 1]
		var act: Dictionary = _tramos[i]
		var mismo := is_equal_approx(float(act["bpm"]), float(ant["bpm"])) 			and int(act["compas"]) == int(ant["compas"])
		if mismo:
			var paso: float = 60.0 / float(ant["bpm"]) * float(int(ant["compas"]))
			var delta: float = float(act["desde"]) - float(ant["desde"])
			var resto: float = fposmod(delta, paso)
			if resto < 0.005 or paso - resto < 0.005:
				_tramos.remove_at(i)
				continue
		i += 1


## Quita el tramo bajo el cursor. El primero no se puede quitar.
func _quitar_tramo() -> void:
	var i := _tramo_en(_posicion())
	if i == 0:
		_avisar("El primer tramo no se puede quitar")
		return
	_recordar("quitar el tramo %d" % (i + 1), false)
	var antes := _tramos.size()
	_tramos.remove_at(i)
	_compactar()
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	var limpiados := antes - 1 - _tramos.size()
	if limpiados > 0:
		_avisar("Tramo quitado · %d corte%s sobrante%s limpiado%s · quedan %d" % [
			limpiados, "" if limpiados == 1 else "s", "" if limpiados == 1 else "s",
			"" if limpiados == 1 else "s", _tramos.size()])
	else:
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
	_recalcular_origenes()
	_seguir_cursor()
	_guardado_hace += delta
	_destello_guardado = maxf(0.0, _destello_guardado - delta * 1.6)
	_juicio_ttl = maxf(0.0, _juicio_ttl - delta)
	_destello_golpe = maxf(0.0, _destello_golpe - delta * 4.0)
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

	# El panel no necesita 60 reconstrucciones por segundo: cada una reparsea
	# todo el BBCode. A 12 por segundo no se nota y cuesta cinco veces menos.
	_etiqueta.offset_bottom = -296.0 - _extra_ensayo()
	_texto_ttl -= delta
	if _texto_ttl <= 0.0:
		_texto_ttl = 1.0 / 12.0
		_actualizar_texto()
	queue_redraw()


# ---------------------------------------------------------------- entrada

func _gui_input(e: InputEvent) -> void:
	# Clicar en la barra salta a ese punto. Imprescindible para configurar una
	# cancion entera sin escucharla del tiron.
	if e is InputEventMouseButton:
		var b := e as InputEventMouseButton

		# Los botones de la columna se atienden antes que nada.
		if b.pressed and b.button_index == MOUSE_BUTTON_LEFT:
			if _pulsar_boton(b.position): return

		# En ensayo se golpea EN EL PAD. Antes valia clicar en cualquier sitio,
		# que es lo mismo que no decir donde.
		if _ensayo and b.pressed and b.button_index == MOUSE_BUTTON_LEFT:
			if _sobre_el_pad(b.position.y):
				_golpear()
				return

		# La barra de desplazamiento se atiende antes que nada.
		if _sobre_el_minimapa(b.position.y):
			if b.button_index == MOUSE_BUTTON_LEFT:
				_arrastrando_mini = b.pressed
				if b.pressed: _centrar_vista_en(_tiempo_en_x_mini(b.position.x))
			return

		# La linea de figuras, antes que la barra de tramos.
		# Soltar cierra el trazo aunque el raton se haya ido de la linea.
		if _pintando != 0 and not b.pressed:
			_terminar_trazo()
			return

		# Soltar cierra el arrastre de la seleccion.
		if _moviendo_sel and not b.pressed:
			_moviendo_sel = false
			_sincronizar_melodia()
			_guardar(true)
			_avisar("Selección movida a %s" % _reloj(_sel_ini()))
			return

		if _sobre_las_notas(b.position.y):
			# MAYUS+clic: ancla y estira la seleccion.
			if b.pressed and b.button_index == MOUSE_BUTTON_LEFT and b.shift_pressed:
				_marcar_rango_notas(_tiempo_en_x(b.position.x))
				return

			# Clic sobre una nota YA SELECCIONADA: mueve el grupo entero.
			if b.pressed and b.button_index == MOUSE_BUTTON_LEFT and _hay_sel_notas():
				var im := _nota_en(_tiempo_en_x(b.position.x))
				if im >= 0 and _nota_seleccionada(im):
					_recordar("mover la selección de notas", false)
					_moviendo_sel = true
					_mov_agarre = _tiempo_en_x(b.position.x)
					_mov_base = _sel_ini()
					_avisar("Moviendo %d notas" % _indices_seleccionados().size())
					return
				# Clic fuera de la seleccion: se suelta y se sigue como siempre.
				_limpiar_sel_notas()

			if b.pressed and b.button_index == MOUSE_BUTTON_LEFT:
				# false = que decida lo que haya debajo
				_empezar_trazo(_tiempo_en_x(b.position.x), false)
			elif b.pressed and b.button_index == MOUSE_BUTTON_RIGHT:
				# true = borrar siempre, caiga lo que caiga
				_empezar_trazo(_tiempo_en_x(b.position.x), true)
			return

		if not _sobre_la_barra(b.position.y): return

		if b.button_index == MOUSE_BUTTON_WHEEL_UP and b.pressed:
			_zoom(0.7, _tiempo_en_x(b.position.x)); return
		elif b.button_index == MOUSE_BUTTON_WHEEL_DOWN and b.pressed:
			_zoom(1.0 / 0.7, _tiempo_en_x(b.position.x)); return

		if b.button_index == MOUSE_BUTTON_LEFT:
			if b.pressed and b.shift_pressed:
				_marcar_tramo(_tramo_en(_tiempo_en_x(b.position.x)))
				return

			if b.pressed:
				# Clicar DENTRO del bloque marcado es agarrarlo. Va antes de
				# limpiar la marca: si no, el propio clic la borraria.
				var it := _tramo_en(_tiempo_en_x(b.position.x))
				if _marcados.has(it):
					_recordar("mover el bloque", false)
					_arrastrando_bloque = true
					_bloque_agarre = _tiempo_en_x(b.position.x)
					_bloque_ini = float(_tramos[_bloque().x]["desde"])
					var n := _bloque().y - _bloque().x + 1
					_avisar("Moviendo %d tramo%s juntos  ·  suelta para fijar" % [
						n, "" if n == 1 else "s"])
					return

				_marcados.clear()
				# Si el clic cae sobre un borde, se agarra el borde en vez de
				# empezar una seleccion nueva.
				var ib := _borde_cercano(b.position.x)
				if ib >= 0:
					_recordar("arrastrar el borde del tramo %d" % (ib + 1), false)
					_arrastrando_borde = ib
					_limpiar_seleccion()
					_avisar("Moviendo el borde del tramo %d" % (ib + 1))
					return
				_arrastrando = true
				_sel_desde = _pegar_a_rejilla(_tiempo_en_x(b.position.x))
				_sel_hasta = _sel_desde
				_saltar_a(_sel_desde)
				# La seleccion empieza con lo que ya hay ahi
				_sel_bpm = _bpm_en(_sel_desde)
				_sel_compas = _compas_en(_sel_desde)
			else:
				if _arrastrando_bloque:
					_arrastrando_bloque = false
					# NO se compacta: compactar puede fusionar tramos, y eso
					# correria los indices de _marcados dejandolos apuntando a
					# otro sitio. Mover no crea cortes sobrantes, asi que no
					# hay nada que limpiar.
					_guardar(true)
					_avisar("Bloque fijado en %s" % _reloj(float(_tramos[_bloque().x]["desde"])))
					return
				if _arrastrando_borde >= 0:
					_arrastrando_borde = -1
					_compactar()
					_guardar(true)
					_avisar("Borde ajustado")
					return
				_arrastrando = false
				_arrastrando_mini = false
				if absf(_sel_hasta - _sel_desde) < 0.25:
					_limpiar_seleccion()      # fue un clic, no un arrastre
				else:
					_avisar("Rango %s → %s · ajusta y pulsa ENTER" % [
						_reloj(minf(_sel_desde, _sel_hasta)),
						_reloj(maxf(_sel_desde, _sel_hasta))])

		elif b.button_index == MOUSE_BUTTON_RIGHT and b.pressed:
			if _arrastrando_bloque:
				_arrastrando_bloque = false
				_deshacer()
				_avisar("Movimiento del bloque cancelado")
			elif _pegando:
				_pegando = false
				_avisar("Pegado cancelado")
			elif not _portapapeles.is_empty():
				_marcados.clear()
				_portapapeles.clear()
				_avisar("Patrón descartado")
			else:
				_limpiar_seleccion()
				_avisar("Selección quitada")

	elif e is InputEventMouseMotion and _moviendo_sel:
		var ms := e as InputEventMouseMotion
		var objetivo := _encajar_figura(_mov_base + (_tiempo_en_x(ms.position.x) - _mov_agarre))
		_mover_sel_notas(objetivo - _sel_ini())

	elif e is InputEventMouseMotion and _pintando != 0:
		# Solo importa la X: una vez empezado el trazo, salirse de la linea por
		# arriba o por abajo no deberia cortarlo.
		_pintar_en(_tiempo_en_x((e as InputEventMouseMotion).position.x))

	elif e is InputEventMouseMotion and _arrastrando_bloque:
		var mv := e as InputEventMouseMotion
		# En relativo al punto de agarre, o el bloque saltaria bajo el cursor.
		var objetivo := _bloque_ini + (_tiempo_en_x(mv.position.x) - _bloque_agarre)
		if _ajustar_a_rejilla:
			objetivo = _pegar_a_rejilla(objetivo)
		var d := _delta_posible(objetivo - float(_tramos[_bloque().x]["desde"]))
		if absf(d) > 0.0005:
			_mover_bloque(d)

	elif e is InputEventMouseMotion and _arrastrando_borde >= 0:
		var mb := e as InputEventMouseMotion
		_mover_borde(_arrastrando_borde, _pegar_a_rejilla(_tiempo_en_x(mb.position.x)))

	elif e is InputEventMouseMotion and _arrastrando_mini:
		var mm := e as InputEventMouseMotion
		_centrar_vista_en(_tiempo_en_x_mini(mm.position.x))

	elif e is InputEventMouseMotion and _arrastrando:
		var m := e as InputEventMouseMotion
		_sel_hasta = _pegar_a_rejilla(_tiempo_en_x(m.position.x))
		_saltar_a(_sel_hasta)


## MAYUS+clic mete o saca un tramo del patron.
func _marcar_tramo(i: int) -> void:
	if i < 0 or i >= _tramos.size(): return
	if _marcados.has(i):
		_marcados.erase(i)
	else:
		_marcados.append(i)
	_marcados.sort()
	_copiar_marcados()


## Convierte los tramos marcados en un patron de duraciones relativas.
func _copiar_marcados() -> void:
	_portapapeles.clear()
	if _marcados.is_empty():
		_avisar("Nada marcado")
		return

	for i in _marcados:
		var hasta_i: float = _duracion()
		if i + 1 < _tramos.size(): hasta_i = float(_tramos[i + 1]["desde"])
		_portapapeles.append({
			"dur": maxf(0.05, hasta_i - float(_tramos[i]["desde"])),
			"bpm": float(_tramos[i]["bpm"]),
			"compas": int(_tramos[i]["compas"]),
		})

	_avisar("Patrón copiado: %s  ·  CTRL+V para pegarlo en el cursor" % _resumen_patron())


## "4/4 → 2/4 → 4/4", para poder leer el patron de un vistazo.
func _resumen_patron() -> String:
	var partes: Array[String] = []
	for x in _portapapeles:
		partes.append("%d/4" % int(x["compas"]))
	return " → ".join(partes)


func _duracion_patron() -> float:
	var total := 0.0
	for x in _portapapeles:
		total += float(x["dur"])
	return total


## Primer y ultimo tramo del bloque marcado.
##
## Si marcaste el 3 y el 5, el bloque es 3..5: el 4 esta fisicamente en medio y
## no puede quedarse quieto mientras los otros dos se mueven.
func _bloque() -> Vector2i:
	if _marcados.is_empty(): return Vector2i(-1, -1)
	return Vector2i(_marcados[0], _marcados[-1])


## Hasta donde llega el bloque: el borde que lo cierra, o el final.
func _fin_del_bloque() -> float:
	var b := _bloque()
	if b.x < 0: return 0.0
	return float(_tramos[b.y + 1]["desde"]) if b.y + 1 < _tramos.size() else _duracion()


## Recorta el desplazamiento a lo que de verdad cabe, y devuelve lo que se va
## a mover.
##
## RECORTA en vez de rechazar: si se rechazara, arrastrando rapido el bloque se
## quedaria plantado antes del tope en lugar de pegarse a el. Y con el ajuste a
## rejilla activado el salto minimo es un tiempo entero (0,35 s a 170 BPM), asi
## que rechazar dejaba el bloque inmovil siempre que le quedara menos de eso.
##
## El tope es pegarse al vecino, SIN hueco minimo: un tramo de 60 ms delante no
## puede impedir que el bloque llegue al principio de la cancion.
func _delta_posible(delta: float) -> float:
	var b := _bloque()
	if b.x < 0: return 0.0
	if b.x == 0: return 0.0       # el tramo 0 siempre empieza en 0.0

	# Hacia atras: hasta pegarse al tramo de delante.
	var minimo: float = float(_tramos[b.x - 1]["desde"]) - float(_tramos[b.x]["desde"])

	# Hacia delante: hasta pegarse a lo que venga detras.
	var maximo: float
	var cierre := b.y + 1
	if cierre < _tramos.size():
		var tope: float = _duracion()
		if cierre + 1 < _tramos.size():
			tope = float(_tramos[cierre + 1]["desde"])
		maximo = tope - float(_tramos[cierre]["desde"])
	else:
		maximo = _duracion() - _fin_del_bloque()

	return clampf(delta, minf(0.0, minimo), maxf(0.0, maximo))


## Desliza el bloque entero conservando sus duraciones.
##
## Se mueve tambien el borde que lo CIERRA, que es lo que hace que el bloque
## conserve su largo: lo de delante se alarga y lo de detras se acorta. El
## resto de la cancion no se entera.
func _mover_bloque(delta: float) -> void:
	var b := _bloque()
	var ultimo: int = mini(b.y + 1, _tramos.size() - 1)
	for i in range(b.x, ultimo + 1):
		_tramos[i]["desde"] = float(_tramos[i]["desde"]) + delta


## CTRL+V prepara el pegado en el cursor. No escribe nada hasta ENTER.
func _preparar_pegado() -> void:
	if _portapapeles.is_empty():
		_avisar("No hay patrón copiado · marca tramos con MAYÚS+clic")
		return
	_pegando = true
	_pegar_en = _pegar_a_rejilla(_posicion())
	_limpiar_seleccion()
	if _pegar_en + _duracion_patron() > _duracion():
		_avisar("Pegando en %s · OJO: el patrón se sale del final, se cortará" % _reloj(_pegar_en))
	else:
		_avisar("Pegando en %s  ·  ENTER confirma, clic derecho cancela" % _reloj(_pegar_en))


## Escribe el patron copiado a partir de _pegar_en.
func _confirmar_pegado() -> void:
	if not _pegando or _portapapeles.is_empty(): return

	_recordar("pegar el patrón %s" % _resumen_patron(), false)
	var fin := _pegar_en + _duracion_patron()
	var bpm_despues := _bpm_en(fin)
	var compas_despues := _compas_en(fin)

	# Fuera lo que hubiera dentro del hueco que ocupa el patron
	for i in range(_tramos.size() - 1, 0, -1):
		var d := float(_tramos[i]["desde"])
		if d > _pegar_en - 0.02 and d < fin + 0.02:
			_tramos.remove_at(i)

	var cursor_t := _pegar_en
	var escritos := 0
	for x in _portapapeles:
		if cursor_t >= _duracion() - 0.05: break   # el patron se sale: se corta
		_insertar_tramo(cursor_t, float(x["bpm"]), int(x["compas"]))
		escritos += 1
		cursor_t += float(x["dur"])

	if fin < _duracion() - 0.05:
		_insertar_tramo(fin, bpm_despues, compas_despues)

	_compactar()
	_pegando = false
	# Los indices marcados apuntaban a la lista de ANTES; el patron sigue
	# valiendo, para poder pegarlo otra vez en el siguiente estribillo.
	_marcados.clear()
	_ultimo_pulso_sonado = -999999
	_guardar(true)
	_avisar("Patrón pegado: %d tramo%s desde %s" % [
		escritos, "" if escritos == 1 else "s", _reloj(_pegar_en)])


## Pega el borde del tramo del cursor a la linea de compas MAS CERCANA del
## tramo de antes.
##
## Despues de esto la deriva queda en CERO por construccion: donde ves el
## borde es donde suena el primer golpe del compas nuevo. Es el arreglo
## directo cuando el panel avisa de que un tramo arranca tarde.
func _cuadrar_borde() -> void:
	if _tramos.is_empty(): return
	var i := _tramo_en(_posicion())
	if i <= 0:
		_avisar("El primer tramo empieza en 0: no hay borde que cuadrar")
		return

	var compas_ant: float = 60.0 / float(_tramos[i - 1]["bpm"]) 		* float(int(_tramos[i - 1]["compas"]))
	var org_ant: float = _origen_de(i - 1)
	var antes: float = float(_tramos[i]["desde"])

	# Al menos un compas: el borde no puede comerse el tramo de delante.
	#
	# El +0.002 desempata hacia ADELANTE. Un borde que cae justo a mitad entre
	# dos lineas de compas (4.4999) es una moneda al aire, y redondear hacia
	# atras moveria el borde al lado contrario de donde YA suena: la rejilla
	# continua usa ceil, asi que el golpe que oyes es el de la linea de
	# adelante. Ante la duda, que lo que ves se mueva hacia lo que oyes.
	var bruto: float = (antes - org_ant) / compas_ant
	var n: float = maxf(1.0, floorf(bruto + 0.502))

	_recordar("cuadrar el borde del tramo %d" % (i + 1), false)
	_mover_borde(i, org_ant + n * compas_ant)
	_recalcular_origenes()
	_guardar(true)

	var ahora: float = float(_tramos[i]["desde"])
	_avisar("Borde %d cuadrado · %.3f → %.3f s  (%+.0f ms)  ·  deriva %+.0f ms" % [
		i + 1, antes, ahora, (ahora - antes) * 1000.0, _deriva_de(i) * 1000.0])


## Empuja un borde. Si hay seleccion, el suyo; si no, el del mapa de tempo
## mas cercano al cursor, que es lo que permite afinar un tramo YA aplicado
## sin tener que borrarlo y rehacerlo.
##
## fino = milesimas (10 ms), para cuando te pasaste por poco en la entrada.
## grueso = un tiempo entero de rejilla.
func _empujar_borde(dir: int, fino: bool = false) -> void:
	# Con tramos marcados, , y . mueven el BLOQUE, igual que C y T editan la
	# seleccion en vez del tramo del cursor.
	if not _marcados.is_empty():
		var b0 := _bloque()
		var ref: float = float(_tramos[maxi(0, b0.x - 1)]["bpm"])
		var paso0: float = 0.010 if fino else 60.0 / ref
		var d0: float = _delta_posible(paso0 * dir)
		if absf(d0) < 0.0005:
			if b0.x == 0:
				_avisar("El bloque incluye el primer tramo, que siempre empieza en 0")
			else:
				_avisar("El bloque ya está pegado a ese lado")
			return
		_recordar("mover el bloque")
		_mover_bloque(d0)
		_guardar(true)
		_avisar("Bloque → %s   (%s)" % [
			_reloj(float(_tramos[b0.x]["desde"])), "±10 ms" if fino else "±1 tiempo"])
		return

	if _hay_seleccion():
		var paso: float = 0.010 if fino else 60.0 / _bpm_en(_sel_desde)
		var c := _posicion()
		if absf(_sel_desde - c) <= absf(_sel_hasta - c):
			_sel_desde += paso * dir
			_avisar("Inicio de la selección → %s" % _reloj(_sel_desde))
		else:
			_sel_hasta += paso * dir
			_avisar("Final de la selección → %s" % _reloj(_sel_hasta))
		return

	var i := _borde_cercano_al_cursor()
	if i < 0:
		_avisar("No hay ningún borde que mover")
		return
	_recordar("empujar el borde del tramo %d" % (i + 1))
	var paso2: float = 0.010 if fino else 60.0 / float(_tramos[i - 1]["bpm"])
	_mover_borde(i, float(_tramos[i]["desde"]) + paso2 * dir)
	_guardar(true)
	_avisar("Borde del tramo %d → %.3f s   (%s)" % [
		i + 1, float(_tramos[i]["desde"]), "±10 ms" if fino else "±1 tiempo"])


func _limpiar_seleccion() -> void:
	_sel_desde = -1.0
	_sel_hasta = -1.0
	_arrastrando = false


func _hay_seleccion() -> bool:
	return _sel_desde >= 0.0 and absf(_sel_hasta - _sel_desde) >= 0.25


func _sobre_la_barra(y: float) -> bool:
	return y >= _y_barra() - 12.0 and y <= _y_barra() + ALTO_BARRA + 10.0


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
		# --- atajos con CTRL ---
		# Van PRIMERO a proposito: en un match gana el primer patron que
		# encaja, y mas abajo estan KEY_Z, KEY_C y KEY_V sin guarda, que
		# los taparian.
		# CTRL+MAYUS+Z va ANTES que CTRL+Z: es el otro rehacer de toda la vida,
		# y si no lo atajas aqui acaba deshaciendo, que es lo contrario.
		KEY_Z when e.ctrl_pressed and e.shift_pressed: _rehacer()
		KEY_Z when e.ctrl_pressed: _deshacer()
		# Con notas seleccionadas, CTRL+C/V van a las NOTAS; si no, al patron
		# de compases, que es lo que hacian antes.
		KEY_C when e.ctrl_pressed and _hay_sel_notas(): _copiar_sel_notas()
		KEY_V when e.ctrl_pressed and not _portapapeles_notas.is_empty(): _pegar_notas()
		KEY_Y when e.ctrl_pressed: _rehacer()
		KEY_C when e.ctrl_pressed: _copiar_marcados()
		KEY_V when e.ctrl_pressed: _preparar_pegado()

		# --- lo que se graba ---
		KEY_SPACE:
			# Ensayando, ESPACIO es un BA. Con el raton se golpea peor: el
			# dedo en la barra va mas fino y no hay que apuntar a ningun sitio.
			if _ensayo: _golpear()
			else: _marcar_melodia()
		KEY_BACKSPACE: _borrar_ultima()
		KEY_DELETE:
			if _hay_sel_notas(): _borrar_sel_notas()
			else: _vaciar_melodia()

		# --- precision ---
		KEY_N:
			_ajustar_a_rejilla = not _ajustar_a_rejilla
			_avisar("Ajuste a rejilla " + ("ON" if _ajustar_a_rejilla else "OFF"))
		KEY_L:
			_rejilla_continua = not _rejilla_continua
			_avisar("Rejilla continua " + ("ON" if _rejilla_continua else "OFF"))
		KEY_COMMA: _empujar_borde(-1, not e.shift_pressed)
		KEY_PERIOD: _empujar_borde(1, not e.shift_pressed)
		KEY_Z: _zoom(0.7, _posicion())
		KEY_A: _zoom(1.0 / 0.7, _posicion())
		KEY_V: _ver_todo()

		# --- mapa de tempo ---
		KEY_ENTER, KEY_KP_ENTER:
			if _pegando: _confirmar_pegado()
			else: _aplicar_seleccion()
		KEY_F: _siguiente_figura()
		KEY_D: _alternar_puntillo()
		KEY_O: _ajustar_tolerancia(-5.0)
		KEY_P when e.shift_pressed: _ajustar_tolerancia(5.0)
		KEY_TAB: _cambiar_voz()
		KEY_E:
			_seguir = not _seguir
			_avisar("La vista %s al cursor" % ("sigue" if _seguir else "ya NO sigue"))
		KEY_R:
			_ensayo = not _ensayo
			_reiniciar_ensayo()
			_avisar("Ensayo " + ("en marcha · golpea en el pad de abajo" if _ensayo else "apagado"))
		KEY_H:
			_ayuda_visible = not _ayuda_visible
			_actualizar_texto()
		KEY_B: _cuadrar_borde()
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
	_recordar("BPM del tramo")
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
	_recordar("compás del tramo")
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
		_recordar("BPM a golpes")
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
	_recordar("marca de melodía", false)
	_melodia.append(_posicion() + _offset_calibracion)
	_melodia.sort()
	_destello_marca = 1.0
	_guardar(true)


func _borrar_ultima() -> void:
	if _melodia.is_empty():
		_avisar("No hay marcas que borrar")
		return
	_recordar("borrar la última nota", false)
	_melodia.remove_at(_melodia.size() - 1)
	_guardar(true)
	_avisar("Borrada · quedan %d" % _melodia.size())


func _vaciar_melodia() -> void:
	if _melodia.is_empty():
		_avisar("La melodía ya estaba vacía")
		return
	_recordar("vaciar la melodía (%d notas)" % _melodia.size(), false)
	_melodia.clear()
	_guardar(true)
	_avisar("Melodía vaciada")


# ------------------------------------------------------------- deshacer

## Copia honda: los tramos son diccionarios y duplicar la lista a secas
## dejaria las mismas referencias dentro.
func _copia_tramos() -> Array[Dictionary]:
	var copia: Array[Dictionary] = []
	for t in _tramos:
		copia.append(t.duplicate())
	return copia


## ⚠️ Las FIGURAS tambien son trabajo, y hasta ahora no se guardaban en la pila.
## Solo se guardaba _melodia —la lista plana de instantes—, asi que deshacer
## restauraba los instantes pero dejaba las figuras como estaban: CTRL+Z no
## hacia nada visible en el carril de notas.
func _copia_notas() -> Array[Dictionary]:
	var copia: Array[Dictionary] = []
	for n in _notas:
		copia.append(n.duplicate())
	return copia


## Apila el estado de AHORA, antes de cambiarlo.
##
## unir = los cambios seguidos del mismo tipo cuentan como uno solo. Subir el
## BPM veinte veces seguidas es UN paso de deshacer, no veinte; pero cada nota
## de melodia es suya, porque se marcan en rafaga y querrias quitarlas una a una.
func _recordar(motivo: String, unir: bool = true) -> void:
	# Un cambio nuevo corta la rama de rehacer, como en cualquier editor.
	_rehacer_pila.clear()
	var ahora := float(Time.get_ticks_msec()) / 1000.0
	if unir and motivo == _ultimo_motivo and ahora - _ultimo_recuerdo < 1.5:
		_ultimo_recuerdo = ahora
		return

	_ultimo_motivo = motivo if unir else ""
	_ultimo_recuerdo = ahora
	_pila.append({
		"motivo": motivo,
		"tramos": _copia_tramos(),
		"melodia": _melodia.duplicate(),
		"notas": _copia_notas(),
	})
	if _pila.size() > MAX_DESHACER:
		_pila.remove_at(0)


## Deja puesta una foto y manda el estado de AHORA a la otra pila.
##
## Es lo unico que hace falta para que deshacer y rehacer sean simetricos: cada
## uno saca de su pila y empuja en la del otro. No pasa por _recordar(), que
## borraria justo la pila a la que queremos volver.
func _canjear(foto: Dictionary, a_la_otra: Array[Dictionary]) -> void:
	a_la_otra.append({
		"motivo": foto["motivo"],
		"tramos": _copia_tramos(),
		"melodia": _melodia.duplicate(),
		"notas": _copia_notas(),
	})
	if a_la_otra.size() > MAX_DESHACER:
		a_la_otra.remove_at(0)

	var tr: Array[Dictionary] = foto["tramos"]
	var me: Array[float] = foto["melodia"]
	var contaba := _tramos.size()
	_tramos = tr
	_melodia = me

	# Las fotos viejas de esta sesion pueden no traer figuras.
	if foto.has("notas"):
		var no: Array[Dictionary] = foto["notas"]
		_notas = no

	# Lo que estuviera a medias apuntaba al mapa de antes.
	_ultimo_motivo = ""
	_limpiar_seleccion()
	_pegando = false
	_arrastrando_borde = -1
	_arrastrando_bloque = false
	_pintando = 0
	_moviendo_sel = false
	_limpiar_sel_notas()
	_ultimo_pulso_sonado = -999999

	# Los indices marcados solo siguen valiendo si no cambio el numero de
	# tramos. Si cambio, apuntarian a otros: mejor soltarlos.
	if _tramos.size() != contaba:
		_marcados.clear()

	_guardar(true)


func _deshacer() -> void:
	if _pila.is_empty():
		_avisar("Nada que deshacer")
		return
	var e: Dictionary = _pila.pop_back()
	_canjear(e, _rehacer_pila)
	_avisar("Deshecho: %s  ·  quedan %d  ·  CTRL+Y lo devuelve" % [e["motivo"], _pila.size()])


func _rehacer() -> void:
	if _rehacer_pila.is_empty():
		_avisar("Nada que rehacer")
		return
	var e: Dictionary = _rehacer_pila.pop_back()
	_canjear(e, _pila)
	_avisar("Rehecho: %s  ·  quedan %d" % [e["motivo"], _rehacer_pila.size()])


## "hace 3 s", "hace 2 min". Un reloj absoluto no dice si acaba de pasar.
func _hace_cuanto(seg: float) -> String:
	if seg < 2.0: return "un instante"
	if seg < 60.0: return "%d s" % int(seg)
	if seg < 3600.0: return "%d min" % int(seg / 60.0)
	return "%d h" % int(seg / 3600.0)


func _avisar(txt: String) -> void:
	_mensaje = txt
	_mensaje_ttl = 2.5
	_texto_ttl = 0.0        # que salga ya, sin esperar al siguiente refresco


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

	# Empujar un bloque contra su vecino deja a ese vecino sin largo. EN
	# MEMORIA se conserva a proposito: asi arrastrar de vuelta lo recupera, en
	# vez de ser un viaje de ida. Pero al archivo no va: alli solo sobra.
	var rejilla := []
	for i in _tramos.size():
		var x: Dictionary = _tramos[i]
		if i + 1 < _tramos.size():
			if float(_tramos[i + 1]["desde"]) - float(x["desde"]) < 0.02:
				continue
		rejilla.append({
			"desde": snappedf(float(x["desde"]), 0.0001),
			"bpm": snappedf(float(x["bpm"]), 0.01),
			"compas": int(x["compas"]),
		})

	# El primero tiene que arrancar en 0 pase lo que pase.
	if not rejilla.is_empty():
		rejilla[0]["desde"] = 0.0

	# "melodia" es el CONTRATO con el nucleo y sigue siendo la voz principal.
	# La de fondo va en su propia clave: asi cualquier lector antiguo —y los
	# tests del nucleo— siguen viendo exactamente lo que veian.
	var notas := []
	var notas2 := []
	for n in _notas:
		var v := snappedf(float(n["t"]), 0.0001)
		if _voz_de(n) == 0: notas.append(v)
		else: notas2.append(v)

	# Las FIGURAS son dato del editor; los INSTANTES son el contrato con el
	# nucleo. Se escriben los dos: asi cualquier lector antiguo —o cualquier
	# otro editor— sigue funcionando solo con "melodia".
	var figuras := []
	for n in _notas:
		var f := {
			"t": snappedf(float(n["t"]), 0.0001),
			"fig": int(n["fig"]),
		}
		if bool(n.get("p", false)): f["p"] = true   # solo si lo tiene
		if _voz_de(n) != 0: f["v"] = _voz_de(n)
		figuras.append(f)

	var datos := {
		"id": _id,
		"archivo": _archivos[_indice_archivo] if not _archivos.is_empty() else "",
		"rejilla": rejilla,
		"secciones": _secciones,
		"melodia": notas,
		"melodiaFondo": notas2,
		"figuras": figuras,
		"toleranciaMs": snappedf(_tolerancia_ms, 0.1),
	}

	# ¿Ha escrito alguien mas desde que cargamos?
	var ruta := _ruta_tema()
	if _tema_mtime > 0 and FileAccess.file_exists(ruta):
		var ahora_mt := FileAccess.get_modified_time(ruta)
		if ahora_mt > _tema_mtime:
			var aparte := "%s/%s - conflicto %s.json" % [
				ProjectSettings.globalize_path(CARPETA_TEMAS), _id,
				Time.get_time_string_from_system().replace(":", "")]
			var fc := FileAccess.open(aparte, FileAccess.WRITE)
			if fc != null:
				fc.store_string(JSON.stringify(datos, "  "))
				fc.close()
			_guardado_error = "otra ventana escribió este tema · lo tuyo va a «%s»" % aparte.get_file()
			_avisar("⚠️ CONFLICTO: otra ventana tocó el archivo · guardado aparte como %s" % aparte.get_file())
			push_warning("CONFLICTO de guardado: %s" % aparte)
			return

	var f := FileAccess.open(ruta, FileAccess.WRITE)
	if f == null:
		# ⚠️ SIEMPRE se avisa, aunque el guardado sea silencioso. Un
		# autoguardado que falla en silencio es la forma exacta de perder una
		# tarde entera sin enterarte.
		_guardado_error = "no se pudo escribir en %s" % _ruta_tema()
		_avisar("⚠️ NO SE PUDO GUARDAR · %s" % _ruta_tema())
		push_error("NO SE PUDO GUARDAR: %s" % _ruta_tema())
		return

	f.store_string(JSON.stringify(datos, "  "))
	f.close()
	_tema_mtime = FileAccess.get_modified_time(ruta)

	_guardado_error = ""
	_guardado_hora = Time.get_time_string_from_system()
	_guardado_hace = 0.0
	_guardado_notas = _notas.size()
	_guardado_tramos = rejilla.size()
	_destello_guardado = 1.0

	if not silencioso:
		_avisar("Guardado · %d notas · %d tramos" % [_notas.size(), rejilla.size()])
	print("GUARDADO  %d notas  ·  %d tramos" % [_notas.size(), rejilla.size()])


func _cargar_tema_si_existe() -> void:
	if not FileAccess.file_exists(_ruta_tema()):
		print("SIN TEMA PREVIO · se empieza de cero")
		_tema_mtime = 0
		return
	_tema_mtime = FileAccess.get_modified_time(_ruta_tema())
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

	_tolerancia_ms = float(datos.get("toleranciaMs", 120.0))

	# Las FIGURAS mandan si estan. Si no, se reconstruyen desde los instantes
	# sueltos —un tema grabado a mano antes de que existiera esta linea— dando
	# a cada nota la figura por defecto. Asi nada se pierde y todo es editable.
	_notas.clear()
	for x in datos.get("figuras", []):
		_notas.append({
			"t": float(x.get("t", 0.0)),
			"fig": int(x.get("fig", 4)),
			"p": bool(x.get("p", false)),
			"v": int(x.get("v", 0)),
		})

	if _notas.is_empty():
		for x in datos.get("melodia", []):
			_notas.append({"t": float(x), "fig": 4, "p": false, "v": 0})
		for x in datos.get("melodiaFondo", []):
			_notas.append({"t": float(x), "fig": 4, "p": false, "v": 1})

	_notas.sort_custom(func(a, b): return float(a["t"]) < float(b["t"]))

	# Si el tema ya trae voz de fondo, el carril se parte solo.
	_voces = 2 if _notas_de_voz(1) > 0 else 1
	_voz = 0

	# Dos notas identicas en el MISMO instante no significan nada y ademas
	# rompen el pegado y el calculo del hueco minimo. Se quitan al cargar.
	var repes := 0
	for k in range(_notas.size() - 1, 0, -1):
		var a: Dictionary = _notas[k]
		var b: Dictionary = _notas[k - 1]
		if absf(float(a["t"]) - float(b["t"])) < 0.001 			and int(a["fig"]) == int(b["fig"]) 			and bool(a.get("p", false)) == bool(b.get("p", false)) 			and int(a.get("v", 0)) == int(b.get("v", 0)):
			_notas.remove_at(k)
			repes += 1

	_sincronizar_melodia()
	if repes > 0:
		_avisar("Se quitaron %d notas duplicadas en el mismo instante" % repes)
		print("LIMPIEZA  %d notas duplicadas fuera" % repes)

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


## El panel. Tres bloques y nada mas:
##
##   CABECERA   que hay cargado, donde estas, si se guardo
##   ESTADO     una linea con lo que esta activo AHORA
##   CONTEXTO   una linea que cambia segun lo que estes haciendo
##
## Todo lo demas —la lista de teclas— vive detras de H. Una interfaz que
## explica treinta cosas a la vez no explica ninguna, y encima se comia el
## sitio de la linea de tiempo, que es donde se trabaja.
func _actualizar_texto() -> void:
	var L := []

	if _archivos.is_empty():
		L.append("[b]GRABADOR DE TEMAS — E.N.M.A.[/b]")
		L.append("")
		L.append("[color=#ff8866]No hay canciones.[/color]")
		L.append("  Deja un .mp3, .ogg o .wav en [color=#66aaff]audio/[/color] y reinicia.")
		_etiqueta.text = "\n".join(L)
		return

	var t := _posicion()

	# ---------------------------------------------------------- cabecera
	L.append("[b]%s[/b]   [color=#556677]%d/%d[/color]      %s [b]%s[/b] / %s      %s" % [
		_archivos[_indice_archivo], _indice_archivo + 1, _archivos.size(),
		"[color=#88ff88]▶[/color]" if _repro.playing else "[color=#667788]■[/color]",
		_reloj(t), _reloj(_duracion()), _estado_guardado()])

	# ------------------------------------------------------------ estado
	var cf: Color = COLOR_FIGURA.get(_figura, Color.WHITE)
	var trozos := []
	if _voces > 1:
		trozos.append("voz [b]%s[/b] [color=#556677](TAB)[/color]" % NOMBRE_VOZ[_voz])
	trozos.append("[color=#%s]■[/color] [b]%s[/b]" % [cf.to_html(false), _nombre_figura_actual()])
	trozos.append("zona [b]±%.0f[/b] ms" % _tolerancia_ms)
	if not _tramos.is_empty():
		var i := _tramo_en(t)
		var cc: Color = COLORES_COMPAS.get(int(_tramos[i]["compas"]), Color.WHITE)
		trozos.append("tramo [b]%d[/b]/%d · [color=#%s][b]%d/4[/b][/color] · [b]%.1f[/b] BPM" % [
			i + 1, _tramos.size(), cc.to_html(false),
			int(_tramos[i]["compas"]), float(_tramos[i]["bpm"])])
	if _notas_de_voz(1) > 0:
		trozos.append("[b]%d[/b] + [color=#8899aa]%d[/color] notas" % [
			_notas_de_voz(0), _notas_de_voz(1)])
	else:
		trozos.append("[b]%d[/b] notas" % _notas.size())
	trozos.append("%.0f ms/px" % (((_vista().y - _vista().x) / maxf(1.0, _ancho_barra())) * 1000.0))
	L.append("[color=#8899aa]" + "   [color=#3a4a5a]│[/color]   ".join(trozos) + "[/color]")

	# ---------------------------------------------------------- contexto
	L.append("")
	L.append(_linea_contexto(t))

	if _mensaje_ttl > 0.0:
		L.append("[color=#66aaff]%s[/color]" % _mensaje)

	if _ayuda_visible:
		L.append("")
		L.append_array(_texto_ayuda())

	_etiqueta.text = "\n".join(L)


func _estado_guardado() -> String:
	if _guardado_error != "":
		return "[b][color=#ff5555]⚠ SIN GUARDAR — %s[/color][/b]" % _guardado_error
	if _guardado_hora == "":
		return "[color=#667788]sin guardar todavía[/color]"
	var brillo := "88ff88" if _destello_guardado > 0.0 else "4a6a4a"
	return "[color=#%s]●[/color] [color=#667788]guardado hace %s[/color]" % [
		brillo, _hace_cuanto(_guardado_hace)]


## Una sola linea, y dice lo que hace falta AHORA. Cuando no pasa nada, la
## chuleta minima; cuando estas en mitad de algo, como terminarlo.
func _linea_contexto(t: float) -> String:
	if _guardado_error != "":
		return "[color=#ff5555]El archivo no se está guardando. Míralo antes de seguir trabajando.[/color]"
	if _ensayo:
		var ca: Color = COLOR_ARMA[_arma]
		var pie := "[color=#667788]no se edita nada mientras ensayas[/color]"
		if not _repro.playing:
			pie = "[color=#ff8866]dale a [b]P[/b]: hace falta que suene[/color]"
		var contra := CAPA_DE_ARMA[_arma]
		if _arma == 0 and _voces > 1:
			contra = "melodía %s" % NOMBRE_VOZ[_voz]
		return "[b][color=#%s]ENSAYO · %s[/color][/b]  cada clic es un BA, va al [b]%s[/b]      %s" % [
			ca.to_html(false), ARMAS[_arma].to_upper(), contra, pie]
	if _moviendo_sel:
		return "[b][color=#88ff88]Moviendo %d notas[/color][/b]   ·   suelta para fijar" % _indices_seleccionados().size()
	if _pintando > 0:
		return "[b][color=#88ff88]Pintando[/color][/b]  %d puestas   ·   suelta para fijar" % _pintadas
	if _pintando < 0:
		return "[b][color=#ff8866]Borrando[/color][/b]  %d quitadas   ·   suelta para fijar" % _pintadas
	if _arrastrando_bloque:
		return "[b][color=#ffd240]Moviendo el bloque[/color][/b]   ·   suelta para fijar   ·   clic derecho cancela"
	if _pegando:
		return "[b][color=#ffd240]Pegando %s[/color][/b] en %s   ·   [b]ENTER[/b] confirma   ·   clic derecho cancela" % [
			_resumen_patron(), _reloj(_pegar_en)]
	if _hay_sel_notas():
		return "[b]%d notas[/b] seleccionadas  %s → %s   ·   arrástralas   ·   [b]SUPR[/b] borra   ·   [b]CTRL+C[/b] copia" % [
			_indices_seleccionados().size(), _reloj(_sel_ini()), _reloj(_sel_fin())]
	if _hay_seleccion():
		return "Rango [b]%s → %s[/b] · [b]%d/4[/b] · [b]%.1f[/b] BPM   ·   [b]ENTER[/b] lo aplica   ·   clic derecho lo quita" % [
			_reloj(minf(_sel_desde, _sel_hasta)), _reloj(maxf(_sel_desde, _sel_hasta)),
			_sel_compas, _sel_bpm]
	if not _marcados.is_empty():
		var bq := _bloque()
		return "[color=#ffd240]Bloque[/color] tramos [b]%d–%d[/b]   ·   arrástralo   ·   [b], .[/b] lo empujan   ·   [b]CTRL+V[/b] pega el patrón" % [
			bq.x + 1, bq.y + 1]

	var aviso := ""
	if not _tramos.is_empty():
		var der := _deriva_de(_tramo_en(t))
		if absf(der) > 0.030:
			aviso = "      [color=#ff8866]⚠ este tramo arranca %+.0f ms tarde · [b]B[/b] lo cuadra[/color]" % (der * 1000.0)

	var base := "[color=#667788]clic pone · arrastra pinta · MAYÚS+clic selecciona · [b]P[/b] suena[/color]"
	if not _portapapeles_notas.is_empty():
		base = "[color=#667788]%d notas copiadas · [b]CTRL+V[/b] las pega en el cursor[/color]" % _portapapeles_notas.size()
	return base + "      [color=#3a4a5a][b]H[/b] ayuda[/color]" + aviso


func _texto_ayuda() -> Array:
	var L := []
	L.append("[color=#3a4a5a]──────────────────────────────────────────────────────────────────[/color]")
	L.append("[color=#ffcc66][b]MELODÍA[/b][/color]  [color=#667788]carril de abajo · dos voces[/color]")
	L.append("   [b]F[/b] figura    [b]D[/b] puntillo    [b]O[/b] / [b]MAYÚS+P[/b] techo de la zona de ayuda")
	L.append("   [b]TAB[/b] añade una segunda voz de fondo, y alterna entre las dos")
	L.append("   [color=#8899aa]solo se edita la voz activa · la otra se ve pero no se toca[/color]")
	L.append("   [color=#8899aa]si vuelves a la principal con la de fondo vacía, se quita sola[/color]")
	L.append("   [color=#8899aa]clic pone · sobre la misma figura la quita · arrastra pinta · clic derecho borra[/color]")
	L.append("   [color=#8899aa]MAYÚS+clic en dos notas coge todo lo que hay entre ellas[/color]")
	L.append("   [color=#8899aa]la selección se arrastra · SUPR borra · CTRL+C copia · CTRL+V pega[/color]")
	L.append("")
	L.append("[color=#ffcc66][b]MAPA DE TEMPO[/b][/color]  [color=#667788]barra de colores[/color]")
	L.append("   [b]G[/b] parte aquí    [b]X[/b] quita    [b]T[/b] golpea al ritmo    [b]↑↓←→[/b] BPM    [b]C[/b] compás    [b]B[/b] cuadra el borde")
	L.append("   [color=#8899aa]arrastra para elegir un rango · ENTER lo aplica[/color]")
	L.append("   [color=#8899aa]MAYÚS+clic marca tramos que se repiten · CTRL+V pega el patrón[/color]")
	L.append("   [color=#8899aa], . mueven el borde ±10 ms · con MAYÚS, un tiempo entero[/color]")
	L.append("   [b]N[/b] ajuste a rejilla %s     [b]L[/b] rejilla continua %s" % [
		"[color=#88ff88]ON[/color]" if _ajustar_a_rejilla else "[color=#ff8866]OFF[/color]",
		"[color=#88ff88]ON[/color]" if _rejilla_continua else "[color=#ff8866]OFF[/color]"])
	L.append("")
	L.append("[color=#ffcc66][b]ENSAYO[/b][/color]  [color=#667788]columna de la derecha[/color]")
	L.append("   [b]R[/b] enciende o apaga    [color=#8899aa]elige arma y dale a P[/color]")
	L.append("   [color=#8899aa]golpea con [b]ESPACIO[/b] o en el pad de abajo[/color]")
	L.append("   [color=#8899aa]el ajuste compensa tu propio desfase · «calibrar» lo pone solo[/color]")
	L.append("   [color=#8899aa]la barra de abajo repite la melodía: verde acertada, rojo fallada,[/color]")
	L.append("   [color=#8899aa]ámbar si la clavaste pero te habías saltado la anterior[/color]")
	L.append("")
	L.append("[color=#ffcc66][b]VISTA · SONIDO · ARCHIVO[/b][/color]")
	L.append("   [b]rueda[/b] o [b]Z[/b]/[b]A[/b] zoom    [b]V[/b] ver todo    [b]P[/b] suena    [b]M[/b] metrónomo")
	L.append("   [b]E[/b] la vista sigue al cursor mientras suena: %s" % (
		"[color=#88ff88]ON[/color]" if _seguir else "[color=#ff8866]OFF[/color]"))
	L.append("   [b]1 2[/b] canción %s    [b]3 4[/b] metrónomo %s" % [
		_barra_db(_vol_cancion), _barra_db(_vol_metronomo)])
	L.append("   [b][ ][/b] cambia de canción    [b]S[/b] guarda    [b]ESC[/b] sale")
	L.append("")
	L.append("[color=#ffcc66][b]DESHACER[/b][/color]  [b]CTRL+Z[/b] · [b]CTRL+Y[/b]")
	if _pila.is_empty():
		L.append("   [color=#667788]nada que deshacer[/color]")
	else:
		L.append("   [color=#8899aa]deshace %s (%d pasos)[/color]" % [_pila[-1]["motivo"], _pila.size()])
	if not _rehacer_pila.is_empty():
		L.append("   [color=#8899aa]rehace %s (%d pasos)[/color]" % [
			_rehacer_pila[-1]["motivo"], _rehacer_pila.size()])

	var ap := _notas_apretadas()
	if ap > 0:
		L.append("")
		L.append("[color=#667788]%d de %d notas llevan la zona recortada por ir pegadas · mínima ±%.0f ms[/color]" % [
			ap, _notas.size(), _tolerancia_maxima_ms()])
	return L


# --------------------------------------------------------------- ENSAYO
#
# El primer trozo JUGABLE. Los BA son clics, y cada clic se juzga contra la
# capa del arma que elijas — igual que lo hara el juego.
#
# Sirve para dos cosas que no se pueden responder pensando:
#   · si una melodia es tocable de verdad o solo lo parece en la pantalla
#   · cuanto desfase tiene TU mano, para compensarlo

const ARMAS: Array[String] = ["ligera", "media", "pesada"]
const CAPA_DE_ARMA: Array[String] = ["melodía", "pulso", "compás"]
const COLOR_ARMA := [
	Color(0.85, 0.55, 0.95),
	Color(0.45, 0.70, 0.95),
	Color(0.95, 0.60, 0.40),
]

var _ensayo := false
var _arma := 0
var _desfase_ms := 0.0

var _aciertos := 0
var _fallos := 0
var _suma_desfase := 0.0          # para la media, en segundos
var _cadena := 0
var _mejor_cadena := 0
var _ultima_nota := -1            # ultima nota de melodia enganchada
var _indice_objetivo := -1        # la que acaba de juzgarse
var _juicio := ""
var _juicio_color := Color.WHITE
var _juicio_ttl := 0.0
var _destello_golpe := 0.0

## Rectangulos de los botones, para poder pulsarlos. Se rehacen al dibujar.
var _botones: Array[Dictionary] = []

## Lo que has intentado, para la barra de aciertos.
## {"t": instante del objetivo, "dur": ancho, "ok": bool, "salto": bool}
var _intentos: Array[Dictionary] = []

## Tonos que SUBEN con la racha. Acertar seguido suena mejor que acertar
## suelto: el premio se oye antes de leerse.
var _tonos: Array[AudioStreamWAV] = []
var _tono_fallo: AudioStreamWAV


## Instantes contra los que se juzga, segun el arma.
##
##   ligera → las notas de la melodia (voz principal)
##   media  → cada tiempo, calculado del BPM
##   pesada → el primer tiempo de cada compas
##
## Solo se devuelve el mas cercano: no hace falta la lista entera.
func _objetivo_mas_cercano(t: float) -> float:
	if _tramos.is_empty(): return -1.0

	# La ligera se juzga contra LA VOZ ACTIVA. Con TAB eliges cual ensayas: la
	# principal, o la linea de fondo del bonus experto. Antes miraba siempre la
	# principal, asi que tocar la de fondo salia todo rojo aunque la clavaras.
	if _arma == 0:
		var mejor := -1.0
		var md := 9999.0
		_indice_objetivo = -1
		for i in _notas.size():
			if _voz_de(_notas[i]) != _voz: continue
			var ti: float = float(_notas[i]["t"])
			var d: float = absf(ti - t)
			if d < md:
				md = d
				mejor = ti
				_indice_objetivo = i
			elif ti > t: break               # ordenadas
		return mejor

	_indice_objetivo = -1

	var i := _tramo_en(t)
	var org: float = _origen_de(i)
	var paso: float = 60.0 / _bpm_en(t)
	if _arma == 2: paso *= float(_compas_en(t))
	return org + roundf((t - org) / paso) * paso


## La ventana que se le concede a ese golpe.
func _ventana_del_objetivo(objetivo: float) -> float:
	var techo: float = _tolerancia_ms / 1000.0
	if _arma != 0: return techo
	for i in _notas.size():
		if _voz_de(_notas[i]) == _voz and absf(float(_notas[i]["t"]) - objetivo) < 0.001:
			return _zona_de(i)
	return techo


## Un clic = un BA.
func _golpear() -> void:
	if not _repro.playing:
		_avisar("Dale a P: el ensayo necesita que suene")
		return

	var t := _posicion() + _desfase_ms / 1000.0
	var objetivo := _objetivo_mas_cercano(t)
	if objetivo < 0.0: return

	var d := t - objetivo
	var ventana := _ventana_del_objetivo(objetivo)
	_destello_golpe = 1.0
	_juicio_ttl = 1.2

	if absf(d) <= ventana:
		_aciertos += 1
		_suma_desfase += d

		# ⚠️ Con la LIGERA no basta con acertar ALGUNA nota: la cadena exige la
		# SIGUIENTE. Saltarse una la rompe, aunque el golpe caiga limpio en la
		# de mas alla. Es la regla de verdad, y el ensayo tiene que medirla o
		# no sirve para saber si una melodia es tocable.
		# ⚠️ NO vale comparar indices de _notas: ese array lleva LAS DOS VOCES
		# mezcladas y ordenadas por tiempo. Entre dos notas seguidas de la
		# principal puede haber diez de fondo, asi que los indices no son
		# consecutivos y la alarma saltaba en TODOS los golpes.
		#
		# Lo que hay que mirar es si te dejaste atras alguna nota DE TU VOZ.
		var salto := false
		if _arma == 0 and _cadena > 0 and _indice_objetivo >= 0 and _ultima_nota >= 0:
			if _indice_objetivo <= _ultima_nota:
				salto = true
			else:
				for k in range(_ultima_nota + 1, _indice_objetivo):
					if _voz_de(_notas[k]) == _voz:
						salto = true
						break

		if salto:
			_cadena = 1
			_juicio = "%+.0f ms · saltaste una" % (d * 1000.0)
			_juicio_color = Color(1.0, 0.75, 0.35)
		else:
			_cadena += 1
			_juicio = "%+.0f ms" % (d * 1000.0)
			_juicio_color = Color(0.5, 1.0, 0.5) if absf(d) < ventana * 0.4 else Color(0.9, 0.9, 0.5)

		_ultima_nota = _indice_objetivo
		_mejor_cadena = maxi(_mejor_cadena, _cadena)
		_anotar_intento(objetivo, true, salto)
		_sonar_acierto()
	else:
		_fallos += 1
		_cadena = 0
		_ultima_nota = -1
		_juicio = "fuera  %+.0f ms" % (d * 1000.0)
		_juicio_color = Color(1.0, 0.45, 0.45)
		_anotar_intento(objetivo, false, false)
		_sonar_fallo()


## Ancho del bloque que se pinta: el de la nota si es la ligera, y si no un
## trozo fijo, porque el pulso y el compas no tienen duracion propia.
func _anotar_intento(objetivo: float, ok: bool, salto: bool) -> void:
	var dur := 0.09
	if _arma == 0 and _indice_objetivo >= 0:
		dur = _dur_nota(_notas[_indice_objetivo])
	_intentos.append({"t": objetivo, "dur": dur, "ok": ok, "salto": salto})
	if _intentos.size() > 400: _intentos.remove_at(0)


## El tono sube con la racha: acertar seguido SUENA mejor que acertar suelto.
func _sonar_acierto() -> void:
	if _tonos.is_empty(): return
	var i: int = clampi(_cadena - 1, 0, _tonos.size() - 1)
	_sfx.stream = _tonos[i]
	_sfx.volume_db = -7.0 + minf(4.0, float(_cadena) * 0.35)
	_sfx.play()


func _sonar_fallo() -> void:
	if _tono_fallo == null: return
	_sfx.stream = _tono_fallo
	_sfx.volume_db = -13.0
	_sfx.play()


func _reiniciar_ensayo() -> void:
	_aciertos = 0
	_fallos = 0
	_suma_desfase = 0.0
	_cadena = 0
	_mejor_cadena = 0
	_ultima_nota = -1
	_intentos.clear()
	_juicio = ""
	_juicio_ttl = 0.0


## El desfase medio de tus aciertos. Si sale +40 ms, es que llegas tarde.
func _desfase_medio_ms() -> float:
	if _aciertos == 0: return 0.0
	return _suma_desfase / float(_aciertos) * 1000.0


## Pone el ajuste para compensar tu propia tendencia, y vuelve a contar.
func _calibrar() -> void:
	if _aciertos < 5:
		_avisar("Necesito al menos 5 aciertos para calibrar")
		return
	var medio := _desfase_medio_ms()
	_desfase_ms = clampf(_desfase_ms - medio, -200.0, 200.0)
	_reiniciar_ensayo()
	_avisar("Calibrado: ajuste %+.0f ms (ibas %+.0f ms)" % [_desfase_ms, medio])


func _boton(r: Rect2, id: String, texto: String, activo: bool, col: Color) -> void:
	_botones.append({"r": r, "id": id})
	var raton := get_local_mouse_position()
	var encima := r.has_point(raton)

	var fondo := Color(col.r, col.g, col.b, 0.85) if activo else Color(1, 1, 1, 0.10 if encima else 0.05)
	draw_rect(r, fondo)
	draw_rect(r, col if activo else Color(1, 1, 1, 0.30 if encima else 0.14), false, 1.0)
	draw_string(ThemeDB.fallback_font, Vector2(r.position.x + 8, r.position.y + r.size.y * 0.68),
		texto, HORIZONTAL_ALIGNMENT_LEFT, r.size.x - 12, 13,
		Color(0.05, 0.05, 0.08) if activo else Color(1, 1, 1, 0.80))


## La columna de la derecha. Se dibuja siempre; en ensayo enseña las cuentas.
func _dibujar_ensayo(w: float) -> void:
	_botones.clear()
	var cx := w - ANCHO_COLUMNA + 14.0
	var an := ANCHO_COLUMNA - 30.0
	var y := 210.0

	draw_string(ThemeDB.fallback_font, Vector2(cx, y), "ENSAYO",
		HORIZONTAL_ALIGNMENT_LEFT, an, 13, Color(1, 0.8, 0.4, 0.9))
	y += 10.0
	_boton(Rect2(cx, y, an, 30), "ensayo",
		"  ● EN MARCHA  (R)" if _ensayo else "  ○ encender  (R)", _ensayo,
		Color(0.4, 0.9, 0.5))
	y += 44.0

	draw_string(ThemeDB.fallback_font, Vector2(cx, y), "ARMA",
		HORIZONTAL_ALIGNMENT_LEFT, an, 12, Color(1, 1, 1, 0.45))
	y += 8.0
	for k in 3:
		_boton(Rect2(cx, y, an, 24), "arma%d" % k,
			"  %s  ·  %s" % [ARMAS[k], CAPA_DE_ARMA[k]], _arma == k, COLOR_ARMA[k])
		y += 27.0

	# ---- ZONA DE AYUDA: cuanto desfase se acepta ----
	y += 16.0
	draw_string(ThemeDB.fallback_font, Vector2(cx, y), "ZONA DE AYUDA",
		HORIZONTAL_ALIGNMENT_LEFT, an, 12, Color(1, 1, 1, 0.45))
	y += 10.0
	_boton(Rect2(cx, y, 34, 26), "zona-", "  −", false, Color.WHITE)
	_boton(Rect2(cx + an - 34, y, 34, 26), "zona+", "  +", false, Color.WHITE)
	draw_string(ThemeDB.fallback_font, Vector2(cx + 42, y + 18),
		"±%.0f ms" % _tolerancia_ms, HORIZONTAL_ALIGNMENT_CENTER, an - 84, 16, Color.WHITE)
	y += 28.0
	var ap2 := _notas_apretadas()
	draw_string(ThemeDB.fallback_font, Vector2(cx, y),
		"techo · %d notas recortadas" % ap2 if ap2 > 0 else "techo · ninguna recortada",
		HORIZONTAL_ALIGNMENT_LEFT, an, 11, Color(1, 1, 1, 0.35))
	y += 20.0

	# ---- AJUSTE DEL CLIC: corrige TU propio retraso ----
	draw_string(ThemeDB.fallback_font, Vector2(cx, y), "AJUSTE DEL CLIC",
		HORIZONTAL_ALIGNMENT_LEFT, an, 12, Color(1, 1, 1, 0.45))
	y += 10.0
	_boton(Rect2(cx, y, 34, 26), "menos", "  −", false, Color.WHITE)
	_boton(Rect2(cx + an - 34, y, 34, 26), "mas", "  +", false, Color.WHITE)
	draw_string(ThemeDB.fallback_font, Vector2(cx + 42, y + 18),
		"%+.0f ms" % _desfase_ms, HORIZONTAL_ALIGNMENT_CENTER, an - 84, 16, Color.WHITE)
	y += 28.0
	draw_string(ThemeDB.fallback_font, Vector2(cx, y),
		"negativo si tus clics van tarde", HORIZONTAL_ALIGNMENT_LEFT, an, 11,
		Color(1, 1, 1, 0.35))
	y += 18.0
	_boton(Rect2(cx, y, an, 24), "calibrar", "  calibrar con lo tocado", false,
		Color(0.6, 0.8, 1.0))
	y += 38.0

	if _aciertos + _fallos > 0:
		var total := _aciertos + _fallos
		draw_string(ThemeDB.fallback_font, Vector2(cx, y),
			"%d de %d   ·   %.0f%%" % [_aciertos, total, 100.0 * _aciertos / total],
			HORIZONTAL_ALIGNMENT_LEFT, an, 15, Color.WHITE)
		y += 20.0
		draw_string(ThemeDB.fallback_font, Vector2(cx, y),
			"vas %+.0f ms de media" % _desfase_medio_ms(),
			HORIZONTAL_ALIGNMENT_LEFT, an, 12, Color(1, 1, 1, 0.55))
		y += 18.0
		draw_string(ThemeDB.fallback_font, Vector2(cx, y),
			"racha %d   ·   mejor %d" % [_cadena, _mejor_cadena],
			HORIZONTAL_ALIGNMENT_LEFT, an, 12, Color(1, 1, 1, 0.55))
		y += 24.0
		_boton(Rect2(cx, y, an, 22), "reiniciar", "  volver a cero", false, Color.WHITE)
		y += 32.0

	if _juicio_ttl > 0.0:
		var c := _juicio_color
		c.a = clampf(_juicio_ttl, 0.0, 1.0)
		draw_string(ThemeDB.fallback_font, Vector2(cx, y + 24), _juicio,
			HORIZONTAL_ALIGNMENT_LEFT, an, 26, c)


## Devuelve true si el clic se lo quedo un boton.
func _pulsar_boton(pos: Vector2) -> bool:
	for b in _botones:
		var r: Rect2 = b["r"]
		if not r.has_point(pos): continue
		match String(b["id"]):
			"ensayo":
				_ensayo = not _ensayo
				_reiniciar_ensayo()
				_avisar("Ensayo " + ("en marcha · cada clic es un BA" if _ensayo else "apagado"))
			"arma0", "arma1", "arma2":
				_arma = int(String(b["id"]).substr(4))
				_reiniciar_ensayo()
				_avisar("Arma %s · va al %s" % [ARMAS[_arma], CAPA_DE_ARMA[_arma]])
			"menos":
				_desfase_ms = clampf(_desfase_ms - 5.0, -200.0, 200.0)
			"mas":
				_desfase_ms = clampf(_desfase_ms + 5.0, -200.0, 200.0)
			"zona-":
				_ajustar_tolerancia(-5.0)
			"zona+":
				_ajustar_tolerancia(5.0)
			"calibrar": _calibrar()
			"reiniciar": _reiniciar_ensayo()
		return true
	return false


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

		# Marcado para copiar: contorno ambar y el orden dentro del patron.
		var orden := _marcados.find(i)
		if orden >= 0:
			draw_rect(Rect2(x0 - 2, by - 5, maxf(4.0, x1 - x0 + 4), alto + 10),
				COLOR_PATRON, false, 3.0)
			draw_string(ThemeDB.fallback_font, Vector2(x0 + 4, by - 10),
				"%d" % (orden + 1), HORIZONTAL_ALIGNMENT_LEFT, 40, 14, COLOR_PATRON)

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
		draw_string(ThemeDB.fallback_font, Vector2(xa + 6, by - 26),
			"%d/4  %.1f BPM  ·  ENTER aplica" % [_sel_compas, _sel_bpm],
			HORIZONTAL_ALIGNMENT_LEFT, 320, 12, csel)

	# ---- asas de los bordes, para que se vea que se pueden agarrar ----
	for i in range(1, _tramos.size()):
		var xb2: float = _x_de_tiempo(float(_tramos[i]["desde"]))
		if xb2 < margen or xb2 > margen + ancho: continue
		var agarrado := (i == _arrastrando_borde)
		var cb := Color.WHITE if agarrado else Color(1, 1, 1, 0.75)
		draw_line(Vector2(xb2, by - 4), Vector2(xb2, by + alto + 4), cb,
			3.0 if agarrado else 2.0)
		draw_rect(Rect2(xb2 - 3, by + alto * 0.5 - 7, 6, 14), cb)

		# Donde arranca DE VERDAD la rejilla de ese tramo, si el borde que
		# pusiste no cayo justo en una linea de compas.
		var org: float = _origen_de(i)
		if absf(org - float(_tramos[i]["desde"])) > 0.004:
			var xo: float = _x_de_tiempo(org)
			if xo >= margen and xo <= margen + ancho:
				draw_line(Vector2(xo, by - 4), Vector2(xo, by + alto + 4),
					Color(0.5, 1.0, 0.6, 0.85), 2.0)
				draw_line(Vector2(xb2, by + alto + 6), Vector2(xo, by + alto + 6),
					Color(0.5, 1.0, 0.6, 0.5), 1.0)

	# ---- vista previa del pegado: lo que ENTER va a escribir ----
	if _pegando and not _portapapeles.is_empty():
		var pt := _pegar_en
		for x in _portapapeles:
			var pa: float = _x_de_tiempo(pt)
			var pb: float = _x_de_tiempo(pt + float(x["dur"]))
			pt += float(x["dur"])
			if pb < margen or pa > margen + ancho: continue
			pa = clampf(pa, margen, margen + ancho)
			pb = clampf(pb, margen, margen + ancho)
			var cp: Color = COLORES_COMPAS.get(int(x["compas"]), Color.WHITE)
			draw_rect(Rect2(pa, by, maxf(2.0, pb - pa), alto),
				Color(cp.r, cp.g, cp.b, 0.55))
			draw_rect(Rect2(pa, by, maxf(2.0, pb - pa), alto), COLOR_PATRON, false, 2.0)
			if pb - pa > 46.0:
				draw_string(ThemeDB.fallback_font, Vector2(pa + 5, by + alto * 0.62),
					"%d/4" % int(x["compas"]), HORIZONTAL_ALIGNMENT_LEFT,
					pb - pa - 9, 13, Color(1, 1, 1, 0.95))

		var xp: float = _x_de_tiempo(_pegar_en)
		if xp >= margen - 1.0 and xp <= margen + ancho:
			draw_line(Vector2(xp, by - 12), Vector2(xp, by + alto + 12), COLOR_PATRON, 2.0)
			draw_string(ThemeDB.fallback_font, Vector2(xp + 5, by - 26),
				"ENTER pega aquí", HORIZONTAL_ALIGNMENT_LEFT, 260, 12, COLOR_PATRON)

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
			var i_tr: int = _tramo_en(tt)
			var tr2: Dictionary = _tramos[i_tr]
			var paso: float = 60.0 / float(tr2["bpm"])
			var desde2: float = _origen_de(i_tr)
			var n_pulso: int = int(ceilf((tt - desde2) / paso))
			var tp: float = desde2 + n_pulso * paso
			if tp > v.y: break
			var xg: float = _x_de_tiempo(tp)
			var fuerte_g: bool = posmod(n_pulso, int(tr2["compas"])) == 0
			draw_line(Vector2(xg, by), Vector2(xg, by + alto),
				Color(1, 1, 1, 0.55 if fuerte_g else 0.18),
				2.0 if fuerte_g else 1.0)
			tt = tp + paso * 0.5

	# ---- la melodia, como figuras ----
	var ny := _y_notas()
	var alto_voz := ALTO_NOTAS / float(_voces)

	if _voces == 1:
		draw_rect(Rect2(margen, ny, ancho, ALTO_NOTAS), Color(1, 1, 1, 0.055))
	else:
		# Una fila por voz. La activa se ve mas clara; la otra sigue a la vista
		# para poder encajar una contra otra, pero no se puede tocar.
		for iv in _voces:
			var yv := ny + iv * alto_voz
			draw_rect(Rect2(margen, yv, ancho, alto_voz),
				Color(1, 1, 1, 0.07 if iv == _voz else 0.025))
			draw_string(ThemeDB.fallback_font, Vector2(margen + 4, yv + 13),
				NOMBRE_VOZ[iv], HORIZONTAL_ALIGNMENT_LEFT, 90, 11,
				Color(1, 1, 1, 0.45 if iv == _voz else 0.18))

	# La banda de la seleccion, por debajo de todo.
	if _hay_sel_notas():
		var sa: float = clampf(_x_de_tiempo(_sel_ini()), margen, margen + ancho)
		var sb: float = clampf(_x_de_tiempo(_sel_fin()), margen, margen + ancho)
		draw_rect(Rect2(sa - 3, ny - 3, maxf(6.0, sb - sa + 6), ALTO_NOTAS + 6),
			Color(1, 1, 1, 0.10))
		draw_rect(Rect2(sa - 3, ny - 3, maxf(6.0, sb - sa + 6), ALTO_NOTAS + 6),
			Color.WHITE, false, 2.0)

	for idx_n in _notas.size():
		var n: Dictionary = _notas[idx_n]
		var n0: float = float(n["t"])

		# Fuera de la ventana no se calcula NADA. Con 980 notas y solo unas
		# decenas a la vista, esto es la diferencia entre ir fluido o no.
		if n0 > v.y + 2.0: break                 # ordenadas: ya no hay mas
		if n0 < v.x - 6.0: continue

		var tol: float = _zona_de(idx_n)
		var fg: int = int(n["fig"])
		var n1: float = n0 + _dur_nota(n)
		var xa: float = _x_de_tiempo(n0)
		var xb: float = _x_de_tiempo(n1)
		if xb < margen or xa > margen + ancho: continue

		# La zona de ayuda: donde el jugador puede pulsar y cuenta.
		var za: float = clampf(_x_de_tiempo(n0 - tol), margen, margen + ancho)
		var zb: float = clampf(_x_de_tiempo(n0 + tol), margen, margen + ancho)
		draw_rect(Rect2(za, ny + _voz_de(n) * alto_voz, maxf(1.0, zb - za), alto_voz),
			Color(1, 1, 1, 0.13 if _voz_de(n) == _voz else 0.05))

		var vn := _voz_de(n)
		var yv2: float = ny + vn * alto_voz
		var cf: Color = COLOR_FIGURA.get(fg, Color.WHITE)
		if vn != _voz: cf = cf.darkened(0.55)
		xa = clampf(xa, margen, margen + ancho)
		xb = clampf(xb, margen, margen + ancho)
		if _nota_seleccionada(idx_n): cf = cf.lightened(0.35)
		var dy: float = 16.0 if _voces > 1 else 8.0
		draw_rect(Rect2(xa, yv2 + dy, maxf(2.0, xb - xa - 1.0), alto_voz - dy - 6.0), cf)
		if _nota_seleccionada(idx_n):
			draw_rect(Rect2(xa, yv2 + dy, maxf(2.0, xb - xa - 1.0), alto_voz - dy - 6.0),
				Color.WHITE, false, 1.5)
		draw_line(Vector2(xa, yv2 + dy - 2.0), Vector2(xa, yv2 + alto_voz - 3),
			cf.lightened(0.5), 2.0)
		if xb - xa > 26.0:
			draw_string(ThemeDB.fallback_font, Vector2(xa + 5, yv2 + alto_voz * 0.72),
				"1/%d" % fg, HORIZONTAL_ALIGNMENT_LEFT, xb - xa - 8, 14, Color(0, 0, 0, 0.75))

		# El puntillo, donde se pone en una partitura: a la derecha de la nota.
		if bool(n.get("p", false)) and xb - xa > 12.0:
			draw_circle(Vector2(xb - 8.0, yv2 + alto_voz * 0.55), 4.0, Color(0, 0, 0, 0.85))

	draw_rect(Rect2(margen, ny, ancho, ALTO_NOTAS), Color(1, 1, 1, 0.18), false, 1.0)

	# ---- vista previa: lo que va a pasar DE VERDAD si pulsas ----
	if _sobre_las_notas(get_local_mouse_position().y) and not _tramos.is_empty():
		var mt: float = _tiempo_en_x(get_local_mouse_position().x)
		var acc := _accion_en(mt)

		# Donde va a caer DE VERDAD, incluida la regla de cabeza y cola.
		var pt: float = _casilla_para_poner(mt)
		var pdur: float = _dur_figura(_figura, pt, _puntillo)

		# Borrando o sustituyendo se señala la nota afectada, no la casilla.
		if acc == Accion.BORRAR or acc == Accion.SUSTITUIR or acc == Accion.IGUAL:
			var k := _nota_en(mt)
			if k < 0 or acc == Accion.IGUAL: k = _nota_que_arranca_en(pt)
			if k < 0: k = _nota_en(pt)
			if k >= 0 and acc != Accion.SUSTITUIR:
				pt = float(_notas[k]["t"])
				pdur = _dur_nota(_notas[k])

		var pa: float = _x_de_tiempo(pt)
		var pb: float = _x_de_tiempo(pt + pdur)
		if pa <= margen + ancho and pb >= margen:
			var cp: Color = COLOR_FIGURA.get(_figura, Color.WHITE)
			var etiqueta := ""
			match acc:
				Accion.BORRAR:
					cp = Color(1.0, 0.35, 0.35); etiqueta = "borrar"
				Accion.SUSTITUIR:
					etiqueta = "sustituir"
				Accion.IGUAL:
					cp = Color(0.5, 0.5, 0.55); etiqueta = "ya está"
				_:
					etiqueta = ""

			var yvp: float = ny + _voz * alto_voz
			var x0: float = clampf(pa, margen, margen + ancho)
			var w0: float = maxf(2.0, minf(pb, margen + ancho) - x0)
			draw_rect(Rect2(x0, yvp + 16, w0, alto_voz - 21), Color(cp.r, cp.g, cp.b, 0.30))
			draw_rect(Rect2(x0, yvp + 16, w0, alto_voz - 21), cp, false, 2.0)

			if acc == Accion.BORRAR and w0 > 16.0:
				# Una X, para que no haya duda de que ese clic quita.
				draw_line(Vector2(x0 + 4, yvp + 19), Vector2(x0 + w0 - 4, yvp + alto_voz - 8), cp, 2.0)
				draw_line(Vector2(x0 + w0 - 4, yvp + 19), Vector2(x0 + 4, yvp + alto_voz - 8), cp, 2.0)

			if etiqueta != "":
				draw_string(ThemeDB.fallback_font, Vector2(x0, yvp + 12), etiqueta,
					HORIZONTAL_ALIGNMENT_LEFT, 120, 11, cp)

	# ---- barra de ACIERTOS: la misma melodia, pintada por resultado ----
	if _ensayo:
		var ay := _y_aciertos()
		draw_rect(Rect2(margen, ay, ancho, ALTO_ACIERTOS), Color(1, 1, 1, 0.04))
		draw_string(ThemeDB.fallback_font, Vector2(margen + 4, ay + 12), "aciertos",
			HORIZONTAL_ALIGNMENT_LEFT, 80, 11, Color(1, 1, 1, 0.30))
		if _intentos.is_empty():
			draw_string(ThemeDB.fallback_font, Vector2(margen, ay + 18),
				"aún no has golpeado nada", HORIZONTAL_ALIGNMENT_CENTER, ancho, 12,
				Color(1, 1, 1, 0.22))

		for it in _intentos:
			var it0: float = float(it["t"])
			var it1: float = it0 + float(it["dur"])
			var ia: float = _x_de_tiempo(it0)
			var ib: float = _x_de_tiempo(it1)
			if ib < margen or ia > margen + ancho: continue
			ia = clampf(ia, margen, margen + ancho)
			ib = clampf(ib, margen, margen + ancho)

			var ci: Color = Color(0.35, 0.90, 0.45)
			if not bool(it["ok"]): ci = Color(0.95, 0.35, 0.35)
			elif bool(it["salto"]): ci = Color(1.00, 0.72, 0.30)

			draw_rect(Rect2(ia, ay + 4, maxf(2.0, ib - ia - 1.0), ALTO_ACIERTOS - 8), ci)

		draw_rect(Rect2(margen, ay, ancho, ALTO_ACIERTOS), Color(1, 1, 1, 0.16), false, 1.0)

	# ---- EL PAD: donde se golpea ----
	if _ensayo:
		var py := _y_pad()
		var ca: Color = COLOR_ARMA[_arma]
		var enc := get_local_mouse_position().y >= py - 2.0

		var fondo := Color(0.10, 0.11, 0.14)
		if _destello_golpe > 0.0:
			fondo = fondo.lerp(_juicio_color, _destello_golpe * 0.55)
		elif enc:
			fondo = Color(0.14, 0.15, 0.19)

		draw_rect(Rect2(margen, py, ancho, ALTO_PAD), fondo)
		draw_rect(Rect2(margen, py, ancho, ALTO_PAD),
			ca if _repro.playing else Color(1, 1, 1, 0.20), false, 2.0)

		if _juicio_ttl > 0.0:
			var cj := _juicio_color
			cj.a = clampf(_juicio_ttl * 1.4, 0.0, 1.0)
			draw_string(ThemeDB.fallback_font, Vector2(margen, py + ALTO_PAD * 0.62),
				_juicio, HORIZONTAL_ALIGNMENT_CENTER, ancho, 30, cj)
		elif not _repro.playing:
			draw_string(ThemeDB.fallback_font, Vector2(margen, py + ALTO_PAD * 0.58),
				"dale a P para que suene", HORIZONTAL_ALIGNMENT_CENTER, ancho, 19,
				Color(1, 1, 1, 0.35))
		else:
			var quien := CAPA_DE_ARMA[_arma]
			if _arma == 0 and _voces > 1:
				quien = "melodía %s  (TAB cambia)" % NOMBRE_VOZ[_voz]
			draw_string(ThemeDB.fallback_font, Vector2(margen, py + ALTO_PAD * 0.58),
				"GOLPEA AQUÍ  ·  %s al %s" % [ARMAS[_arma], quien],
				HORIZONTAL_ALIGNMENT_CENTER, ancho, 19, Color(ca.r, ca.g, ca.b, 0.75))

		# La racha, en la esquina, para verla sin apartar la vista del pad.
		if _cadena > 0:
			draw_string(ThemeDB.fallback_font, Vector2(margen + 12, py + 26),
				"×%d" % _cadena, HORIZONTAL_ALIGNMENT_LEFT, 120, 22,
				Color(0.5, 1.0, 0.5, 0.9))

	# ---- barra de desplazamiento: la cancion entera, siempre ----
	var mmy := _y_minimapa()
	draw_rect(Rect2(margen, mmy, ancho, ALTO_MINIMAPA), Color(1, 1, 1, 0.05))

	# Los tramos en miniatura, para ver la estructura de un vistazo
	for i in _tramos.size():
		var d0: float = float(_tramos[i]["desde"])
		var d1: float = float(_tramos[i + 1]["desde"]) if i + 1 < _tramos.size() else dur
		var mxa: float = margen + (d0 / dur) * ancho
		var mxb: float = margen + (d1 / dur) * ancho
		var mc: Color = COLORES_COMPAS.get(int(_tramos[i]["compas"]), Color.WHITE)
		draw_rect(Rect2(mxa, mmy, maxf(1.5, mxb - mxa), ALTO_MINIMAPA), mc.darkened(0.55))
		if _marcados.has(i):
			draw_rect(Rect2(mxa, mmy, maxf(1.5, mxb - mxa), ALTO_MINIMAPA),
				COLOR_PATRON, false, 1.5)

	# La ventana visible, como pastilla de scroll
	var mx0: float = margen + (v.x / dur) * ancho
	var mx1: float = margen + (v.y / dur) * ancho
	draw_rect(Rect2(mx0, mmy - 2, maxf(6.0, mx1 - mx0), ALTO_MINIMAPA + 4),
		Color(1, 1, 1, 0.28))
	draw_rect(Rect2(mx0, mmy - 2, maxf(6.0, mx1 - mx0), ALTO_MINIMAPA + 4),
		Color.WHITE, false, 2.0)

	# El cursor, dentro de la cancion entera
	var mxc: float = margen + (t / dur) * ancho
	draw_line(Vector2(mxc, mmy - 3), Vector2(mxc, mmy + ALTO_MINIMAPA + 3),
		Color(1, 0.9, 0.5, 0.9), 1.0)

	draw_rect(Rect2(margen, mmy, ancho, ALTO_MINIMAPA), Color(1, 1, 1, 0.22), false, 1.0)
	draw_string(ThemeDB.fallback_font, Vector2(margen, mmy - 6),
		"toda la canción · arrastra para moverte", HORIZONTAL_ALIGNMENT_LEFT,
		300, 11, Color(1, 1, 1, 0.30))

	# Destello al guardar: se ve por el rabillo del ojo sin tener que leer.
	if _destello_guardado > 0.0:
		draw_rect(Rect2(margen, _y_notas() + ALTO_NOTAS + 5, ancho, 3.0),
			Color(0.4, 1.0, 0.4, _destello_guardado))

	# Donde cayo el ultimo golpe del ensayo, contra donde estaba su objetivo.
	if _ensayo and _destello_golpe > 0.0:
		var xg2: float = _x_de_tiempo(t)
		var cg := _juicio_color
		cg.a = _destello_golpe
		draw_rect(Rect2(xg2 - 2.0, _y_barra() - 16, 4.0,
			_y_notas() + ALTO_NOTAS + 16 - _y_barra()), cg)

	# ---- cursor ----
	var xc: float = _x_de_tiempo(t)
	var abajo: float = _y_aciertos() + ALTO_ACIERTOS + 4.0 if _ensayo else _y_notas() + ALTO_NOTAS + 6.0
	draw_line(Vector2(xc, by - 12), Vector2(xc, abajo), Color.WHITE, 2.0)
	draw_circle(Vector2(xc, by - 16), 4.0, Color.WHITE)

	_dibujar_ensayo(w)

	# ---- pulso, en su columna de la derecha ----
	var cy := 130.0
	var cx := w - ANCHO_COLUMNA * 0.5
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
