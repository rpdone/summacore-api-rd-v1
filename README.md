# SummaCore DGII API v1.0

## 🎯 Descripción
API REST completa para facturación electrónica según normativas de la DGII (Dirección General de Impuestos Internos) de República Dominicana.

**Tecnologías:**
- C# 14
- .NET 10 LTS
- REST API con XML
- Firma Digital SHA-256

---

## 📋 Tabla de Contenidos
1. [Arquitectura](#arquitectura)
2. [Configuración](#configuración)
3. [Funcionalidades Implementadas](#funcionalidades-implementadas)
4. [Endpoints](#endpoints)
5. [Modelos XML](#modelos-xml)
6. [Flujo de Certificación](#flujo-de-certificación)
7. [Deployment](#deployment)
8. [Testing](#testing)

---

## 🏗️ Arquitectura

### Componentes Principales

```
SummaCore/
├── Configuration/
│   └── DgiiEnvironmentConfig.cs      # Configuración multi-ambiente
├── Controllers/
│   ├── CargaMasivaController.cs      # Carga masiva Excel
│   ├── DGIIController.cs             # Receptor endpoints
│   └── PruebasController.cs          # Testing utilities
├── Models/
│   ├── ECF_Modelos.cs                # Modelo e-CF principal
│   ├── ARECF.cs                      # Acuse de Recibo
│   ├── ACECF.cs                      # Aprobación Comercial
│   ├── ANECF.cs                      # Anulación
│   └── RFCE.cs                       # Resumen Consumo
├── Services/
│   ├── DgiiService.cs                # Cliente DGII (Emisor)
│   └── SignerService.cs              # Firma digital SHA-256
└── Validation/
    ├── ECF_Validador.cs              # Validador de reglas
    └── ECF_Reglas.cs                 # Matriz de obligatoriedad
```

---

## ⚙️ Configuración

### 1. Ambientes DGII

Editar `appsettings.json`:

```json
{
  "Dgii": {
    "Environment": "Certification",  // PreCertification | Certification | Production
    "Rnc": "131487272",
    "RazonSocial": "TU EMPRESA SRL",
    "CertificatePath": "13049552_identity.p12",
    "CertificatePassword": "tu_password"
  }
}
```

### 2. URLs por Ambiente

**Pre-Certificación (Testing):**
- Authentication: `https://ecf.dgii.gov.do/testecf/autenticacion`
- Reception: `https://ecf.dgii.gov.do/testecf/recepcion`

**Certificación:**
- Authentication: `https://ecf.dgii.gov.do/certecf/autenticacion`
- Reception: `https://ecf.dgii.gov.do/CerteCF/Recepcion`

**Producción:**
- Authentication: `https://ecf.dgii.gov.do/ecf/autenticacion`
- Reception: `https://ecf.dgii.gov.do/ecf/recepcion`

### 3. Certificado Digital

Coloca tu archivo `.p12` en la raíz del proyecto y asegúrate de que `CopyToOutputDirectory` esté configurado en `Always` en el `.csproj`:

```xml
<ItemGroup>
  <None Update="13049552_identity.p12">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## ✅ Funcionalidades Implementadas

### ✓ Fase 1: Core Services
- [x] Firma Digital SHA-256 (SignerService)
- [x] Autenticación DGII (Semilla + Token)
- [x] Soporte multi-ambiente

### ✓ Fase 2: Servicios Emisor
- [x] Enviar e-CF (todos los tipos ≥ $250k)
- [x] Enviar RFCE (Consumo < $250k)
- [x] Consultar Estado por TrackID
- [x] Consultar Estado por RNC + e-NCF
- [x] Enviar Aprobación Comercial (ACECF)
- [x] Anular e-NCF (ANECF)
- [x] Consultar Directorio
- [x] Consultar Estado del Servicio

### ✓ Fase 3: Servicios Receptor
- [x] Endpoint Autenticación (`/fe/autenticacion/api/semilla`)
- [x] Endpoint Recepción (`/fe/recepcion/api/ecf`)
- [x] Endpoint Aprobación Comercial (`/fe/aprobacioncomercial/api/ecf`)
- [x] Generación de ARECF firmado
- [x] Validación de XML y firma digital

### ✓ Fase 4: Utilidades
- [x] Carga masiva desde Excel
- [x] Validación según matriz de obligatoriedad
- [x] Sanitización automática de campos

---

## 🔌 Endpoints

### Emisor (Envío a DGII)

#### 1. Carga Masiva Excel
```http
POST /api/CargaMasiva/procesar-plantilla-dgii
Content-Type: multipart/form-data

Form Data:
- archivoExcel: [archivo.xlsx]
```

**Respuesta:**
```json
{
  "total": 10,
  "exitosos": 8,
  "fallidos": 2,
  "resultados": [
    {
      "fila": 2,
      "encf": "E310000000001",
      "tipoECF": 31,
      "estado": "ENVIADO",
      "trackId": "abc-123-xyz"
    }
  ]
}
```

#### 2. Consultar Estado por Lote
```http
POST /api/pruebas/consultar-estatus-lote
Content-Type: application/json

["trackId1", "trackId2", "trackId3"]
```

### Receptor (Recepción desde Emisores Externos)

#### 3. Recibir e-CF
```http
POST /fe/recepcion/api/ecf
Content-Type: application/xml

[XML firmado del e-CF]
```

**Respuesta:** ARECF firmado (XML)

#### 4. Recibir Aprobación Comercial
```http
POST /fe/aprobacioncomercial/api/ecf
Content-Type: application/xml

[XML firmado del ACECF]
```

**Respuesta:**
```json
{
  "mensaje": "Aprobación comercial procesada exitosamente",
  "encf": "E310000000001",
  "estado": "Aprobado"
}
```

#### 5. Obtener Semilla (Autenticación)
```http
GET /fe/autenticacion/api/semilla
```

**Respuesta:** XML con semilla

---

## 📄 Modelos XML

### e-CF (Factura Electrónica)

```xml
<ECF>
  <Encabezado>
    <Version>1.0</Version>
    <IdDoc>
      <TipoeCF>31</TipoeCF>
      <eNCF>E310000000001</eNCF>
      <FechaVencimientoSecuencia>31-12-2025</FechaVencimientoSecuencia>
      <TipoIngresos>01</TipoIngresos>
      <TipoPago>1</TipoPago>
    </IdDoc>
    <Emisor>
      <RNCEmisor>131487272</RNCEmisor>
      <RazonSocialEmisor>EMPRESA PRUEBA SRL</RazonSocialEmisor>
      <FechaEmision>2025-11-23</FechaEmision>
    </Emisor>
    <Comprador>
      <RNCComprador>101796361</RNCComprador>
      <RazonSocialComprador>CLIENTE PRUEBA</RazonSocialComprador>
    </Comprador>
    <Totales>
      <MontoGravadoTotal>1000.00</MontoGravadoTotal>
      <TotalITBIS>180.00</TotalITBIS>
      <MontoTotal>1180.00</MontoTotal>
    </Totales>
  </Encabezado>
  <DetallesItems>
    <Item>
      <NumeroLinea>1</NumeroLinea>
      <NombreItem>SERVICIO</NombreItem>
      <MontoItem>1000.00</MontoItem>
    </Item>
  </DetallesItems>
  <FechaHoraFirma>2025-11-23T10:30:00</FechaHoraFirma>
  <Signature>...</Signature>
</ECF>
```

### ARECF (Acuse de Recibo)

```xml
<ARECF>
  <Version>1.0</Version>
  <RNCEmisor>131487272</RNCEmisor>
  <RNCComprador>101796361</RNCComprador>
  <eNCF>E310000000001</eNCF>
  <FechaEmision>2025-11-23</FechaEmision>
  <FechaRecepcion>2025-11-23T10:35:00</FechaRecepcion>
  <Estado>1</Estado>
  <CodigoSeguridadAR>AB12CD34</CodigoSeguridadAR>
  <FechaHoraFirma>2025-11-23T10:35:00</FechaHoraFirma>
  <Signature>...</Signature>
</ARECF>
```

### Tipos de e-CF Soportados

| Tipo | Descripción | RNC Comprador |
|------|-------------|---------------|
| 31 | Factura Crédito Fiscal | Obligatorio |
| 32 | Factura Consumo | Opcional |
| 33 | Nota de Débito | Opcional |
| 34 | Nota de Crédito | Opcional |
| 41 | Compras | Obligatorio |
| 43 | Gastos Menores | No aplica |
| 44 | Regímenes Especiales | Obligatorio |
| 45 | Gubernamental | Obligatorio |
| 46 | Exportaciones | Opcional |
| 47 | Pagos al Exterior | Obligatorio |

---

## 🔄 Flujo de Certificación DGII

### Pasos Obligatorios

1. **Pre-Certificación** (Ambiente de prueba)
   - Enviar 10 e-CF de cada tipo (31, 32, 33, 34, etc.)
   - Validar respuestas trackId

2. **Certificación Técnica**
   - Probar endpoints Receptor
   - DGII enviará e-CF de prueba a `/fe/recepcion/api/ecf`
   - Tu API debe responder con ARECF válido

3. **Aprobación Comercial**
   - DGII enviará ACECF a `/fe/aprobacioncomercial/api/ecf`
   - Responder HTTP 200 o 400

4. **Producción**
   - Cambiar ambiente a `Production` en `appsettings.json`
   - Obtener secuencias NCF oficiales

### URLs para Registrar en DGII

Durante la certificación, debes proporcionar:

```
URL Recepción:
https://tu-dominio.com/fe/recepcion/api/ecf

URL Aprobación Comercial:
https://tu-dominio.com/fe/aprobacioncomercial/api/ecf

URL Autenticación (opcional):
https://tu-dominio.com/fe/autenticacion/api/semilla
```

---

## 🚀 Deployment

### Azure App Service

1. **Publicar desde VS Code:**
   - Abrir Command Palette (`Ctrl+Shift+P`)
   - Seleccionar `Azure App Service: Deploy to Web App`
   - Elegir tu App Service

2. **Variables de Entorno:**
   ```
   Dgii__Environment=Production
   Dgii__Rnc=131487272
   Dgii__CertificatePassword=tu_password_seguro
   ```

3. **Configurar SSL:**
   - Tu dominio debe tener certificado SSL válido
   - DGII requiere HTTPS obligatorio

### Docker (Opcional)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY bin/Release/net10.0/publish/ .
ENTRYPOINT ["dotnet", "SummaCore.dll"]
```

---

## 🧪 Testing

### 1. Test Local

```bash
# Iniciar API
dotnet run

# Abrir testecf.html en navegador
# Seleccionar archivo Excel
# Hacer clic en "Subir y Procesar"
```

### 2. Test Endpoints Receptor

**PowerShell:**
```powershell
# Test GET Semilla
Invoke-WebRequest -Uri "https://localhost:7186/fe/autenticacion/api/semilla"

# Test POST Recepción
$body = Get-Content "factura_firmada.xml" -Raw
Invoke-WebRequest -Uri "https://localhost:7186/fe/recepcion/api/ecf" `
  -Method POST -Body $body -ContentType "application/xml"
```

**cURL:**
```bash
# Test GET Semilla
curl https://localhost:7186/fe/autenticacion/api/semilla

# Test POST Recepción
curl -X POST https://localhost:7186/fe/recepcion/api/ecf \
  -H "Content-Type: application/xml" \
  --data @factura_firmada.xml
```

### 3. Plantilla Excel

Descargar plantilla oficial DGII y configurar:

**Columnas obligatorias:**
- C: TipoeCF (31, 32, 33, etc.)
- D: e-NCF (E310000000001)
- Columnas 76-90: Totales
- Columnas 100+: Items (hasta 100 líneas)

---

## 📊 Matriz de Obligatoriedad

El sistema implementa validación automática según tipo de e-CF:

| Campo | E31 | E32 | E33 | E34 | E41 | E43 | E44 | E45 | E46 | E47 |
|-------|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|
| FechaVencimientoSecuencia | 1 | 0 | 1 | 0 | 1 | 1 | 1 | 1 | 1 | 1 |
| RNCComprador | 1 | 2 | 2 | 2 | 1 | 0 | 1 | 1 | 2 | 3 |
| TipoIngresos | 1 | 1 | 1 | 1 | 1 | 0 | 1 | 1 | 1 | 0 |
| MontoGravadoTotal | 2 | 2 | 2 | 2 | 0 | 0 | 2 | 2 | 2 | 0 |

**Leyenda:**
- 0 = No corresponde (se elimina)
- 1 = Obligatorio
- 2 = Condicional (depende de otros campos)
- 3 = Opcional

---

## 🔐 Seguridad

### Firma Digital

El servicio implementa firma XML según:
- **Algoritmo Canonicalización:** `http://www.w3.org/TR/2001/REC-xml-c14n-20010315`
- **Algoritmo Firma:** `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256`
- **Algoritmo Digest:** `http://www.w3.org/2001/04/xmlenc#sha256`
- **Reference URI:** Vacío (firma todo el documento)
- **PreserveWhitespace:** false

### Certificados

- Usar certificados P12 emitidos por Cámara de Comercio (CCPSD)
- Renovar anualmente
- Proteger contraseña en Azure Key Vault (producción)

---

## 📞 Soporte

**DGII República Dominicana:**
- Portal: https://dgii.gov.do
- Soporte e-CF: ecf@dgii.gov.do
- Documentación: https://dgii.gov.do/ecf/Paginas/default.aspx

**SummaCore:**
- Repositorio: `rpdone/summacore-api-rd-v1`
- Issues: GitHub Issues

---

## 📝 Licencia

Propiedad de SummaGroup Malaysia. Uso interno.

---

## 🔄 Changelog

### v1.0.0 (2025-11-23)
- ✅ Implementación completa Fase 1-4
- ✅ Soporte multi-ambiente (Pre-Cert, Cert, Prod)
- ✅ Todos los endpoints Emisor/Receptor
- ✅ Validación automática según matriz DGII
- ✅ Carga masiva Excel
- ✅ Generación ARECF automática

---

**¡Listo para Certificación DGII! 🎉**