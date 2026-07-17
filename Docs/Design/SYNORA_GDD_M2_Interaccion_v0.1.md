# SYNORA — GDD Funcional M2: Interacción y Observación Básica

| Campo | Valor |
|---|---|
| Documento | `SYNORA_GDD_M2_Interaccion_v0.1.md` |
| Versión | 0.1 |
| Documento padre | `SYNORA_GDD_Prototipo_Mecanico_v0.1.docx` (Docs/Design) |
| Hito cubierto | M2 — Interacción contextual y observación básica |
| Depende de | M0 (fundación) y M1 (movimiento, cámara, colisiones, transición), ambos cerrados y etiquetados (`m0-complete`, `m1-complete`) |
| Fecha real de redacción | 2026-07-17 |
| Estado | **Draft aprobado para especificación técnica** |
| Fuente | Contrato funcional entregado por el Director para el inicio formal de M2 |

> Este documento registra **decisiones funcionales**, no técnicas. La forma de implementarlas (clases, namespaces, Physics2D, Input Actions, UI concreta) se define en `Docs/Technical/SYNORA_SPEC_M2_Interaccion_v0.1.md`.

---

## 1. Propósito

M2 introduce la primera forma de interacción contextual del jugador con el mundo: acercarse a un elemento relevante, mirarlo, y **examinarlo** para leer una observación breve. No hay todavía diálogo, elección, inventario ni consecuencias persistentes: M2 es el cimiento funcional sobre el que se construirán sistemas de interacción más complejos (M3+).

## 2. Bucle de interacción

```
Explorar
 → detectar elemento relevante
 → acercarse
 → mirar hacia el elemento
 → aparece [E] Examinar
 → pulsar Interact
 → leer observación
 → pulsar Interact nuevamente
 → continuar explorando
```

## 3. Decisiones funcionales congeladas

- Acción lógica única: **Interact**.
- Binding de teclado inicial: **E**.
- Binding de gamepad previsto: **buttonSouth**.
- Tipo inicial de interacción: **Examinar** (no hay otros tipos en M2).
- Prompt visible: **`[E] Examinar`**.
- Ubicación del prompt: parte inferior central de la pantalla.
- Detección por **distancia** y **zona frontal**.
- Orientación considerada: las 4 cardinales de `PlayerOrientation` (norte, sur, este, oeste).
- Distancia máxima inicial de detección: **1.25 unidades**.
- Solo puede haber **un objetivo activo** a la vez.
- Orden de selección cuando hay varios candidatos:
  1. Mayor prioridad.
  2. Menor distancia.
  3. Desempate estable y determinista (mismo resultado siempre, sin importar el orden de evaluación).
- El objetivo actual se **conserva** mientras siga siendo válido; no cambia por capricho aunque aparezca otro candidato igual de válido.
- El panel de observación contiene únicamente:
  - Título.
  - Texto.
  - `[E] Cerrar`.
- **No** hay paginación, elecciones, retratos ni escritura animada (typewriter).
- Al abrir el panel:
  - Se oculta el prompt `[E] Examinar`.
  - Se bloquea el movimiento del jugador.
  - La velocidad física del jugador se fija en cero.
- La orientación del jugador **no cambia** mientras el panel está abierto.
- Interact cierra el panel mediante **una pulsación posterior** (no la misma pulsación que lo abrió).
- La pulsación que abre el panel **no puede cerrarlo inmediatamente** (sin importar cómo se implemente el input, esta regla funcional es innegociable).
- `Time.timeScale` permanece siempre en **1** (no se pausa el juego).
- No se conserva ninguna interacción ni UI abierta al cambiar de escena.
- Cada escena administra sus propios interactuables de forma autónoma.
- No se usa `DontDestroyOnLoad` para nada relacionado con M2.
- M2 utiliza **placeholders** (graybox): los primeros sprites definitivos quedan para M3.

## 4. Objetos graybox de M2

### 4.1 CamaraPreservacion — Terminal de diagnóstico

| Campo | Valor |
|---|---|
| Nombre | Terminal de diagnóstico |
| Título | TERMINAL DE DIAGNÓSTICO |
| Texto provisional | La unidad reconoce actividad biológica, pero no puede validar la identidad del sujeto.<br><br>Varias funciones permanecen suspendidas. |

### 4.2 CorredorTecnico — Panel de mantenimiento

| Campo | Valor |
|---|---|
| Nombre | Panel de mantenimiento |
| Título | PANEL DE MANTENIMIENTO |
| Texto provisional | La red secundaria sigue recibiendo energía, pero la conexión principal está interrumpida.<br><br>El daño no parece reciente. |

### 4.3 ClaroExterior — Nodo inactivo

| Campo | Valor |
|---|---|
| Nombre | Nodo inactivo |
| Título | NODO INACTIVO |
| Texto provisional | La estructura conserva una carga mínima.<br><br>Algo en el entorno parece responder a su presencia, aunque el nodo no emite ninguna señal reconocible. |

> Estos textos son **provisionales** y no constituyen canon narrativo definitivo. El canon y su revisión corresponden a la Biblia del Proyecto y al proceso de dirección creativa, no a este documento.

