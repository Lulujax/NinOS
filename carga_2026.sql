BEGIN;

-- 1. Tabla temporal con los 162 productos
CREATE TEMP TABLE tmp_carga_inicial (
    product_code    varchar,
    name            varchar,
    category        varchar,
    unit_price_usd  numeric
) ON COMMIT PRESERVE ROWS;

INSERT INTO tmp_carga_inicial (product_code, name, category, unit_price_usd) VALUES

-- ================= OLEOS (19) =================
('OLE30300', 'OLEO''S AMPOLLA ANTICAIDA 24 UNDS. OLEOS',                             'OLEOS', 1.1066666666666667),
('OLE30301', 'OLEO''S AMPOLLA COMPLEX (Hidratacion intensiva)',                      'OLEOS', 1.23),
('OLE30302', 'OLEO''S AMPOLLA ANTI- FRIZZ OLEOS',                                    'OLEOS', 1.1066666666666667),
('OLE30303', 'OLEO''S AMPOLLA ALISADORA OLEOS',                                      'OLEOS', 1.1066666666666667),
('OLE30304', 'OLEO''S AMPOLLA C.DE SABILA/ACEITE OLIVA OLEOS',                       'OLEOS', 1.1066666666666667),
('OLE30305', 'OLEO''S SHAMPOO CONTROL FRIZZ',                                        'OLEOS', 6.3999999999999995),
('OLE30306', 'OLEO''S ACONDICIONADOR CONTROL FRIZZ',                                 'OLEOS', 6.3999999999999995),
('OLE30307', 'OLEO''S SHAMPOO CONTROL CAIDA',                                        'OLEOS', 6.3999999999999995),
('OLE30308', 'OLEO''S ACONDICIONADOR CONTROL CAIDA',                                 'OLEOS', 6.3999999999999995),
('OLE30309', 'OLEO''S SHAMPOO RESTAURADOR',                                          'OLEOS', 6.3999999999999995),
('OLE30310', 'OLEO''S ACONDICIONADOR RESTAURADOR',                                   'OLEOS', 6.3999999999999995),
('OLE30311', 'OLEO''S SHAMPOO CONTROL CASPA',                                        'OLEOS', 6.3999999999999995),
('OLE30312', 'OLEO''S ACONDICIONADOR CONTROL CASPA',                                 'OLEOS', 6.3999999999999995),
('OLE30313', 'OLEO''S SHAMPOO CUIDADO DIARIO',                                       'OLEOS', 6.3999999999999995),
('OLE30314', 'OLEO''S ACONDICIONADOR CUIDADO DIARIO',                                'OLEOS', 6.3999999999999995),
('OLE30315', 'OLEO''S SHAMPOO RIZOS DEFINIDOS',                                      'OLEOS', 6.3999999999999995),
('OLE30316', 'OLEO''S ACONDICIONADOR RIZOS DEFINIDOS',                               'OLEOS', 6.3999999999999995),
('OLE30317', 'OLEO''S MASCARILLA HIDRATANTE + PROTEINAS',                            'OLEOS', 6.3999999999999995),
('OLE30318', 'OLEO''S CUBRE CANAS HIDRATANTE',                                       'OLEOS', 0),

