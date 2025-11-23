# 📋 DGII Certification Checklist

## Pre-Requisitos

### ✅ Documentación
- [ ] RNC de la empresa
- [ ] Certificado digital P12 (.p12)
- [ ] Contraseña del certificado
- [ ] Carta de solicitud dirigida a DGII
- [ ] Resolución de autorización de secuencias NCF

### ✅ Infraestructura
- [ ] Dominio público con SSL (HTTPS obligatorio)
- [ ] API deployada y accesible desde internet
- [ ] Certificado SSL válido y sin errores
- [ ] Firewall configurado (permitir IPs de DGII)

### ✅ Configuración del Sistema
- [ ] `appsettings.json` configurado con datos reales
- [ ] Certificado P12 incluido en deployment
- [ ] Variables de entorno configuradas en Azure
- [ ] Logging activado (Application Insights recomendado)

---

## Fase 1: Pre-Certificación (Testing)

**Ambiente:** https://ecf.dgii.gov.do/testecf/

### Paso 1: Configurar Ambiente
```json
{
  "Dgii": {
    "Environment": "PreCertification"
  }
}
```

### Paso 2: Pruebas de Emisor
Enviar **10 comprobantes de CADA tipo**:

- [ ] 10 x Tipo 31 (Factura Crédito Fiscal)
- [ ] 10 x Tipo 32 (Factura Consumo)
- [ ] 10 x Tipo 33 (Nota de Débito)
- [ ] 10 x Tipo 34 (Nota de Crédito)
- [ ] 10 x Tipo 41 (Compras)
- [ ] 10 x Tipo 43 (Gastos Menores)
- [ ] 10 x Tipo 44 (Regímenes Especiales)
- [ ] 10 x Tipo 45 (Gubernamental)

**Verificar:**
- [ ] Todos recibieron `trackId` válido
- [ ] Sin errores de validación
- [ ] Consulta de estado funciona
- [ ] XMLs firmados correctamente

### Paso 3: Pruebas de Receptor (Opcional en Pre-Cert)
- [ ] Endpoint `/fe/recepcion/api/ecf` responde
- [ ] ARECF generado correctamente
- [ ] Firma digital válida en ARECF

---

## Fase 2: Certificación Oficial

**Ambiente:** https://ecf.dgii.gov.do/certecf/

### Paso 1: Solicitud Formal a DGII
Enviar a **ecf@dgii.gov.do**:

```
Asunto: Solicitud de Certificación e-CF - RNC [TU_RNC]

Adjuntar:
1. Carta de solicitud (membrete de la empresa)
2. Formulario de registro (descargar de portal DGII)
3. Datos técnicos:
   - URL Recepción: https://tu-dominio.com/fe/recepcion/api/ecf
   - URL Aprobación: https://tu-dominio.com/fe/aprobacioncomercial/api/ecf
   - URL Autenticación: https://tu-dominio.com/fe/autenticacion/api/semilla
   - Contacto técnico (nombre, email, teléfono)
```

### Paso 2: Cambiar a Ambiente Certificación
```json
{
  "Dgii": {
    "Environment": "Certification"
  }
}
```

### Paso 3: Pruebas Emisor en Certificación
Repetir envío de comprobantes:
- [ ] 10 x Tipo 31
- [ ] 10 x Tipo 32
- [ ] 10 x Tipo 33
- [ ] 10 x Tipo 34

**Validar:**
- [ ] Sin errores en ambiente Certificación
- [ ] TrackIDs recibidos
- [ ] Consultas funcionan

### Paso 4: Pruebas Receptor (CRÍTICO)
DGII enviará comprobantes de prueba a tu endpoint:

#### 4.1 Verificar Endpoint Activo
```bash
# Desde máquina externa, verificar que responde:
curl https://tu-dominio.com/fe/recepcion/api/ecf \
  -X POST \
  -H "Content-Type: application/xml" \
  --data "<ECF>...</ECF>"
```

#### 4.2 Monitorear Logs
- [ ] Activar logging detallado
- [ ] Verificar que recibes POSTs desde IPs de DGII
- [ ] Revisar ARECF generados

#### 4.3 Validaciones Receptor
DGII verificará:
- [ ] XML firmado correctamente (SHA-256)
- [ ] ARECF retornado en < 5 segundos
- [ ] Validación de estructura correcta
- [ ] Código de seguridad único por ARECF
- [ ] Fechas en formato correcto (yyyy-MM-ddTHH:mm:ss)

### Paso 5: Aprobación Comercial (CRÍTICO)
DGII enviará ACECF a tu endpoint:

```bash
POST https://tu-dominio.com/fe/aprobacioncomercial/api/ecf
```

**Tu API debe:**
- [ ] Retornar HTTP 200 si todo OK
- [ ] Retornar HTTP 400 si hay errores
- [ ] Procesar en < 3 segundos
- [ ] Guardar en base de datos

### Paso 6: Pruebas de Anulación
- [ ] Crear e-CF de prueba
- [ ] Anular mediante ANECF
- [ ] Verificar estado = "Anulado"

