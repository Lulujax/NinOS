import psycopg2

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
    print(f"\n==================== NOTA {num} ====================")
    cur.execute("""
        SELECT nd.id_note_detail, coalesce(p.product_code, pr.promotion_code, nd.product_code_snapshot) as code,
               coalesce(p.name, pr.name) as name, nd.quantity, nd.unit_price_usd, nd.subtotal_usd
        FROM delivery_note dn
        JOIN note_detail nd ON dn.id_delivery_note = nd.id_delivery_note
        LEFT JOIN product p ON nd.id_product = p.id_product
        LEFT JOIN promotion pr ON nd.id_promotion = pr.id_promotion
        WHERE dn.note_number = %s
        ORDER BY nd.id_note_detail
    """, (num,))
    for r in cur.fetchall():
        print(f"  {r[1]} | {r[2]} | Cant: {r[3]} | P.U: ${r[4]} | Subt: ${r[5]}")

conn.close()