-- ================= REMBRANDT (19) =================
('REM30401', 'AMPOLLA ANTI CAIDA REMBRANT',                                          'REMBRANDT', 1.2133333333333334),
('REM30402', 'AMPOLLA GOTAS DE SEDA REMBRANT',                                       'REMBRANDT', 1.2133333333333334),
('REM30403', 'AMPOLLA SEMILINO REMBRANT',                                            'REMBRANDT', 1.2133333333333334),
('REM30404', 'AMPOLLA PHYTO KERATINA REMBRANT',                                      'REMBRANDT', 1.2133333333333334),
('REM30405', 'AMPOLLA PLACENTA DE OVEJO REMBRANT',                                   'REMBRANDT', 1.2133333333333334),
('REM30406', 'PRE-TRATAMIENTO PLACENTA OVEJO 1 LITRO',                               'REMBRANDT', 5.333333333333333),
('REM30407', 'TRATAMIENTO INTENSIVO PLACENTA OVEJO 400 GR',                          'REMBRANDT', 4.666666666666667),
('REM30408', 'Pre-Tratamiento Argán 360 ml REMBRANDT',                               'REMBRANDT', 5.013333333333333),
('REM30409', 'Post-Tratamiento Aceite/Argán 360ml REMBRANDT',                        'REMBRANDT', 5.1066666666666665),
('REM30410', 'Tratamiento Intensivo Capilar Baño de Crema Aceite/Argán 240ml REMBRANDT', 'REMBRANDT', 4.933333333333334),
('REM30411', 'Crema Reafirmante con Colageno y Vitamina E 60 Grs. REMBRANDT',        'REMBRANDT', 5.133333333333334),
('REM30412', 'Agua Micelar 120 ML. REMBRANDT',                                       'REMBRANDT', 5.133333333333334),
('REM30413', 'Locion Desmaquillante 120 ML. REMBRANDT',                              'REMBRANDT', 3.9600000000000004),
('REM30414', 'Crema Corporal Hidratante 400 ML. REMBRANDT',                          'REMBRANDT', 5.746666666666666),
('REM30415', 'Body Splah Frambuesa Desire 240 ML. REMBRANDT',                        'REMBRANDT', 4.906666666666667),
('REM30416', 'Body Splah Vainilla Rocio 240 ML. REMBRANDT',                          'REMBRANDT', 4.906666666666667),
('REM30417', 'AGUA DE ROSA REMBRANDT 120 ML',                                        'REMBRANDT', 5.133333333333334),
('REM30418', 'KID''S HAIR CLEAN CHAMPU NIÑOS Fragancia Manzanilla',                  'REMBRANDT', 3.3333333333333335),
('REM30419', 'PRE - TRATAMIENTO PLACENTA OVEJO 500 ML',                              'REMBRANDT', 4),