### Paso 7: Consultas Avanzadas
- [ ] Consultar por TrackID
- [ ] Consultar por RNC + e-NCF + Código
- [ ] Consultar directorio
- [ ] Verificar estatus del servicio

---

## Fase 3: Aprobación y Producción

### Paso 1: Esperar Aprobación DGII
DGII revisará manualmente:
- [ ] Todos los logs de transacciones
- [ ] Estructura de XMLs enviados
- [ ] Tiempos de respuesta
- [ ] Validaciones implementadas

**Tiempo estimado:** 5-10 días hábiles

### Paso 2: Notificación de Aprobación
Recibirás email de DGII confirmando:
- [ ] Certificación aprobada
- [ ] Autorización para usar secuencias oficiales
- [ ] Instrucciones para activar producción

### Paso 3: Configurar Producción
```json
{
  "Dgii": {
    "Environment": "Production",
    "Rnc": "TU_RNC_REAL",
    "RazonSocial": "NOMBRE_EMPRESA_OFICIAL"
  }
}
```

### Paso 4: Obtener Secuencias NCF Oficiales
- [ ] Solicitar secuencias en Portal DGII
- [ ] Configurar rangos en tu sistema
- [ ] Formato: E31XXXXXXXXX (11 dígitos)

### Paso 5: Go Live
- [ ] Deploy a producción
- [ ] Prueba con 1 factura real
- [ ] Verificar recepción correcta
- [ ] Monitorear primeras 24 horas

---

## Monitoreo Post-Certificación

### Diario
- [ ] Verificar logs de errores
- [ ] Revisar rechazos de DGII
- [ ] Consultar estatus del servicio DGII

### Semanal
- [ ] Revisar tasa de éxito/fallo
- [ ] Validar tiempos de respuesta
- [ ] Backup de certificados

### Mensual
- [ ] Revisar vencimiento de certificado P12
- [ ] Análisis de volumen de transacciones
- [ ] Actualización de documentación

---

## Troubleshooting Común

### Error: "Firma digital inválida"
**Causa:** Certificado incorrecto o algoritmo SHA-1 en lugar de SHA-256
**Solución:**
```csharp
// Verificar en SignerService.cs:
signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
```

### Error: "eNCF duplicado"
**Causa:** Ya enviaste ese e-NCF antes
**Solución:** 
- Incrementar secuencia
- Verificar base de datos de NCFs usados

### Error: "RNC Comprador inválido"
**Causa:** RNC no existe en base DGII o formato incorrecto
**Solución:**
- Validar formato: 9 u 11 dígitos
- Consultar en portal DGII si RNC existe

### Error: "Timeout conexión"
**Causa:** DGII no responde o tu firewall bloquea
**Solución:**
- Verificar conectividad: `ping ecf.dgii.gov.do`
- Revisar reglas de firewall
- Aumentar timeout en HttpClient

### Error: "XML no cumple XSD"
**Causa:** Estructura XML no coincide con esquema
**Solución:**
- Validar contra XSD oficial
- Revisar campos obligatorios según tipo
- Eliminar campos con valor `null`

---

## Contactos DGII

**Soporte Técnico e-CF:**
- Email: ecf@dgii.gov.do
- Teléfono: +1 (809) 689-2181 ext. 2500
- Horario: Lunes-Viernes 8:00 AM - 4:00 PM

**Portal DGII:**
- https://dgii.gov.do
- https://dgii.gov.do/ecf/

**Oficina Virtual:**
- https://www.dgii.gov.do/app/WebApp/
- Login con usuario y contraseña

---

## Checklist Final Pre-Producción

### Seguridad
- [ ] Certificados SSL válidos y no expirados
- [ ] Contraseñas en Azure Key Vault
- [ ] IP whitelisting configurado
- [ ] Logs sin información sensible

### Performance
- [ ] API responde en < 2 segundos
- [ ] Caché de tokens implementado
- [ ] Connection pooling activo
- [ ] Rate limiting configurado

### Backup
- [ ] Base de datos respaldada diariamente
- [ ] Certificados P12 guardados en lugar seguro
- [ ] Documentación actualizada
- [ ] Plan de recuperación ante desastres

### Monitoreo
- [ ] Application Insights activo
- [ ] Alertas configuradas
- [ ] Dashboard de métricas
- [ ] Logs centralizados

---

## ✅ Estado de Certificación

```
[ ] Pre-Certificación Iniciada
[ ] Pre-Certificación Completada
[ ] Solicitud Formal Enviada
[ ] Certificación en Proceso
[ ] Pruebas Receptor Pasadas
[ ] Aprobación Comercial OK
[ ] Certificación Aprobada
[ ] Producción Activa
```

**Fecha Inicio:** _______________
**Fecha Estimada Go-Live:** _______________
**Responsable Técnico:** _______________
**Contacto DGII:** _______________

---

**¡Éxito en tu Certificación! 🚀**