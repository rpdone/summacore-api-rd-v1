using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SummaCore.Models
{
    [XmlRoot("ECF")]
    public class ECF
    {
        [XmlElement(Order = 1)] public Encabezado Encabezado { get; set; } = new Encabezado();
        [XmlElement("DetallesItems", Order = 2)] public DetallesItems DetallesItems { get; set; } = new DetallesItems();
        [XmlElement(Order = 3)] public Subtotales Subtotales { get; set; } = new Subtotales();
        [XmlElement(Order = 4)] public DescuentosORecargos DescuentosORecargos { get; set; }
        [XmlElement(Order = 5)] public Paginacion Paginacion { get; set; }
        [XmlElement(Order = 6)] public InformacionReferencia InformacionReferencia { get; set; }
        [XmlElement(Order = 7)] public List<TablaAdicional> TablaAdicional { get; set; }
        [XmlElement(Order = 8)] public string FechaHoraFirma { get; set; }
        [XmlElement(Order = 99)] public Signature Signature { get; set; }

        public bool ShouldSerializeDetallesItems() => DetallesItems?.Item != null && DetallesItems.Item.Any();
        public bool ShouldSerializeSubtotales() => Subtotales?.Subtotal != null && Subtotales.Subtotal.Any();
        public bool ShouldSerializeDescuentosORecargos() => DescuentosORecargos?.DescuentoORecargo != null && DescuentosORecargos.DescuentoORecargo.Any();
        public bool ShouldSerializePaginacion() => Paginacion?.Pagina != null && Paginacion.Pagina.Any();
        public bool ShouldSerializeInformacionReferencia() => InformacionReferencia != null && !string.IsNullOrEmpty(InformacionReferencia.NCFModificado);
        public bool ShouldSerializeTablaAdicional() => TablaAdicional != null && TablaAdicional.Any();
        public bool ShouldSerializeFechaHoraFirma() => !string.IsNullOrEmpty(FechaHoraFirma);
    }

    public class Encabezado
    {
        [XmlElement(Order = 1)] public VersionType Version { get; set; } = new VersionType();
        [XmlElement(Order = 2)] public IdDoc IdDoc { get; set; } = new IdDoc();
        [XmlElement(Order = 3)] public Emisor Emisor { get; set; } = new Emisor();
        [XmlElement(Order = 4)] public Comprador Comprador { get; set; } = new Comprador();
        [XmlElement(Order = 5)] public InformacionesAdicionales InformacionesAdicionales { get; set; }
        [XmlElement(Order = 6)] public Transporte Transporte { get; set; }
        [XmlElement(Order = 7)] public Totales Totales { get; set; } = new Totales();
        [XmlElement(Order = 8)] public OtraMoneda OtraMoneda { get; set; }
        [XmlElement(Order = 9)] public string CodigoSeguridadeCF { get; set; }

        public bool ShouldSerializeComprador() => Comprador != null && !string.IsNullOrEmpty(Comprador.RNCComprador);
        public bool ShouldSerializeInformacionesAdicionales() => InformacionesAdicionales != null;
        public bool ShouldSerializeTransporte() => Transporte != null;
        public bool ShouldSerializeOtraMoneda() => OtraMoneda != null;
        public bool ShouldSerializeCodigoSeguridadeCF() => !string.IsNullOrEmpty(CodigoSeguridadeCF);
    }

    public class IdDoc
    {
        private string _tipoIngresos;

        [XmlElement(Order = 1)] public int TipoeCF { get; set; }
        [XmlElement(Order = 2)] public string eNCF { get; set; }
        [XmlElement(Order = 3)] public string FechaVencimientoSecuencia { get; set; }
        [XmlElement(Order = 4)] public int? IndicadorNotaCredito { get; set; }
        [XmlElement(Order = 5)] public int? IndicadorEnvioDiferido { get; set; }
        [XmlElement(Order = 6)] public int? IndicadorMontoGravado { get; set; }
        [XmlElement(Order = 7)] public int? IndicadorServicioTodoIncluido { get; set; }

        [XmlIgnore]
        public string TipoIngresosRaw
        {
            get => _tipoIngresos;
            set => _tipoIngresos = (value == "#e" || value == "0" || string.IsNullOrEmpty(value)) ? null : value.PadLeft(2, '0');
        }

        [XmlElement("TipoIngresos", Order = 8)]
        public string TipoIngresos
        {
            get => _tipoIngresos;
            set => _tipoIngresos = value;
        }

        [XmlElement(Order = 9)] public int? TipoPago { get; set; }
        [XmlElement(Order = 10)] public string FechaLimitePago { get; set; }
        [XmlElement(Order = 11)] public string TerminoPago { get; set; }
        
        [XmlArray(Order = 12)]
        [XmlArrayItem("FormaDePago")]
        public List<FormaPagoItem> TablaFormasPago { get; set; }

        [XmlElement(Order = 13)] public string TipoCuentaPago { get; set; }
        [XmlElement(Order = 14)] public string NumeroCuentaPago { get; set; }
        [XmlElement(Order = 15)] public string BancoPago { get; set; }
        [XmlElement(Order = 16)] public string FechaDesde { get; set; }
        [XmlElement(Order = 17)] public string FechaHasta { get; set; }
        [XmlElement(Order = 18)] public int? TotalPaginas { get; set; }

        public bool ShouldSerializeFechaVencimientoSecuencia() => !string.IsNullOrEmpty(FechaVencimientoSecuencia) && FechaVencimientoSecuencia != "#e" && TipoeCF != 32;
        public bool ShouldSerializeIndicadorNotaCredito() => IndicadorNotaCredito.HasValue && TipoeCF == 34;
        public bool ShouldSerializeIndicadorEnvioDiferido() => IndicadorEnvioDiferido.HasValue && IndicadorEnvioDiferido == 1;
        
        public bool ShouldSerializeIndicadorMontoGravado() 
        {
            if (TipoeCF == 46 || TipoeCF == 47) return false;
            return IndicadorMontoGravado.HasValue;
        }

        public bool ShouldSerializeIndicadorServicioTodoIncluido() => IndicadorServicioTodoIncluido.HasValue && IndicadorServicioTodoIncluido == 1;
        public bool ShouldSerializeTipoIngresos() => !string.IsNullOrEmpty(_tipoIngresos);
        public bool ShouldSerializeTipoPago() => TipoPago.HasValue && TipoPago > 0;
        public bool ShouldSerializeFechaLimitePago() => !string.IsNullOrEmpty(FechaLimitePago) && FechaLimitePago != "#e";
        public bool ShouldSerializeTerminoPago() => !string.IsNullOrEmpty(TerminoPago) && TerminoPago != "#e";
        public bool ShouldSerializeTablaFormasPago() => TablaFormasPago != null && TablaFormasPago.Any();
        public bool ShouldSerializeTipoCuentaPago() => !string.IsNullOrEmpty(TipoCuentaPago) && TipoCuentaPago != "#e";
        public bool ShouldSerializeNumeroCuentaPago() => !string.IsNullOrEmpty(NumeroCuentaPago) && NumeroCuentaPago != "#e";
        public bool ShouldSerializeBancoPago() => !string.IsNullOrEmpty(BancoPago) && BancoPago != "#e";
        public bool ShouldSerializeTotalPaginas() => TotalPaginas.HasValue;
        public bool ShouldSerializeFechaDesde() => !string.IsNullOrEmpty(FechaDesde);
        public bool ShouldSerializeFechaHasta() => !string.IsNullOrEmpty(FechaHasta);
    }

    public class FormaPagoItem {
        [XmlElement(Order = 1)] public string FormaPago { get; set; }
        [XmlElement(Order = 2)] public decimal MontoPago { get; set; }
    }

    public class Emisor {
        [XmlElement(Order = 1)] public string RNCEmisor { get; set; }
        [XmlElement(Order = 2)] public string RazonSocialEmisor { get; set; }
        [XmlElement(Order = 3)] public string NombreComercial { get; set; }
        [XmlElement(Order = 4)] public string Sucursal { get; set; }
        [XmlElement(Order = 5)] public string DireccionEmisor { get; set; }
        [XmlElement(Order = 6)] public string Municipio { get; set; }
        [XmlElement(Order = 7)] public string Provincia { get; set; }
        [XmlElement(Order = 8)] public List<string> TablaTelefonoEmisor { get; set; }
        [XmlElement(Order = 9)] public string CorreoEmisor { get; set; }
        [XmlElement(Order = 10)] public string WebSite { get; set; }
        [XmlElement(Order = 11)] public string ActividadEconomica { get; set; }
        [XmlElement(Order = 12)] public string CodigoVendedor { get; set; }
        [XmlElement(Order = 13)] public string NumeroFacturaInterna { get; set; }
        [XmlElement(Order = 14)] public string NumeroPedidoInterno { get; set; }
        [XmlElement(Order = 15)] public string ZonaVenta { get; set; }
        [XmlElement(Order = 16)] public string RutaVenta { get; set; }
        [XmlElement(Order = 17)] public string InformacionAdicionalEmisor { get; set; }
        [XmlElement(Order = 18)] public string FechaEmision { get; set; }

        public bool ShouldSerializeNombreComercial() => !string.IsNullOrEmpty(NombreComercial) && NombreComercial != "#e";
        public bool ShouldSerializeSucursal() => !string.IsNullOrEmpty(Sucursal) && Sucursal != "#e";
        public bool ShouldSerializeWebSite() => !string.IsNullOrEmpty(WebSite) && WebSite != "#e";
        public bool ShouldSerializeCodigoVendedor() => !string.IsNullOrEmpty(CodigoVendedor) && CodigoVendedor != "#e";
        public bool ShouldSerializeInformacionAdicionalEmisor() => !string.IsNullOrEmpty(InformacionAdicionalEmisor) && InformacionAdicionalEmisor != "#e";
    }

    public class Comprador {
        [XmlElement(Order = 1)] public string RNCComprador { get; set; }
        [XmlElement(Order = 2)] public string IdentificadorExtranjero { get; set; }
        [XmlElement(Order = 3)] public string RazonSocialComprador { get; set; }
        [XmlElement(Order = 4)] public string ContactoComprador { get; set; }
        [XmlElement(Order = 5)] public string CorreoComprador { get; set; }
        [XmlElement(Order = 6)] public string DireccionComprador { get; set; }
        [XmlElement(Order = 7)] public string MunicipioComprador { get; set; }
        [XmlElement(Order = 8)] public string ProvinciaComprador { get; set; }
        [XmlElement(Order = 9)] public string PaisComprador { get; set; }
        [XmlElement(Order = 10)] public string FechaEntrega { get; set; }
        [XmlElement(Order = 11)] public string ContactoEntrega { get; set; }
        [XmlElement(Order = 12)] public string DireccionEntrega { get; set; }
        [XmlElement(Order = 13)] public string TelefonoAdicional { get; set; }
        [XmlElement(Order = 14)] public string FechaOrdenCompra { get; set; }
        [XmlElement(Order = 15)] public string NumeroOrdenCompra { get; set; }
        [XmlElement(Order = 16)] public string CodigoInternoComprador { get; set; }
        [XmlElement(Order = 17)] public string ResponsablePago { get; set; }
        [XmlElement(Order = 18)] public string InformacionAdicionalComprador { get; set; }

        public bool ShouldSerializeRNCComprador() => !string.IsNullOrEmpty(RNCComprador) && RNCComprador != "#e";
        public bool ShouldSerializeIdentificadorExtranjero() => !string.IsNullOrEmpty(IdentificadorExtranjero) && IdentificadorExtranjero != "#e";
        public bool ShouldSerializePaisComprador() => !string.IsNullOrEmpty(PaisComprador) && PaisComprador != "#e";
        public bool ShouldSerializeFechaEntrega() => !string.IsNullOrEmpty(FechaEntrega) && FechaEntrega != "#e";
    }

    public class Totales
    {
        [XmlElement(Order = 1)] public decimal? MontoGravadoTotal { get; set; }
        [XmlElement(Order = 2)] public decimal? MontoGravadoI1 { get; set; }
        [XmlElement(Order = 3)] public decimal? MontoGravadoI2 { get; set; }
        [XmlElement(Order = 4)] public decimal? MontoGravadoI3 { get; set; }
        [XmlElement(Order = 5)] public decimal? MontoExento { get; set; }
        [XmlElement(Order = 6)] public int? ITBIS1 { get; set; }
        [XmlElement(Order = 7)] public int? ITBIS2 { get; set; }
        [XmlElement(Order = 8)] public int? ITBIS3 { get; set; }
        [XmlElement(Order = 9)] public decimal? TotalITBIS { get; set; }
        [XmlElement(Order = 10)] public decimal? TotalITBIS1 { get; set; }
        [XmlElement(Order = 11)] public decimal? TotalITBIS2 { get; set; }
        [XmlElement(Order = 12)] public decimal? TotalITBIS3 { get; set; }
        [XmlElement(Order = 13)] public decimal? MontoImpuestoAdicional { get; set; }
        
        [XmlArray(Order = 14)]
        [XmlArrayItem("ImpuestoAdicional")]
        public List<ImpuestoAdicional> ImpuestosAdicionales { get; set; }

        [XmlElement(Order = 15)] public decimal MontoTotal { get; set; }
        [XmlElement(Order = 16)] public decimal? MontoNoFacturable { get; set; }
        [XmlElement(Order = 17)] public decimal? MontoPeriodo { get; set; }
        [XmlElement(Order = 18)] public decimal? SaldoAnterior { get; set; }
        [XmlElement(Order = 19)] public decimal? MontoAvancePago { get; set; }
        [XmlElement(Order = 20)] public decimal? ValorPagar { get; set; }
        [XmlElement(Order = 21)] public decimal? MontoRetencionRenta { get; set; }
        [XmlElement(Order = 22)] public decimal? MontoRetencionITBIS { get; set; }
        [XmlElement(Order = 23)] public decimal? TotalISRRetencion { get; set; }

        public bool ShouldSerializeMontoGravadoTotal() => MontoGravadoTotal.HasValue && MontoGravadoTotal != 0;
        public bool ShouldSerializeMontoGravadoI1() => MontoGravadoI1.HasValue && MontoGravadoI1 != 0;
        public bool ShouldSerializeMontoGravadoI2() => MontoGravadoI2.HasValue && MontoGravadoI2 != 0;
        public bool ShouldSerializeMontoGravadoI3() => MontoGravadoI3.HasValue && MontoGravadoI3 != 0;
        public bool ShouldSerializeMontoExento() => MontoExento.HasValue && MontoExento != 0;
        public bool ShouldSerializeITBIS1() => ITBIS1.HasValue && ITBIS1 != 0;
        public bool ShouldSerializeITBIS2() => ITBIS2.HasValue && ITBIS2 != 0;
        public bool ShouldSerializeITBIS3() => ITBIS3.HasValue && ITBIS3 != 0;
        public bool ShouldSerializeTotalITBIS() => TotalITBIS.HasValue && TotalITBIS != 0;
        public bool ShouldSerializeTotalITBIS1() => TotalITBIS1.HasValue && TotalITBIS1 != 0;
        public bool ShouldSerializeTotalITBIS2() => TotalITBIS2.HasValue && TotalITBIS2 != 0;
        public bool ShouldSerializeTotalITBIS3() => TotalITBIS3.HasValue && TotalITBIS3 != 0;
        public bool ShouldSerializeMontoImpuestoAdicional() => MontoImpuestoAdicional.HasValue && MontoImpuestoAdicional != 0;
        public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Any();
        public bool ShouldSerializeMontoNoFacturable() => MontoNoFacturable.HasValue && MontoNoFacturable != 0;
        public bool ShouldSerializeMontoPeriodo() => MontoPeriodo.HasValue && MontoPeriodo != 0;
        public bool ShouldSerializeSaldoAnterior() => SaldoAnterior.HasValue && SaldoAnterior != 0;
        public bool ShouldSerializeMontoAvancePago() => MontoAvancePago.HasValue && MontoAvancePago != 0;
        public bool ShouldSerializeValorPagar() => ValorPagar.HasValue && ValorPagar != 0;
        public bool ShouldSerializeMontoRetencionRenta() => MontoRetencionRenta.HasValue && MontoRetencionRenta != 0;
        public bool ShouldSerializeMontoRetencionITBIS() => MontoRetencionITBIS.HasValue && MontoRetencionITBIS != 0;
        public bool ShouldSerializeTotalISRRetencion() => TotalISRRetencion.HasValue && TotalISRRetencion != 0;
    }

    public class DetallesItems
    {
        [XmlElement("Item")]
        public List<Item> Item { get; set; } = new List<Item>();
    }

    public class Item
    {
        [XmlElement(Order = 1)] public int NumeroLinea { get; set; }
        
        [XmlArray(Order = 2)]
        [XmlArrayItem("CodigosItem")]
        public List<CodigoItem> TablaCodigosItem { get; set; }

        [XmlElement(Order = 3)] public string CodigoItem { get; set; }
        [XmlElement(Order = 4)] public int? IndicadorFacturacion { get; set; }
        
        [XmlElement(Order = 5)] public RetencionItem Retencion { get; set; }

        [XmlElement(Order = 6)] public string NombreItem { get; set; }
        [XmlElement(Order = 7)] public int IndicadorBienoServicio { get; set; }
        [XmlElement(Order = 8)] public string DescripcionItem { get; set; }
        [XmlElement(Order = 9)] public decimal? CantidadItem { get; set; }
        [XmlElement(Order = 10)] public string UnidadMedida { get; set; }
        [XmlElement(Order = 11)] public decimal? CantidadReferencia { get; set; }
        [XmlElement(Order = 12)] public string UnidadReferencia { get; set; }
        [XmlElement(Order = 13)] public string FechaElaboracion { get; set; }
        [XmlElement(Order = 14)] public string FechaVencimientoItem { get; set; }
        [XmlElement(Order = 15)] public Mineria Mineria { get; set; }
        [XmlElement(Order = 16)] public decimal PrecioUnitarioItem { get; set; }
        [XmlElement(Order = 17)] public decimal? DescuentoMonto { get; set; }
        [XmlElement(Order = 18)] public decimal? RecargoMonto { get; set; }
        [XmlElement(Order = 19)] public OtraMonedaDetalle OtraMonedaDetalle { get; set; }
        [XmlElement(Order = 20)] public decimal MontoItem { get; set; }

        public bool ShouldSerializeTablaCodigosItem() => TablaCodigosItem != null && TablaCodigosItem.Any();
        public bool ShouldSerializeRetencion() => Retencion != null;
        public bool ShouldSerializeMineria() => Mineria != null;
        public bool ShouldSerializeOtraMonedaDetalle() => OtraMonedaDetalle != null;
        public bool ShouldSerializeCodigoItem() => !string.IsNullOrEmpty(CodigoItem);
        public bool ShouldSerializeDescripcionItem() => !string.IsNullOrEmpty(DescripcionItem) && DescripcionItem != "#e";
        public bool ShouldSerializeCantidadItem() => CantidadItem.HasValue;
        public bool ShouldSerializeCantidadReferencia() => CantidadReferencia.HasValue && CantidadReferencia != 0;
        public bool ShouldSerializeDescuentoMonto() => DescuentoMonto.HasValue && DescuentoMonto != 0;
        public bool ShouldSerializeRecargoMonto() => RecargoMonto.HasValue && RecargoMonto != 0;
    }

    public class Subtotales
    {
        [XmlElement("Subtotal")]
        public List<Subtotal> Subtotal { get; set; } = new List<Subtotal>();
    }

    public class Subtotal
    {
        [XmlElement(Order = 1)] public int NumeroSubTotal { get; set; }
        [XmlElement(Order = 2)] public string DescripcionSubtotal { get; set; }
        [XmlElement(Order = 3)] public int Orden { get; set; }
        [XmlElement(Order = 4)] public decimal? SubTotalMontoGravadoTotal { get; set; }
        [XmlElement(Order = 5)] public decimal? SubTotalMontoGravadoI1 { get; set; }
        [XmlElement(Order = 6)] public decimal? SubTotalMontoGravadoI2 { get; set; }
        [XmlElement(Order = 7)] public decimal? SubTotalMontoGravadoI3 { get; set; }
        
        // CORRECCIÓN VITAL: SubTotaITBIS sin 'l' según XSD
        [XmlElement("SubTotaITBIS", Order = 8)] public decimal? SubTotalITBIS { get; set; }
        [XmlElement("SubTotaITBIS1", Order = 9)] public decimal? SubTotalITBIS1 { get; set; }
        [XmlElement("SubTotaITBIS2", Order = 10)] public decimal? SubTotalITBIS2 { get; set; }
        [XmlElement("SubTotaITBIS3", Order = 11)] public decimal? SubTotalITBIS3 { get; set; }
        
        [XmlElement(Order = 12)] public decimal? SubTotalImpuestoAdicional { get; set; }
        [XmlElement(Order = 13)] public decimal? SubTotalExento { get; set; }
        [XmlElement(Order = 14)] public decimal MontoSubTotal { get; set; }
        [XmlElement(Order = 15)] public int Lineas { get; set; }

        public bool ShouldSerializeSubTotalMontoGravadoTotal() => SubTotalMontoGravadoTotal.HasValue && SubTotalMontoGravadoTotal != 0;
        public bool ShouldSerializeSubTotalMontoGravadoI1() => SubTotalMontoGravadoI1.HasValue && SubTotalMontoGravadoI1 != 0;
        public bool ShouldSerializeSubTotalMontoGravadoI2() => SubTotalMontoGravadoI2.HasValue && SubTotalMontoGravadoI2 != 0;
        public bool ShouldSerializeSubTotalMontoGravadoI3() => SubTotalMontoGravadoI3.HasValue && SubTotalMontoGravadoI3 != 0;
        public bool ShouldSerializeSubTotalITBIS() => SubTotalITBIS.HasValue && SubTotalITBIS != 0;
        public bool ShouldSerializeSubTotalITBIS1() => SubTotalITBIS1.HasValue && SubTotalITBIS1 != 0;
        public bool ShouldSerializeSubTotalITBIS2() => SubTotalITBIS2.HasValue && SubTotalITBIS2 != 0;
        public bool ShouldSerializeSubTotalITBIS3() => SubTotalITBIS3.HasValue && SubTotalITBIS3 != 0;
        public bool ShouldSerializeSubTotalImpuestoAdicional() => SubTotalImpuestoAdicional.HasValue && SubTotalImpuestoAdicional != 0;
        public bool ShouldSerializeSubTotalExento() => SubTotalExento.HasValue && SubTotalExento != 0;
    }

    public class InformacionesAdicionales { [XmlElement(Order=1)] public string FechaEmbarque { get; set; } public bool ShouldSerializeFechaEmbarque() => !string.IsNullOrEmpty(FechaEmbarque); [XmlElement(Order=7)] public decimal? TotalFob { get; set; } public bool ShouldSerializeTotalFob() => TotalFob.HasValue; }
    public class CodigoItem { public string TipoCodigo { get; set; } public string CodigoItemValue { get; set; } }
    public class RetencionItem { public int IndicadorAgenteRetencionoPercepcion { get; set; } [XmlIgnore] public decimal MontoISRRetenido { get; set; } [XmlElement("MontoISRRetenido")] public string MontoISRRetenidoString { get => MontoISRRetenido.ToString("F2"); set => MontoISRRetenido = decimal.Parse(value); } }
    public class Mineria { public decimal? PesoNetoKilogramo { get; set; } public decimal? PesoNetoMineria { get; set; } public int? TipoAfiliacion { get; set; } public int? Liquidacion { get; set; } }
    public class OtraMonedaDetalle { public decimal? PrecioOtraMoneda { get; set; } public decimal? DescuentoOtraMoneda { get; set; } public decimal? RecargoOtraMoneda { get; set; } public decimal? MontoItemOtraMoneda { get; set; } }
    public class DescuentosORecargos { [XmlElement("DescuentoORecargo")] public List<DescuentoORecargo> DescuentoORecargo { get; set; } }
    public class DescuentoORecargo { [XmlElement(Order = 1)] public int? NumeroLinea { get; set; } [XmlElement(Order = 2)] public string TipoDescuentoORecargo { get; set; } [XmlElement(Order = 3)] public string TipoAjuste { get; set; } [XmlElement(Order = 4)] public decimal? MontoDescuentoORecargo { get; set; } }
    public class Paginacion { [XmlElement("Pagina")] public List<Pagina> Pagina { get; set; } }
    public class Pagina { [XmlElement(Order = 1)] public int PaginaNo { get; set; } [XmlElement(Order = 2)] public int NoLineaDesde { get; set; } [XmlElement(Order = 3)] public int NoLineaHasta { get; set; } [XmlElement(Order = 4)] public decimal? MontoSubtotalPagina { get; set; } }
    public class InformacionReferencia { [XmlElement(Order = 1)] public string NCFModificado { get; set; } [XmlElement(Order = 2)] public string RNCOtroContribuyente { get; set; } [XmlElement(Order = 3)] public string FechaNCFModificado { get; set; } [XmlElement(Order = 4)] public int? CodigoModificacion { get; set; } public bool ShouldSerializeRNCOtroContribuyente() => !string.IsNullOrEmpty(RNCOtroContribuyente) && RNCOtroContribuyente != "#e"; public bool ShouldSerializeCodigoModificacion() => CodigoModificacion.HasValue; }
    public class TablaAdicional { [XmlElement(Order = 1)] public int TipoAlfanumerico { get; set; } [XmlElement(Order = 2)] public int TipoNumerico { get; set; } }
    public class ImpuestoAdicional { [XmlElement(Order = 1)] public string TipoImpuesto { get; set; } [XmlElement(Order = 2)] public decimal? MontoImpuestoSelectivoConsumoEspecifico { get; set; } [XmlElement(Order = 3)] public decimal? MontoImpuestoSelectivoConsumoAdvalorem { get; set; } [XmlElement(Order = 4)] public decimal? OtrosImpuestosAdicionales { get; set; } }
    public class VersionType { [XmlText] public string Value { get; set; } = "1.0"; }
    public class OtraMoneda { [XmlElement(Order = 1)] public string TipoMoneda { get; set; } [XmlElement(Order = 2)] public decimal TipoCambio { get; set; } [XmlElement(Order = 3)] public decimal? MontoGravadoTotalOtraMoneda { get; set; } [XmlElement(Order = 4)] public decimal? MontoTotalOtraMoneda { get; set; } }
    public class Transporte { [XmlElement(Order=1)] public string ViaTransporte { get; set; } [XmlElement(Order=2)] public string PaisOrigen { get; set; } [XmlElement(Order=3)] public string DireccionDestino { get; set; } [XmlElement(Order=4)] public string PaisDestino { get; set; } [XmlElement(Order=5)] public string RNCIdentificacionCompaniaTransportista { get; set; } [XmlElement(Order=6)] public string NombreCompaniaTransportista { get; set; } [XmlElement(Order=7)] public string NumeroViaje { get; set; } [XmlElement(Order=8)] public string Conductor { get; set; } [XmlElement(Order=9)] public string DocumentoTransporte { get; set; } [XmlElement(Order=10)] public string Ficha { get; set; } [XmlElement(Order=11)] public string Placa { get; set; } [XmlElement(Order=12)] public string RutaTransporte { get; set; } [XmlElement(Order=13)] public string ZonaTransporte { get; set; } [XmlElement(Order=14)] public string NumeroAlbaran { get; set; } public bool ShouldSerializePaisDestino() => !string.IsNullOrEmpty(PaisDestino) && PaisDestino != "#e"; public bool ShouldSerializeConductor() => !string.IsNullOrEmpty(Conductor) && Conductor != "#e"; }
    
    public class Signature { [XmlAttribute] public string Id { get; set; } = "Signature"; public SignedInfo SignedInfo { get; set; } public string SignatureValue { get; set; } public KeyInfo KeyInfo { get; set; } }
    public class SignedInfo { public CanonicalizationMethod CanonicalizationMethod { get; set; } public SignatureMethod SignatureMethod { get; set; } public Reference Reference { get; set; } }
    public class CanonicalizationMethod { [XmlAttribute] public string Algorithm { get; set; } = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315"; }
    public class SignatureMethod { [XmlAttribute] public string Algorithm { get; set; } = "http://www.w3.org/2000/09/xmldsig#rsa-sha1"; }
    public class Reference { [XmlAttribute] public string URI { get; set; } = ""; public Transforms Transforms { get; set; } public DigestMethod DigestMethod { get; set; } public string DigestValue { get; set; } }
    public class Transforms { public Transform Transform { get; set; } }
    public class Transform { [XmlAttribute] public string Algorithm { get; set; } = "http://www.w3.org/2000/09/xmldsig#enveloped-signature"; }
    public class DigestMethod { [XmlAttribute] public string Algorithm { get; set; } = "http://www.w3.org/2000/09/xmldsig#sha1"; }
    public class KeyInfo { public X509Data X509Data { get; set; } }
    public class X509Data { public string X509Certificate { get; set; } }
}