-- ================= DEFILE (64) =================
('DEF30001', 'AMPOLLA TRICOMPLEX MATIZADOR (tipo vial)',                             'DEFILE', 2.76),
('DEF30002', 'AMPOLLA MATIZADORA (tipo Embudo)',                                     'DEFILE', 1.6933333333333334),
('DEF30003', 'AMPOLLA TRICOMPLEX CON ACIDO HIALURONICO',                             'DEFILE', 1.8399999999999999),
('DEF30004', 'AMPOLLA K-BOTROX HIDRATANTE',                                          'DEFILE', 1.3866666666666667),
('DEF30005', 'AMPOLLA K-BOTROX ACONDICIONADOR',                                      'DEFILE', 1.3866666666666667),
('DEF30006', 'AMPOLLA K-BOTROX 3 (Ultra Hidratante D-Phantenol)',                    'DEFILE', 2.76),
('DEF30007', 'AMPOLLA REGULADOR (CABELLOS GRASOS)',                                  'DEFILE', 1.3866666666666667),
('DEF30008', 'AMPOLLA ACEITE DE ARGAN ACONDICIONADOR',                               'DEFILE', 1.3866666666666667),
('DEF30009', 'AMPOLLA ACEITE DE ARGAN SUAVIDAD',                                     'DEFILE', 1.3866666666666667),
('DEF30010', 'AMPOLLA BIOTINA (FORTALECE LA FIBRAS CAPILARES)',                      'DEFILE', 1.6933333333333334),
('DEF30011', 'AMPOLLA TRICOMPLEX (Ultra acondicionador y brillo)',                   'DEFILE', 1.6933333333333334),
('DEF30012', 'AMPOLLA ANTICAIDA (FORTALECE LA RAIZ)',                                'DEFILE', 1.3866666666666667),
('DEF30013', 'AMPOLLA KERATINA (Ideal para el cabello fino y fragil)',               'DEFILE', 1.3866666666666667),
('DEF30014', 'AMPOLLA SILICON Y SEDA',                                               'DEFILE', 1.3866666666666667),
('DEF30015', 'AMPOLLA ANTICASPA',                                                    'DEFILE', 1.3866666666666667),
('DEF30016', 'AMPOLLA KERATINA PLANCHADO EXPRESS',                                   'DEFILE', 1.6933333333333334),
('DEF30017', 'AMPOLLA KERATINA SHOCK',                                               'DEFILE', 1.3866666666666667),
('DEF30018', 'AMPOLLA SEMILINO',                                                     'DEFILE', 1.3866666666666667),
('DEF30019', 'AMPOLLA PLACENTA DE OVEJO',                                            'DEFILE', 1.3866666666666667),
('DEF30020', 'AMPOLLA CRISTAL DE SAVILA',                                            'DEFILE', 1.3866666666666667),
('DEF30021', 'AMPOLLA SBLOCK 27',                                                    'DEFILE', 1.6933333333333334),
('DEF30022', 'AMPOLLA MEZCLA TINTE',                                                 'DEFILE', 1.38666666666667),
('DEF30023', 'AMPOLLA LISO Y BRILLO',                                                'DEFILE', 1.3866666666666667),
('DEF30024', 'AMPOLLA UVA THERAPY',                                                  'DEFILE', 1.3866666666666667),
('DEF30025', 'AMPOLLA CUBRE CANAS',                                                  'DEFILE', 1.3866666666666667),
('DEF30026', 'AMPOLLA ACEITE MACADAMIA NUTRI (NUTRE)',                               'DEFILE', 1.3866666666666667),
('DEF30027', 'AMPOLLA ACEITE MACADAMIA HIDRATACION (DEFILE)',                        'DEFILE', 1.3866666666666667),
('DEF30028', 'AMPOLLA ISOSFOLIEX HAIR SPA',                                          'DEFILE', 1.6933333333333334),
('DEF30029', 'AMPOLLA LECHE DE ALMENDRA',                                            'DEFILE', 1.3866666666666667),
('DEF30030', 'AMPOLLA TRICOMPLEX MATIZADOR (tipo embudo)',                           'DEFILE', 2.4533333333333336),
('DEF30100', 'PRE-TRATAMIENTO TRICOMPLEX MATIZADOR',                                 'DEFILE', 5.826666666666667),
('DEF30101', 'TRATAMIENTO INTENSIVO TRICOMPLEX MATIZADORA',                          'DEFILE', 5.6),
('DEF30102', 'PRE-TRATAMIENTO TRICOMPLEX CON VITAMINA E',                            'DEFILE', 5.826666666666667),
('DEF30103', 'TRATAMIENTO INTENSIVO TRICOMPLEX CON VITAMINA E',                      'DEFILE', 5.6000000000000005),
('DEF30104', 'PRE-TRATAMIENTO TRICOMPLEX CON ACIDO HIALURONICO',                     'DEFILE', 5.826666666666667),
('DEF30105', 'TRATAMIENTO INTENSIVO TRICOMPLEX CON ACIDO HIALURONICO',               'DEFILE', 5.6000000000000005),
('DEF30106', 'PRE-TRATAMIENTO ACIDO HIALURONICO. (BLANCO)',                          'DEFILE', 5.586666666666667),
('DEF30107', 'TRATAMIENTO INTENSIVO ACIDO HIALURONICO. (BLANCO)',                    'DEFILE', 5.68),
('DEF30108', 'PRE-TRATAMIENTO K-BOTROX',                                             'DEFILE', 5.6000000000000005),
('DEF30109', 'TRATAMIENTO INTENSIVO K-BOTROX',                                       'DEFILE', 5.453333333333333),
('DEF30110', 'PRE-TRATAMIENTO REGULADOR',                                            'DEFILE', 5.6000000000000005),
('DEF30111', 'TRATAMIENTO INTENSIVO REGULADOR',                                      'DEFILE', 5.453333333333333),
('DEF30112', 'PRE-TRATAMIENTO ARGAN',                                                'DEFILE', 5.826666666666667),
('DEF30113', 'TRATAMIENTO INTENSIVO ACEITE DE ARGAN',                                'DEFILE', 5.52),
('DEF30114', 'PRE-TRATAMIENTO BIOTINA DAMA',                                         'DEFILE', 5.6000000000000005),
('DEF30115', 'PRE-TRATAMIENTO BIOTINA CABALLERO',                                    'DEFILE', 5.6),
('DEF30116', 'CHAMPU PROFESIONAL PH NEUTRO 2 Lt',                                    'DEFILE', 8.85333333333333),
('DEF30117', 'PRE-TRATAMIENTO PH NEUTRO GALÓN',                                      'DEFILE', 15.3333333333333),
('DEF30118', 'POST TRATAMIENTO PH NEUTRO GALÓN',                                     'DEFILE', 15.333333333333334),
('DEF30119', 'SUERO CAPILAR K-BOTROX',                                               'DEFILE', 4.6000000000000005),
('DEF30120', 'ACEITE DE ARGAN CAPILAR',                                              'DEFILE', 5.373333333333334),
('DEF30121', 'ACTIVADOR DE RIZOS',                                                   'DEFILE', 6.573333333333333),
('DEF30122', 'CREMA DESENREDANTE CON ACIDO HIALURONICO Y COLAGENO',                  'DEFILE', 6.573333333333333),
('DEF30123', 'CREMA ALISADORA SUAVE CON KERATINA',                                   'DEFILE', 3.069733333333333),
('DEF30124', 'CREMA ALISADORA FUERTE CON KERATINA',                                  'DEFILE', 5.333333333333333),
('DEF30125', 'POLVO DECOLORANTE DEFILE',                                             'DEFILE', 17.706666666666667),
('DEF30126', 'CIRUGIA LISS EVOLUTION 911 KIT-DE 3',                                  'DEFILE', 24.15),
('DEF30127', 'CIRUGIA LISS EVOLUTION 911 KIT-DE 2',                                  'DEFILE', 20.7),
('DEF30128', 'LISS EVOLUTION 911 SPRAY PROTEC TERMICO',                              'DEFILE', 6.626666666666666),
('DEF30129', 'TONICO CAPILAR ISOSFOLIEX',                                            'DEFILE', 5.746666666666666),
('DEF30130', 'DESENGRASANTE MULTIUSO GALÓN',                                         'DEFILE', 12.266666666666666),
('DEF30131', 'AGUA OXIGENADA VOL. 20',                                               'DEFILE', 1.08),
('DEF30132', 'AGUA OXIGENADA VOL. 30',                                               'DEFILE', 1.08),
('DEF30135', 'BALSAMO PROFESIONAL PH NEUTRO 2 Lt',                                   'DEFILE', 8.853333333333333),

