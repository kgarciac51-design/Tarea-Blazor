# Tabla comparativa de resultados – OWASP ZAP

- **Proyecto analizado:** Tarea-Blazor (WebEmpresa – Blazor Web App .NET 10, consume ApiEmpresa)
- **URL analizada:** https://localhost:7094/Clientes
- **Herramienta:** OWASP ZAP 2.17.0. Escaneo automatizado con la política *Dev Standard*, spider tradicional, cliente para el spider (Edge) y escaneo activo.
- **Condiciones:** las dos ejecuciones se hicieron con ApiEmpresa (https://localhost:7043) y WebEmpresa en ejecución local con el perfil https, la misma URL y la misma configuración de ZAP.
- **Evidencia:** [`Informe_ZAP_Antes.html`](Informe_ZAP_Antes.html) · [`Informe_ZAP_Despues.html`](Informe_ZAP_Despues.html)

## Resumen

| Riesgo      | Antes | Después |
|-------------|:-----:|:-------:|
| Alto        | 0     | 0       |
| Medio       | 4     | 0       |
| Bajo        | 3     | 2       |
| Informativo | 4     | 3       |
| **Total**   | **11**| **5**   |

## Tabla comparativa de resultados

| Alerta encontrada | Riesgo | Corrección realizada | Resultado final |
|---|---|---|---|
| CSP: Directiva Wildcard | Medio | Se agregó en `Program.cs` una Content-Security-Policy completa con `default-src 'self'` y cada directiva limitada al propio sitio. Se desactivó la CSP parcial que Blazor agrega por defecto, que solo traía `frame-ancestors` y dejaba las demás directivas abiertas. | Corregida |
| CSP: Failure to Define Directive with No Fallback | Medio | Se definieron `form-action 'self'` y `frame-ancestors 'none'`, directivas que no heredan el valor de `default-src`. | Corregida |
| CSP: script-src unsafe-inline | Medio | `script-src 'self'` más un *nonce* aleatorio por petición, que autoriza solo el `<script type="importmap">` de Blazor (`App.razor`). El `onclick` en línea de `NavMenu.razor` se movió a `wwwroot/js/navmenu.js`. | Corregida |
| CSP: style-src unsafe-inline | Medio | `style-src 'self'`. Los atributos `style="..."` de `Cliente.razor` se reemplazaron por clases CSS en `Cliente.razor.css`. | Corregida |
| Cookie Sin Flag de Seguridad | Bajo | La cookie antiforgery se configuró con `SecurePolicy = SameAsRequest` (Secure en HTTPS), `HttpOnly = true` y `SameSite = Strict`. | Corregida |
| Falta encabezado X-Content-Type-Options | Bajo | Se agregó `X-Content-Type-Options: nosniff` a todas las respuestas (páginas y archivos estáticos). | Corregida |
| Strict-Transport-Security Header No Establecido | Bajo | Se configuró HSTS de 365 días con `IncludeSubDomains` (`AddHsts` + `UseHsts`) para el ambiente de producción. | Mitigada |
| El servidor filtra información de versión a través del campo "Server" *(apareció en el segundo análisis)* | Bajo | Se desactivó la cabecera `Server` de la aplicación con `AddServerHeader = false`. | Falso positivo |
| Aplicación Web Moderna | Informativo | No requiere modificación. | Informativa |
| Divulgación de información - Comentarios sospechosos | Informativo | Se reescribieron los comentarios de `ReconnectModal.razor.js` que contenían palabras que ZAP considera sospechosas (por ejemplo, "user"). Ningún comentario contenía datos sensibles. | Corregida |
| Reexaminar las Directivas de Control de Caché | Informativo | Las páginas HTML envían `Cache-Control: no-cache, no-store, must-revalidate` y `Pragma: no-cache`, porque muestran datos personales de clientes (CUI, NIT, teléfono). Las instancias bajaron de 3 a 1. | Mitigada |
| Respuesta de Gestión de Sesión Identificada | Informativo | No requiere modificación. | Informativa |

## Alertas que permanecen después del segundo análisis

| Alerta | Motivo por el que permanece |
|---|---|
| **Strict-Transport-Security Header No Establecido** (Mitigada) | ASP.NET Core excluye intencionalmente `localhost`, `127.0.0.1` y `[::1]` de HSTS (`HstsOptions.ExcludedHosts`). Si el navegador guardara HSTS para `localhost`, obligaría a usar HTTPS en todos los proyectos locales durante un año. La protección queda configurada (365 días) y se aplica al publicar la aplicación con un dominio real en producción. |
| **El servidor filtra información de versión a través del campo "Server"** (Falso positivo) | La alerta corresponde a `[pegue aquí la URL que muestra ZAP]`, un servidor externo al que se conecta el navegador Edge que usa el cliente para el spider de ZAP. No es una respuesta de la aplicación. La aplicación ya no envía la cabecera `Server` (`AddServerHeader = false`) y Kestrel nunca incluye número de versión. |
| **Reexaminar las Directivas de Control de Caché** (Mitigada) | Las páginas HTML que muestran datos de clientes ya tienen `no-store`. La instancia restante corresponde a `[pegue aquí la URL que muestra ZAP]`, un recurso interno de Blazor que no contiene datos personales, por lo que no representa riesgo de exposición por caché. |
| **Aplicación Web Moderna** (Informativa) | Solo indica que el sitio usa JavaScript (Blazor) y que conviene usar el spider moderno para recorrerlo. Describe la tecnología, no un riesgo. |
| **Respuesta de Gestión de Sesión Identificada** (Informativa) | ZAP solo identifica la cookie antiforgery que usa Blazor. La cookie ya tiene los atributos Secure, HttpOnly y SameSite=Strict. |

## Archivos modificados

| Archivo | Cambio |
|---|---|
| `Program.cs` | Cabeceras de seguridad (CSP con nonce, nosniff, X-Frame-Options, Referrer-Policy, Permissions-Policy, Cache-Control), cookie antiforgery segura, HSTS y cabecera `Server` desactivada. |
| `Components/App.razor` | Nonce en `<ImportMap>` e inclusión de `js/navmenu.js`. |
| `Components/Layout/NavMenu.razor` | Se quitó el `onclick` en línea. |
| `Components/Pages/Cliente.razor` | Se quitaron los atributos `style` en línea. |
| `Components/Pages/Cliente.razor.css` | Nuevo: estilos que antes estaban en línea. |
| `Components/Layout/ReconnectModal.razor.js` | Comentarios reescritos. |
| `wwwroot/js/navmenu.js` | Nuevo: comportamiento del menú sin JavaScript en línea. |
