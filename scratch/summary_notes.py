import psycopg2
from decimal import Decimal

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
    cur.execute("""
        SELECT dn.id_delivery_note, dn.note_number, dn.creation_date, dn.status,
               dn.total_amount_usd, dn.discount_percentage, dn.volume_discount_percentage,
               s.full_name as seller_name, s.seller_code,
               c.customer_code, c.business_name, c.rif, z.name as zona,
               r.relation_number
        FROM delivery_note dn
        LEFT JOIN seller s ON dn.id_seller = s.id_seller
        LEFT JOIN customer c ON dn.id_customer = c.id_customer
        LEFT JOIN zona z ON c.id_zona = z.id_zona
        LEFT JOIN relacion r ON dn.id_relacion = r.id_relacion
        WHERE dn.note_number = %s
    """, (num,))
    h = cur.fetchone()
    if not h:
        print(f"Nota {num}: No encontrada")
        continue

    cur.execute("""
        SELECT count(*), sum(quantity), sum(subtotal_usd)
        FROM note_detail
        WHERE id_delivery_note = %s
    """, (h[0],))
    cnt, sum_qty, sum_subtot = cur.fetchone()

    cur.execute("""
        SELECT count(*), 
               sum(case when movement_type='ENTRADA' then quantity else 0 end),
               sum(case when movement_type='SALIDA' then quantity else 0 end)
        FROM stock_movement
        WHERE id_delivery_note = %s
    """, (h[0],))
    sm_cnt, sm_in, sm_out = cur.fetchone()

    print(f"NOTA: {h[1]} (ID: {h[0]})")
    print(f"  Fecha: {h[2]}")
    print(f"  Estado: {h[3]}")
    print(f"  Vendedor: {h[7]} (Cod: {h[8]}) | Cliente: [{h[9]}] {h[10]} (RIF: {h[11]}) | Zona: {h[12]}")
    print(f"  Relación: Nro {h[13]}")
    print(f"  Renglones: {cnt} | Unidades vendidas: {sum_qty} | Suma bruta renglones: ${sum_subtot}")
    print(f"  Descuento: {h[5]}% | Descuento Volumen: {h[6]}%")
    print(f"  Total registrado en BD: ${h[4]}")
    print(f"  Movimientos Stock: {sm_cnt} registros (Entradas: {sm_in}, Salidas: {sm_out})")
    
    # Check if formula matches
    disc = (Decimal(str(h[5] or 0)))
    vol = (Decimal(str(h[6] or 0)))
    calc = (Decimal(str(sum_subtot or 0)) * (1 - disc/100) * (1 - vol/100)).quantize(Decimal('0.01'))
    print(f"  Total recalculado matemáticamente: ${calc} -> Coincide: {calc == h[4]}")
    print("-" * 60)

conn.close()
