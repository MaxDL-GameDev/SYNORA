# SYNORA — Informe de cierre de M3

> Cierre formal del hito **M3 — Criaturas y Ecosistema Vivo (Verak)**. Complementa `SYNORA_M3_Test_Report.md` con la evidencia de build final y validación fuera del Editor. No modifica SPEC, GDD ni decisiones de diseño.

## 1. Entorno

| Elemento | Valor |
|---|---|
| Editor | Unity 6000.5.3f1 (c2eb47b3a2a9) |
| Render Pipeline | Universal RP 2D |
| Plataforma | Windows StandaloneWindows64 (x86_64) |
| Backend de scripting | Mono (MonoBleedingEdge) |
| Fecha de cierre | 2026-07-22 |
| Base validada (HEAD) | `c14025a` (docs: add M3 QA report) |
| Escena de validación | `Assets/Scenes/ClaroExterior.unity` |

## 2. Resultado global

**M3 — Criaturas y Ecosistema Vivo: PASS.**

| Verificación | Resultado |
|---|---|
| Pruebas automatizadas EditMode | **139/139** · 0 failed · 0 skipped |
| QA manual A–F (fuera del Editor) | **PASS** |
| Build Windows | **Succeeded** |
| Validación fuera del Editor | **PASS** |
| Player.log | **Limpio** (0 errores/excepciones/referencias faltantes propias) |
| Incidencias abiertas | **Ninguna** |
| Consola del Editor | 0 errores / 0 warnings |
| Missing Scripts (escena y prefab) | 0 |

## 3. Build final

| Campo | Valor |
|---|---|
| Plataforma / arquitectura | StandaloneWindows64 / x86_64 |
| Backend | Mono |
| Development Build | OFF |
| Compresión | LZ4 |
| Ruta | `Builds/M3-Windows/SYNORA_M3.exe` |
| Duración | 54.06 s |
| Tamaño total | 109.4 MB |
| Resultado | `BuildResult.Succeeded` |
| Warnings / Errores del proceso | 0 / 0 |

Artefactos presentes: `SYNORA_M3.exe`, `SYNORA_M3_Data/`, `UnityPlayer.dll`, `MonoBleedingEdge/`, `UnityCrashHandler64.exe`, `dstorage*.dll`. La carpeta `Builds/` está ignorada por Git; ningún binario entra al repositorio.

## 4. Validación fuera del Editor (casos A–F)

Ejecutada por el Director sobre `SYNORA_M3.exe` (doble clic, fuera de Unity), playthrough de 5–10 min.

| Caso | Descripción | Resultado |
|---|---|---|
| A — Inicio | Ejecutable abre, arranque Bootstrap→ClaroExterior, Player/cámara/sprites/materiales, sin pantalla negra ni referencias faltantes | ✅ |
| B — Verak_A | Idle/Patrol, PingPong, cadencia natural, movimiento suave, respeta paredes | ✅ |
| C — Verak_B | Independiente (estado, índice y waypoints propios), sin interferir con Verak_A | ✅ |
| D — Alert | Entra en Alert, conserva facing de entrada, se detiene, no persigue, no rota al rodearlo, sale por loseRadius+linger, vuelve a Patrol | ✅ |
| E — Colisiones | Player atraviesa a Verak; Verak respeta paredes del Environment | ✅ |
| F — Estabilidad | Múltiples ciclos de Alert, sin congelamientos, sin cierres inesperados, cierre normal | ✅ |

## 5. Player.log

- Ruta revisada: `C:\Users\MaxDeLuna\AppData\LocalLow\SynoraDev\SYNORA Prototipo\Player.log`.
- Contenido normal de runtime (init de Mono/engine/D3D11/PhysX/Input System, descargas de assets entre escenas, cierre limpio).
- Única nota: `GarbageCollector disposing of ComputeBuffer` al cerrar — mensaje del finalizador de Unity/URP, **no propio** (no existe `ComputeBuffer` en el código del proyecto), no recurrente, no bloqueante.
- **0 errores propios, 0 excepciones, 0 referencias faltantes, 0 crashes.**

## 6. Deuda visual aceptada (MVP, no bloqueante)

1. **Clip Alert** = arte placeholder (frame estático).
2. **Baseline lateral** entre Idle-entrada y Walk: Left ~0.78u / Right ~0.41u.
3. **Vaivén horizontal leve en `Walk_Right`** ~0.19u (medido; documentado en el Test Report).

Conocidas, documentadas, exclusivamente visuales; no se corrigen en esta fase. No apareció deuda nueva. El sangrado de cola de `Walk_Right` quedó **resuelto** (ver Test Report, commit `7028b4e`).

## 7. Integridad estructural

Sin combate, persecución, huida, daño o vida; sin `Find`/`FindObjectOfType`, singleton nuevo, Service Locator, LINQ en runtime, AnimationEvent, StateMachineBehaviour, Root Motion ni coroutines en el presenter. Layer `Creatures` = 12; matriz de colisión: Player↔Creatures sin colisión, Creatures↔Environment con colisión, Player↔Environment intacto. Patrullas de Verak_A y Verak_B independientes.

## 8. Tag

- Tag previsto: **`m3-complete`** (anotado), apuntando al commit de cierre de esta fase.
- `m0-complete`, `m1-complete`, `m2-complete` permanecen intactos.

---

**M3 — Criaturas y Ecosistema Vivo: COMPLETE.**