-- ================= BIOLINE (22) =================
('BIO30200', 'AGUA MISCELAR',                                                        'BIOLINE', 5.1466666666666665),
('BIO30201', 'LOCION DESMAQUILLANTE',                                                'BIOLINE', 3.9600000000000004),
('BIO30202', 'AGUA DE ROSAS',                                                        'BIOLINE', 5.1466666666666665),
('BIO30203', 'LIMPIADOR FACIAL HIDRATANTE',                                          'BIOLINE', 7.32),
('BIO30204', 'LIMPIADOR DE BROCHAS',                                                 'BIOLINE', 7.653333333333333),
('BIO30205', 'CREMA FACIAL REAFIRMANTE CON COLAGENO Y VIT. E',                       'BIOLINE', 5.1466666666666665),
('BIO30206', 'CREMA FACIAL COLAGENO CON ANTIOXIDANTE',                               'BIOLINE', 5.1466666666666665),
('BIO30207', 'CREMA FACIAL SKIN PERFECT NOCHE CON ALOE VERA Y RETINOL',              'BIOLINE', 5.1466666666666665),
('BIO30208', 'CREMA FACIAL ANTI ARRUGAS ACIDO HIALURONICO Y VIT. E',                 'BIOLINE', 5.1466666666666665),
('BIO30209', 'SERUM ACIDO HIALURONICO Y COLAGENO',                                   'BIOLINE', 6.079999999999999),
('BIO30210', 'SERUM COLAGENO (HASTA AGOSTAR EXISTENCIA)',                            'BIOLINE', 6.079999999999999),
('BIO30211', 'SERUM NIACINAMIDA VITAMINA B3',                                        'BIOLINE', 6.079999999999999),
('BIO30212', 'SERUM DE VITAMINA C',                                                  'BIOLINE', 6.079999999999999),
('BIO30213', 'BODY CREAM FRAMBUESA',                                                 'BIOLINE', 5.746666666666666),
('BIO30214', 'BODY CREAM ORQUIDEA',                                                  'BIOLINE', 5.746666666666666),
('BIO30215', 'BODY CREAM MANZANA MELON',                                             'BIOLINE', 5.746666666666666),
('BIO30216', 'BODY CREAM ROSA',                                                      'BIOLINE', 5.746666666666666),
('BIO30217', 'BODY CREAM VAINILLA',                                                  'BIOLINE', 5.746666666666666),
('BIO30223', 'GEL ANTIBACTERIAL 70% ALCOHOL 1L',                                     'BIOLINE', 12.266666666666666),
('BIO30224', 'GEL ANTIBACTERIAL 70% ALCOHOL 60ml',                                   'BIOLINE', 1.5333333333333332),
('BIO30225', 'DESODORANTE ACLARANTE',                                                'BIOLINE', 2.3066666666666666),
('BIO30226', 'DESODORANTE UNISEX',                                                   'BIOLINE', 1.5333333333333332),

