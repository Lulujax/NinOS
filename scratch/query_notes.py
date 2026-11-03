import psycopg2
import json
from decimal import Decimal
from datetime import date, datetime

def default_converter(o):
    if isinstance(o, (datetime, date)):
        return o.isoformat()
    if isinstance(o, Decimal):
        return float(o)
    return str(o)

conn = psycopg2.connect(
    host='82.39.109.158',
    port=5432,
    dbname='ninos_db',
    user='ninos_admin',
    password='1234'
)
cur = conn.cursor()

note_numbers = ['000_004', '000_005', '000_006', '000_007', '000_008', '000_009']

for num in note_numbers:
    print(f"\n================================================================================")
    print(f"                                NOTA {num}                                      ")
    print(f"================================================================================")
    cur.execute("SELECT * FROM delivery_note WHERE note_number = %s", (num,))
    dn_cols = [desc[0] for desc in cur.description]
    dn_row = cur.fetchone()
    if not dn_row:
        print("NO ENCONTRADA")
        continue
    dn_data = dict(zip(dn_cols, dn_row))
    print("--- CABECERA DELIVERY NOTE ---")
    for k, v in dn_data.items():
        print(f"  {k}: {v}")

    # Seller info
    if dn_data.get('id_seller'):
        cur.execute("SELECT * FROM seller WHERE id_seller = %s", (dn_data['id_seller'],))
        s_cols = [desc[0] for desc in cur.description]
        s_row = cur.fetchone()
        print("\n--- VENDEDOR ---")
        if s_row:
            for k, v in dict(zip(s_cols, s_row)).items():
                print(f"  {k}: {v}")

    # Customer info
    if dn_data.get('id_customer'):
        cur.execute("""
            SELECT c.*, z.name as zona_name, z.code as zona_code, z.is_pro_venta as zona_is_pro_venta
            FROM customer c
            LEFT JOIN zona z ON c.id_zona = z.id_zona
            WHERE c.id_customer = %s
        """, (dn_data['id_customer'],))
        c_cols = [desc[0] for desc in cur.description]
        c_row = cur.fetchone()
        print("\n--- CLIENTE ---")
        if c_row:
            for k, v in dict(zip(c_cols, c_row)).items():
                print(f"  {k}: {v}")

    # Relacion info
    if dn_data.get('id_relacion'):
        cur.execute("SELECT * FROM relacion WHERE id_relacion = %s", (dn_data['id_relacion'],))
        rel_cols = [desc[0] for desc in cur.description]
        rel_row = cur.fetchone()
        print("\n--- RELACIÓN ---")
        if rel_row:
            for k, v in dict(zip(rel_cols, rel_row)).items():
                print(f"  {k}: {v}")

    # Note Details
    cur.execute("""
        SELECT nd.*, p.name as product_name, p.product_code, p.category as product_category,
               pr.name as promo_name, pr.promotion_code
        FROM note_detail nd
        LEFT JOIN product p ON nd.id_product = p.id_product
        LEFT JOIN promotion pr ON nd.id_promotion = pr.id_promotion
        WHERE nd.id_delivery_note = %s
        ORDER BY nd.id_note_detail ASC
    """, (dn_data['id_delivery_note'],))
    det_cols = [desc[0] for desc in cur.description]
    det_rows = cur.fetchall()
    print(f"\n--- DETALLES / ITEMS ({len(det_rows)} renglones) ---")
    total_calc = Decimal('0')
    total_items_qty = 0
    for d in det_rows:
        dd = dict(zip(det_cols, d))
        qty = dd.get('quantity') or 0
        price = dd.get('unit_price_usd') or Decimal('0')
        subtot = dd.get('subtotal_usd') or Decimal('0')
        p_name = dd.get('product_name') or dd.get('promo_name') or dd.get('product_code_snapshot')
        p_code = dd.get('product_code') or dd.get('promotion_code') or dd.get('product_code_snapshot')
        total_items_qty += qty
        print(f"  Item #{dd['id_note_detail']} | Cod: {p_code} | Desc: {p_name} | Cant: {qty} | P.Unit: {price} | Subtotal: {subtot}")
        if subtot is not None:
            total_calc += Decimal(str(subtot))
    
    disc_pct = Decimal(str(dn_data.get('discount_percentage') or 0))
    vol_pct = Decimal(str(dn_data.get('volume_discount_percentage') or 0))
    disc_factor = (Decimal('1') - disc_pct / Decimal('100')) * (Decimal('1') - vol_pct / Decimal('100'))
    expected_total = (total_calc * disc_factor).quantize(Decimal('0.01'))
    
    print(f"\n  [ANÁLISIS MATEMÁTICO]")
    print(f"  Total unidades: {total_items_qty}")
    print(f"  Suma bruta de renglones: ${total_calc}")
    print(f"  Descuento base aplicado: {disc_pct}%")
    print(f"  Descuento volumen aplicado: {vol_pct}%")
    print(f"  Total esperado con descuentos: ${expected_total}")
    print(f"  Total guardado en BD (total_amount_usd): ${dn_data.get('total_amount_usd')}")
    print(f"  Total ajustado en BD (adjusted_total_usd): ${dn_data.get('adjusted_total_usd')}")
    diff = Decimal(str(dn_data.get('total_amount_usd') or 0)) - expected_total
    print(f"  Diferencia (Guardado - Esperado): ${diff}")

    # Payments
    cur.execute("SELECT * FROM payment WHERE id_delivery_note = %s", (dn_data['id_delivery_note'],))
    p_cols = [desc[0] for desc in cur.description]
    p_rows = cur.fetchall()
    print(f"\n--- PAGOS ASOCIADOS ({len(p_rows)}) ---")
    for p in p_rows:
        print(" ", dict(zip(p_cols, p)))

    # Credit notes
    cur.execute("SELECT * FROM credit_note WHERE id_delivery_note = %s", (dn_data['id_delivery_note'],))
    cn_cols = [desc[0] for desc in cur.description]
    cn_rows = cur.fetchall()
    print(f"\n--- NOTAS DE CRÉDITO ASOCIADAS ({len(cn_rows)}) ---")
    for cn in cn_rows:
        print(" ", dict(zip(cn_cols, cn)))

    # Stock movements
    cur.execute("""
        SELECT sm.*, p.name as product_name, p.product_code
        FROM stock_movement sm
        LEFT JOIN product p ON sm.id_product = p.id_product
        WHERE sm.id_delivery_note = %s
    """, (dn_data['id_delivery_note'],))
    sm_cols = [desc[0] for desc in cur.description]
    sm_rows = cur.fetchall()
    print(f"\n--- MOVIMIENTOS DE STOCK ({len(sm_rows)}) ---")
    for sm in sm_rows:
        smd = dict(zip(sm_cols, sm))
        print(f"  ID #{smd.get('id_stock_movement')} | Prod: {smd.get('product_code')} | Tipo: {smd.get('movement_type')} | Cant: {smd.get('quantity')} | Status: {smd.get('document_status')} | Fecha: {smd.get('movement_date')}")

conn.close()