## 5. Fuera de alcance de M2

- NPCs.
- Árboles de diálogo.
- Elecciones narrativas.
- Inventario.
- Recolección.
- Misiones.
- Guardado.
- Codex.
- Activación o reparación de los objetos examinables.
- Cambios persistentes en el mundo.
- Cinemáticas.
- Audio definitivo.
- Sprites definitivos.
- Soporte de mouse.
- Interacción sostenida (mantener pulsado).
- Localización completa (multi-idioma).
- Reasignación completa de controles (rebinding UI).

## 6. Reglas de detección (funcionales)

- Un elemento es candidato a interacción si está dentro del **rango máximo** (1.25 unidades) **y** dentro de la **zona frontal** correspondiente a la orientación cardinal actual del jugador.
- Un elemento que queda **detrás** del jugador, o fuera de rango, no es válido aunque esté muy cerca en línea recta.
- Solo puede considerarse un candidato **activo** a la vez, incluso si varios cumplen la condición de detección.

## 7. Estabilidad del objetivo

- Mientras el objetivo actual siga cumpliendo las condiciones de detección (rango + zona frontal + habilitado), se mantiene como objetivo, aunque aparezca otro candidato de igual o mayor prioridad cerca.
- Solo se evalúa un reemplazo cuando el objetivo actual **deja de ser válido** (sale de rango, sale de la zona frontal, se deshabilita, o la escena lo destruye).
- Esta regla existe para evitar parpadeo del prompt y cambios de objetivo que se sientan erráticos al jugador.

## 8. Apertura y cierre

- Con un objetivo válido y el panel cerrado: pulsar Interact abre el panel de observación de ese objetivo, oculta el prompt, bloquea el movimiento y detiene la velocidad física.
- Con el panel abierto: pulsar Interact lo cierra y devuelve el control al jugador.
- La misma pulsación de teclado/botón que abrió el panel no debe poder cerrarlo en el mismo instante: tiene que mediar una pulsación **posterior**.
- Mientras el panel está abierto, Interact sigue siendo la única acción con efecto (no hay Cancel ni Submit en M2).

## 9. Casos especiales

- **Cambio de escena con panel abierto:** no ocurre en la práctica porque el movimiento está bloqueado y las transiciones de área requieren desplazamiento físico del jugador; aun así, ninguna UI ni estado de interacción sobrevive a un cambio de escena.
- **Objetivo se invalida con el panel abierto:** el panel, una vez abierto, no depende de que el objetivo se mantenga detectado; solo Interact (la pulsación posterior) lo cierra. La reevaluación de candidatos se retoma normalmente al cerrar.
- **Pulsaciones rápidas repetidas:** no deben abrir y cerrar el panel en una cascada accidental; la regla de "la pulsación que abre no cierra" ya cubre el caso más común, pero cualquier implementación debe verificarse contra pulsaciones muy rápidas y repetidas.
- **Ningún objetivo disponible:** el prompt permanece oculto y Interact no tiene efecto.
- **Objeto examinable deshabilitado en la escena:** no aparece como candidato ni genera prompt.

## 10. Pruebas funcionales

1. Aparece `[E] Examinar` solo cuando hay un objetivo válido en rango y en la zona frontal.
2. El prompt desaparece si el jugador se aleja o se da la vuelta.
3. Interact abre el panel del objetivo actual y oculta el prompt.
4. Al abrir el panel, el jugador no puede moverse ni cambiar de orientación.
5. La velocidad física del jugador queda en cero al abrir el panel.
6. La misma pulsación que abre el panel no lo cierra.
7. Una pulsación posterior de Interact cierra el panel y devuelve el control.
8. Con varios candidatos en rango, gana el de mayor prioridad; en empate, el más cercano; en empate total, el desempate es siempre el mismo objeto.
9. El objetivo activo no cambia mientras siga siendo válido, aunque aparezca otro candidato equivalente.
10. Cada escena (CamaraPreservacion, CorredorTecnico, ClaroExterior) funciona en aislamiento, con su propio interactuable, sin depender de estado de otra escena.
11. Ningún estado de interacción ni UI persiste al cambiar de escena.
12. `Time.timeScale` permanece en 1 durante todo el flujo.

## 11. Definition of Done (funcional)

M2 se considera funcionalmente terminado cuando:

- Los tres objetos graybox (Terminal de diagnóstico, Panel de mantenimiento, Nodo inactivo) son examinables en sus respectivas escenas.
- El bucle completo (detectar → prompt → abrir → leer → cerrar) funciona en las 4 direcciones cardinales.
- La selección de objetivo es estable y determinista, sin parpadeos.
- El movimiento y la orientación quedan bloqueados correctamente durante la observación, sin velocidad residual al cerrar.
- La apertura y el cierre respetan la regla de "no abrir y cerrar con la misma pulsación".
- Ninguna escena depende de otra para funcionar de forma aislada.
- Las pruebas funcionales de la sección 10 pasan en las tres escenas.
- No se han introducido NPCs, diálogo, inventario, guardado, ni ningún otro sistema fuera de alcance.

---

**FIN DEL GDD FUNCIONAL M2 v0.1.**
