import pandas as pd
import random
from datetime import datetime, timedelta

def generate_dummy_data():
    # ECF Sheet Data
    ecf_data = []
    
    # Common fields
    rnc_emisor = "131487272"
    razon_social_emisor = "EMISOR DE PRUEBA S.R.L"
    
    # Case 1: Factura de Crédito Fiscal (31) - Valid
    ecf_data.append({
        "CasoPrueba": "131487272E310000000001",
        "Version": "1.0",
        "TipoeCF": 31,
        "ENCF": "E310000000001",
        "FechaVencimientoSecuencia": (datetime.now() + timedelta(days=365)).strftime("%Y-%m-%d"),
        "IndicadorNotaCredito": 0,
        "IndicadorEnvioDiferido": 0,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": 1,
        "TipoPago": 1,
        "FechaLimitePago": (datetime.now() + timedelta(days=30)).strftime("%Y-%m-%d"),
        "TerminoPago": "30 dias",
        "FormaPago1": "01",
        "MontoPago1": 1180.00,
        "RNCEmisor": rnc_emisor,
        "RazonSocialEmisor": razon_social_emisor,
        "RNCComprador": "101001001",
        "RazonSocialComprador": "COMPRADOR PRUEBA",
        "MontoGravadoTotal": 1000.00,
        "MontoGravadoI1": 1000.00,
        "TotalITBIS": 180.00,
        "TotalITBIS1": 180.00,
        "MontoTotal": 1180.00,
        "FechaEmision": datetime.now().strftime("%Y-%m-%d"),
        "CodigoMoneda": "DOP"
    })

    # Case 2: Factura de Consumo (32) - Valid
    ecf_data.append({
        "CasoPrueba": "131487272E320000000001",
        "Version": "1.0",
        "TipoeCF": 32,
        "ENCF": "E320000000001",
        "FechaVencimientoSecuencia": (datetime.now() + timedelta(days=365)).strftime("%Y-%m-%d"),
        "IndicadorNotaCredito": 0,
        "IndicadorEnvioDiferido": 0,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": 1,
        "TipoPago": 1,
        "FormaPago1": "01",
        "MontoPago1": 590.00,
        "RNCEmisor": rnc_emisor,
        "RazonSocialEmisor": razon_social_emisor,
        "RNCComprador": "", # Optional for < 250k
        "RazonSocialComprador": "",
        "MontoGravadoTotal": 500.00,
        "MontoGravadoI1": 500.00,
        "TotalITBIS": 90.00,
        "TotalITBIS1": 90.00,
        "MontoTotal": 590.00,
        "FechaEmision": datetime.now().strftime("%Y-%m-%d"),
        "CodigoMoneda": "DOP"
    })

    # Case 3: Nota de Crédito (34) - Valid
    ecf_data.append({
        "CasoPrueba": "131487272E340000000001",
        "Version": "1.0",
        "TipoeCF": 34,
        "ENCF": "E340000000001",
        "FechaVencimientoSecuencia": (datetime.now() + timedelta(days=365)).strftime("%Y-%m-%d"),
        "IndicadorNotaCredito": 1, # Required for 34
        "IndicadorEnvioDiferido": 0,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": 1,
        "TipoPago": 1,
        "FormaPago1": "01",
        "MontoPago1": 118.00,
        "RNCEmisor": rnc_emisor,
        "RazonSocialEmisor": razon_social_emisor,
        "RNCComprador": "101001001",
        "RazonSocialComprador": "COMPRADOR PRUEBA",
        "MontoGravadoTotal": 100.00,
        "MontoGravadoI1": 100.00,
        "TotalITBIS": 18.00,
        "TotalITBIS1": 18.00,
        "MontoTotal": 118.00,
        "FechaEmision": datetime.now().strftime("%Y-%m-%d"),
        "CodigoMoneda": "DOP",
        "NCFModificado": "E310000000001", # Required for 33/34
        "FechaNCFModificado": (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
    })

     # Case 4: Factura de Crédito Fiscal (31) - Invalid (Missing RNC Comprador)
    ecf_data.append({
        "CasoPrueba": "131487272E310000000002",
        "Version": "1.0",
        "TipoeCF": 31,
        "ENCF": "E310000000002",
        "FechaVencimientoSecuencia": (datetime.now() + timedelta(days=365)).strftime("%Y-%m-%d"),
        "IndicadorNotaCredito": 0,
        "IndicadorEnvioDiferido": 0,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": 1,
        "TipoPago": 1,
        "FormaPago1": "01",
        "MontoPago1": 1180.00,
        "RNCEmisor": rnc_emisor,
        "RazonSocialEmisor": razon_social_emisor,
        "RNCComprador": "", # Missing!
        "RazonSocialComprador": "COMPRADOR PRUEBA",
        "MontoGravadoTotal": 1000.00,
        "MontoGravadoI1": 1000.00,
        "TotalITBIS": 180.00,
        "TotalITBIS1": 180.00,
        "MontoTotal": 1180.00,
        "FechaEmision": datetime.now().strftime("%Y-%m-%d"),
        "CodigoMoneda": "DOP"
    })

    df_ecf = pd.DataFrame(ecf_data)

    # RFCE Sheet Data (Summaries)
    rfce_data = []
    # Summary for Case 2
    rfce_data.append({
        "CasoPrueba": "131487272E320000000001",
        "Version": "1.0",
        "TipoeCF": 32,
        "ENCF": "E320000000001",
        "TipoIngresos": 1,
        "TipoPago": 1,
        "FormaPago1": "01",
        "MontoPago1": 590.00,
        "RNCEmisor": rnc_emisor,
        "RazonSocialEmisor": razon_social_emisor,
        "FechaEmision": datetime.now().strftime("%Y-%m-%d"),
        "MontoGravadoTotal": 500.00,
        "MontoGravadoI1": 500.00,
        "TotalITBIS": 90.00,
        "TotalITBIS1": 90.00,
        "MontoTotal": 590.00
    })

    df_rfce = pd.DataFrame(rfce_data)

    # Write to Excel
    with pd.ExcelWriter('dummy_data.xlsx') as writer:
        df_ecf.to_excel(writer, sheet_name='ECF', index=False)
        df_rfce.to_excel(writer, sheet_name='RFCE', index=False)

    print("dummy_data.xlsx created successfully.")

if __name__ == "__main__":
    generate_dummy_data()