-- ================= AMAZONIA SECRET (3) =================
('AMA31001', 'CHAMPU EXTRA NATURAL CEBOLLA MORADA',                                  'AMAZONIA SECRET', 2.6666666666666665),
('AMA31002', 'TRATAMIENTO INTENSIVO DE CEBOLLA MORADA - SOLO EN COMBO',              'AMAZONIA SECRET', 3.3333333333333335),
('AMA31003', 'ACONDICIONADOR CEBOLLA MORADA',                                        'AMAZONIA SECRET', 3.3333333333333335),

-- ================= KEDAM (8) =================
('KED32001', 'CHAMPU ANTICAIDA',                                                     'KEDAM', 4.666666666666667),
('KED32002', 'CHAMPU HIDRATACION',                                                   'KEDAM', 4.666666666666667),
('KED32003', 'CHAMPU 2 en 1',                                                        'KEDAM', 4.666666666666667),
('KED32004', 'ACONDICIONADOR FLORES TROPICALES',                                     'KEDAM', 4.666666666666667),
('KED32005', 'CHAMPU CEBOLLA',                                                       'KEDAM', 4.666666666666667),
('KED32006', 'CHAMPU PARA NIÑOS',                                                    'KEDAM', 4.666666666666667),
('KED32007', 'CHAMPU ANTICASPA',                                                     'KEDAM', 4.666666666666667),
('KED32008', 'CHAMPU FRESH CON LECHE DE COCO',                                       'KEDAM', 4.666666666666667),

-- ================= DEPIL CLEAR (12) =================
('DEP30501', 'ACEITE POST DEPIL MANZANILLA',                                         'DEPIL CLEAR', 3.3333333333333335),
('DEP30502', 'ACEITE POST DEPIL ARGAN',                                              'DEPIL CLEAR', 3.3333333333333335),
('DEP30503', 'ACEITE POST DEPIL ALMENDRAS',                                          'DEPIL CLEAR', 3.3333333333333335),
('DEP30504', 'AMPOLLA POST DEPILACION',                                              'DEPIL CLEAR', 1.2),
('DEP30505', 'DEPILIA TIRAS DEPILATORIAS',                                           'DEPIL CLEAR', 4.666666666666667),
('DEP30506', 'DEPILIA ROLLO DE DEPILACION',                                          'DEPIL CLEAR', 13),
('DEP30508', 'CERA LATA MANZANA VERDE (DEPIL CLEAR)',                                'DEPIL CLEAR', 13),
('DEP30509', 'CERA LATA MIEL (DEPIL CLEAR)',                                         'DEPIL CLEAR', 0),
('DEP30510', 'CERA LATA BANANA (DEPIL CLEAR)',                                       'DEPIL CLEAR', 13),
('DEP30511', 'CERA LATA TALCO (DEPIL CLEAR)',                                        'DEPIL CLEAR', 13),
('DEP30513', 'CALENTADOR DE CERA DEPILWAX 800 gr',                                   'DEPIL CLEAR', 90),
('DEP30514', 'CALENTADOR DE CERA DEPILWAX 1000 gr',                                  'DEPIL CLEAR', 104),

-- ================= ESTILISTA (12) =================
('EST30601', 'CAPA PARA TINTE PLASTICA DESCARTABLE X 30 PIEZAS',                     'ESTILISTA', 13.333333333333334),
('EST30602', 'CAPA COLORES SURTIDO',                                                 'ESTILISTA', 8),
('EST30806', 'PAÑUELO COSMETICO MULTIUSO 48 PIEZAS',                                 'ESTILISTA', 4.1066666666666665),
('EST30807', 'PAÑUELO COSMETICO MULTIUSO 40 PIEZAS',                                 'ESTILISTA', 4),
('EST30808', 'GORRO BAÑO AZUL OSCURO',                                               'ESTILISTA', 2),
('EST30609', 'GORRO DE BAÑO AMARILLO',                                               'ESTILISTA', 2),
('EST30610', 'GORRO DE BAÑO VERDE',                                                  'ESTILISTA', 2),
('EST30613', 'PEINE NARANJA GRANDE',                                                 'ESTILISTA', 1.3333333333333333),
('EST30614', 'PEINE NARANJA PEQUEÑO',                                                'ESTILISTA', 1.3333333333333333),
('EST30612', 'PEINE NEGRO CON EMPAQUE',                                              'ESTILISTA', 1.3333333333333333),
('EST30615', 'PORTA HILO DENTAL',                                                    'ESTILISTA', 1.3333333333333333),
('EST30616', 'PEINE MARRON',                                                         'ESTILISTA', 1.3333333333333333),

-- ================= CUTIQUE (3) =================
('CUT32001', 'MEN SHAMPOO CUTIQUE CONTROL DE CASPA 300 ML',                          'CUTIQUE', 0),
('CUT32002', 'MEN SHAMPOO CUTIQUE CONTROL DE CAIDA 300 ML',                          'CUTIQUE', 0),
('CUT32003', 'MEN 3 EN 1 CARA, CUERPO Y CABELLO 300 ML',                             'CUTIQUE', 0);

-- ============================================================
-- 2. UPSERT: inserta o actualiza + kardex 500 und
-- ============================================================
WITH upsert AS (
    INSERT INTO product (product_code, name, category, unit_price_usd, stock_quantity, is_active)
    SELECT product_code, name, category, unit_price_usd, 500, true
    FROM tmp_carga_inicial
    ON CONFLICT (product_code) DO UPDATE
        SET stock_quantity = COALESCE(product.stock_quantity, 0) + 500,
            unit_price_usd = EXCLUDED.unit_price_usd,
            name           = EXCLUDED.name,
            category       = EXCLUDED.category,
            is_active      = true
    RETURNING id_product, product_code, unit_price_usd
)
INSERT INTO stock_movement (
    movement_date,
    id_product,
    quantity,
    movement_type,
    reason,
    document_type,
    document_number,
    sold_as,
    unit_price_usd
)
SELECT
    CURRENT_TIMESTAMP,
    id_product,
    500,
    'ENTRADA',
    'CARGA INICIAL',
    'INVENTARIO',
    'INICIAL-' || product_code,
    'Producto',
    unit_price_usd
FROM upsert;

COMMIT;
