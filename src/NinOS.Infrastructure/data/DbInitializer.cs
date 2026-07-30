using System;
using System.Linq;
using NinOS.Domain;

namespace NinOS.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void initialize(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));

            db_context.Database.EnsureCreated();

            if (!db_context.sellers.Any())
            {
                db_context.sellers.AddRange(
                    new seller("Sandra", "S-001"),
                    new seller("Anais", "A-001"),
                    new seller("Alejandra", "A-002")
                );
            }

            if (!db_context.customers.Any())
            {
                db_context.customers.AddRange(
                    new customer("C-001", "Carlos Perez", "0414-1234567", "Valencia"),
                    new customer("C-002", "Maria Gomez", "0412-7654321", "Naguanagua")
                );
            }

            if (!db_context.products.Any())
            {
                product product_1 = new product("OLE30300", "OLEO'S AMPOLLA ANTICAIDA 24 UNDS.   OLEOS", "Óleos", 1.11m, 3);
                product product_2 = new product("OLE30302", "OLEO'S AMPOLLA ANTI- FRIZZ OLEOS", "Óleos", 1.11m, 4);
                product product_3 = new product("OLE30303", "OLEO'S AMPOLLA ALISADORA  OLEOS", "Óleos", 1.11m, 3);
                product product_4 = new product("OLE30304", "OLEO'S AMPOLLA C.DE SABILA/ACEITE OLIVA     OLEOS", "Óleos", 1.11m, 4);
                product product_5 = new product("S-C-001", "OLEO'S AMPOLLA ACONDICIONADORA Y SUAVIZANTE CON ACEITE DE OLIVA    OLEOS", "Óleos", 1.11m, 3);
                product product_6 = new product("S-C-002", "OLEO'S AMPOLLA MEZCLA DE TINTE  OLEOS", "Óleos", 1.11m, 4);
                product product_7 = new product("S-C-003", "OLEO'S AMPOLLA CUBRE CANA   OLEOS", "Óleos", 1.11m, 3);
                product product_8 = new product("REM30401", "AMPOLLA ANTI CAIDA x 10   REMBRANT", "Rembrandt", 1.21m, 4);
                product product_9 = new product("REM30402", "AMPOLLA GOTAS DE SEDA x 10 REMBRANT", "Rembrandt", 1.21m, 3);
                product product_10 = new product("REM30403", "AMPOLLA SEMILINO  x 10 REMBRANT", "Rembrandt", 1.21m, 4);
                product product_11 = new product("REM30404", "AMPOLLA PHYTO KERATINA  x 10  REMBRANT", "Rembrandt", 1.21m, 3);
                product product_12 = new product("TRI", "AMPOLLA PLACENTA DE OVEJO  x 24  REMBRANT", "Rembrandt", 1.21m, 4);
                product product_13 = new product("DEF30004", "AMPOLLA K-BOTROX HIDRATANTE", "Defile", 1.39m, 3);
                product product_14 = new product("DEF30005", "AMPOLLA K-BOTROX ACONDICIONADOR", "Defile", 1.39m, 4);
                product product_15 = new product("DEF30007", "AMPOLLA REGULADOR (CABELLOS GRASOS)", "Defile", 1.39m, 3);
                product product_16 = new product("DEF30008", "AMPOLLA ACEITE DE ARGAN ACONDICIONADOR", "Defile", 1.39m, 4);
                product product_17 = new product("DEF30009", "AMPOLLA ACEITE DE ARGAN SUAVIDAD", "Defile", 1.39m, 3);
                product product_18 = new product("DEF30012", "AMPOLLA ANTICAIDA (FORTALECE LA RAIZ)", "Defile", 1.39m, 4);
                product product_19 = new product("DEF30013", "AMPOLLA KERATINA DEFILE", "Defile", 1.39m, 3);
                product product_20 = new product("DEF30014", "AMPOLLA SILICON Y SEDA", "Defile", 1.39m, 4);
                product product_21 = new product("DEF30015", "AMPOLLA ANTICASPA", "Defile", 1.39m, 3);
                product product_22 = new product("DEF30017", "AMPOLLA KERATINA SHOCK", "Defile", 1.39m, 4);
                product product_23 = new product("AC", "AMPOLLA SEMILINO DEFILE", "Defile", 1.39m, 3);
                product product_24 = new product("DEF30019", "AMPOLLA PLACENTA DE OVEJO DEFILE", "Defile", 1.39m, 4);
                product product_25 = new product("DEF30020", "AMPOLLA CRISTAL DE SAVILA", "Defile", 1.39m, 3);
                product product_26 = new product("DEF30022", "AMPOLLA MEZCLA TINTE DEFILE", "Defile", 1.39m, 4);
                product product_27 = new product("DEF30023", "AMPOLLA LISO Y BRILLO", "Defile", 1.39m, 3);
                product product_28 = new product("DEF30024", "AMPOLLA UVA THERAPY", "Defile", 1.39m, 4);
                product product_29 = new product("DEF30025", "AMPOLLA CUBRE CANAS DEFILE", "Defile", 1.39m, 3);
                product product_30 = new product("DEF30026", "AMPOLLA ACEITE MACADAMIA NUTRI (NUTRE)", "Defile", 1.39m, 4);
                product product_31 = new product("DEF30027", "AMPOLLA ACEITE MACADAMIA HIDRATACION (DEFILE)", "Defile", 1.39m, 3);
                product product_32 = new product("DEF30029", "AMPOLLA LECHE DE ALMENDRA", "Defile", 1.39m, 4);
                product product_33 = new product("DEF30002", "AMPOLLA   MATIZADORA  (tipo Embudo)", "Defile", 1.69m, 3);
                product product_34 = new product("DEF30010", "AMPOLLA BIOTINA (FORTALECE LA FIBRAS CAPILARES)", "Defile", 1.69m, 4);
                product product_35 = new product("DEF30011", "AMPOLLA TRICOMPLEX VITAMINA E", "Defile", 1.69m, 3);
                product product_36 = new product("DEF30016", "AMPOLLA KERATINA PLANCHADO EXPRESS", "Defile", 1.69m, 4);
                product product_37 = new product("DEF30021", "AMPOLLA SBLOCK 27", "Defile", 1.69m, 3);
                product product_38 = new product("DEF30028", "AMPOLLA ISOSFOLIEX HAIR SPA", "Defile", 1.69m, 4);
                product product_39 = new product("OLE30301", "OLEO'S AMPOLLA COMPLEX (Hidratacion intensiva)", "Óleos", 1.69m, 3);
                product product_40 = new product("DEF30003", "AMPOLLA TRICOMPLEX CON ACIDO HIALURONICO", "Defile", 1.84m, 4);
                product product_41 = new product("DEF30030", "AMPOLLA TRICOMPLEX  MATIZADOR  (tipo embudo)", "Defile", 2.45m, 3);
                product product_42 = new product("DEF30001", "AMPOLLA TRICOMPLEX  MATIZADOR  (tipo vial)", "Defile", 2.76m, 4);
                product product_43 = new product("DEF30006", "AMPOLLA K-BOTROX 3 (Ultra Hidratante D-Phantenol)", "Defile", 2.76m, 3);
                product product_44 = new product("DEF30100", "PRE-TRATAMIENTO TRICOMPLEX MATIZADOR", "Defile", 5.83m, 4);
                product product_45 = new product("DEF30101", "TRATAMIENTO INTENSIVO TRICOMPLEX MATIZADORA", "Defile", 5.60m, 3);
                product product_46 = new product("DEF30102", "PRE-TRATAMIENTO  TRICOMPLEX  CON VITAMINA E", "Defile", 5.83m, 4);
                product product_47 = new product("DEF30103", "TRATAMIENTO INTENSIVO TRICOMPLEX  CON VITAMINA E", "Defile", 5.60m, 3);
                product product_48 = new product("DEF30104", "PRE-TRATAMIENTO TRICOMPLEX CON ACIDO HIALURONICO", "Defile", 5.83m, 4);
                product product_49 = new product("DEF30105", "TRATAMIENTO INTENSIVO TRICOMPLEX CON ACIDO HIALURONICO", "Defile", 5.60m, 3);
                product product_50 = new product("DEF30106", "PRE-TRATAMIENTO ACIDO HIALURONICO. (BLANCO)", "Defile", 5.59m, 4);
                product product_51 = new product("DEF30107", "TRATAMIENTO INTENSIVO ACIDO HIALURONICO.  (BLANCO)", "Defile", 5.68m, 3);
                product product_52 = new product("DEF30108", "PRE-TRATAMIENTO K-BOTROX", "Defile", 5.60m, 4);
                product product_53 = new product("DEF30109", "TRATAMIENTO INTENSIVO K-BOTROX", "Defile", 5.45m, 3);
                product product_54 = new product("DEF30110", "PRE-TRATAMIENTO REGULADOR", "Defile", 5.60m, 4);
                product product_55 = new product("DEF30111", "TRATAMIENTO INTENSIVO REGULADOR", "Defile", 5.45m, 3);
                product product_56 = new product("DEF30112", "PRE-TRATAMIENTO ARGAN", "Defile", 5.83m, 4);
                product product_57 = new product("DEF30113", "TRATAMIENTO INTENSIVO ACEITE DE ARGAN", "Defile", 5.52m, 3);
                product product_58 = new product("DEF30114", "PRE-TRATAMIENTO BIOTINA DAMA", "Defile", 5.60m, 4);
                product product_59 = new product("DEF30115", "PRE-TRATAMIENTO BIOTINA CABALLERO", "Defile", 5.60m, 3);
                product product_60 = new product("DEF30116", "CHAMPU PROFESIONAL  PH NEUTRO  2 Lt", "Defile", 8.85m, 4);
                product product_61 = new product("DEF30117", "PRE-TRATAMIENTO PH NEUTRO GALÓN - AGOTADO", "Defile", 15.33m, 3);
                product product_62 = new product("DEF30118", "POST  TRATAMIENTO PH NEUTRO GALÓN - AGOTADO", "Defile", 15.33m, 4);
                product product_63 = new product("DEF30119", "SUERO CAPILAR  K-BOTROX", "Defile", 4.60m, 3);
                product product_64 = new product("DEF30120", "ACEITE DE ARGAN CAPILAR", "Defile", 5.37m, 4);
                product product_65 = new product("DEF30121", "ACTIVADOR DE RIZOS", "Defile", 6.57m, 3);
                product product_66 = new product("DEF30122", "CREMA DESENREDANTE 250 ML", "Rembrandt", 6.57m, 4);
                product product_67 = new product("DEF30123", "CREMA ALISADORA  SUAVE   CON   KERATINA   -  AGOTADO", "Rembrandt", 3.07m, 3);
                product product_68 = new product("DEF30124", "CREMA ALISADORA  FUERTE   CON   KERATINA", "Rembrandt", 5.33m, 4);
                product product_69 = new product("DEF30125", "POLVO DECOLORANTE DEFILE", "Defile", 17.71m, 3);
                product product_70 = new product("DEF30126", "CIRUGIA LISS EVOLUTION 911 KIT-DE 3", "Defile", 32.20m, 4);
                product product_71 = new product("DEF30127", "CIRUGIA LISS EVOLUTION 911 KIT-DE 2", "Defile", 27.60m, 3);
                product product_72 = new product("DEF30128", "LISS EVOLUTION 911 SPRAY PROTEC TERMICO", "Defile", 6.63m, 4);
                product product_73 = new product("DEF30129", "TONICO CAPILAR ISOSFOLIEX", "Defile", 5.75m, 3);
                product product_74 = new product("DEF30130", "DESENGRASANTE MULTIUSO GALÓN", "Defile", 12.27m, 4);
                product product_75 = new product("DEF30131", "AGUA OXIGENADA VOL. 20", "Defile", 1.08m, 3);
                product product_76 = new product("DEF30132", "AGUA OXIGENADA VOL. 30", "Defile", 1.08m, 4);
                product product_77 = new product("DEF30135", "BALSAMO  PROFESIONAL  PH NEUTRO  2 Lt", "Defile", 8.85m, 3);
                product product_78 = new product("BIO30200", "AGUA MISCELAR", "Bioline", 5.15m, 4);
                product product_79 = new product("BIO30201", "LOCION DESMAQUILLANTE", "Bioline", 3.96m, 3);
                product product_80 = new product("BIO30202", "AGUA DE ROSAS", "Bioline", 5.15m, 4);
                product product_81 = new product("BIO30203", "LIMPIADOR FACIAL HIDRATANTE", "Bioline", 7.32m, 3);
                product product_82 = new product("BIO30204", "LIMPIADOR DE BROCHAS", "Bioline", 7.65m, 4);
                product product_83 = new product("BIO30205", "CREMA FACIAL REAFIRMANTE CON COLAGENO Y VIT. E", "Bioline", 5.15m, 3);
                product product_84 = new product("BIO30206", "CREMA FACIAL  COLAGENO CON ANTIOXIDANTE", "Bioline", 5.15m, 4);
                product product_85 = new product("BIO30207", "CREMA FACIAL SKIN PERFECT NOCHE CON ALOE VERA Y RETINOL", "Bioline", 5.15m, 3);
                product product_86 = new product("BIO30208", "CREMA FACIAL ANTI ARRUGAS ACIDO HIALURONICO Y VIT. E", "Bioline", 5.15m, 4);
                product product_87 = new product("BIO30209", "SERUM ACIDO HIALURONICO Y COLAGENO", "Bioline", 6.08m, 3);
                product product_88 = new product("BIO30210", "SERUM COLAGENO (HASTA AGOSTAR EXISTENCIA)", "Bioline", 6.08m, 4);
                product product_89 = new product("BIO30211", "SERUM NIACINAMIDA VITAMINA B3", "Bioline", 6.08m, 3);
                product product_90 = new product("BIO30212", "SERUM DE VITAMINA C", "Bioline", 6.08m, 4);
                product product_91 = new product("BIO30213", "BODY CREAM FRAMBUESA", "Bioline", 5.75m, 3);
                product product_92 = new product("BIO30214", "BODY CREAM ORQUIDEA", "Bioline", 5.75m, 4);
                product product_93 = new product("BIO30215", "BODY CREAM MANZANA MELON", "Bioline", 5.75m, 3);
                product product_94 = new product("BIO30216", "BODY CREAM ROSA", "Bioline", 5.75m, 4);
                product product_95 = new product("BIO30217", "BODY CREAM VAINILLA", "Bioline", 5.75m, 3);
                product product_96 = new product("BIO30223", "GEL ANTIBACTERIAL 70% ALCOHOL", "Bioline", 12.27m, 4);
                product product_97 = new product("BIO30225", "DESODORANTE ACLARANTE", "Bioline", 2.31m, 3);
                product product_98 = new product("BIO30226", "DESODORANTE UNISEX", "Bioline", 1.53m, 4);
                product product_99 = new product("OLE30305", "OLEO'S SHAMPOO CONTROL FRIZZ", "Óleos", 6.40m, 3);
                product product_100 = new product("OLE30306", "OLEO'S ACONDICIONADOR CONTROL FRIZZ", "Óleos", 6.40m, 4);
                product product_101 = new product("OLE30307", "OLEO'S SHAMPOO CONTROL CAIDA", "Óleos", 6.40m, 3);
                product product_102 = new product("OLE30308", "OLEO'S ACONDICIONADOR CONTROL CAIDA", "Óleos", 6.40m, 4);
                product product_103 = new product("OLE30309", "OLEO'S SHAMPOO RESTAURADOR", "Óleos", 6.40m, 3);
                product product_104 = new product("OLE30310", "OLEO'S ACONDICIONADOR  RESTAURADOR", "Óleos", 6.40m, 4);
                product product_105 = new product("OLE30311", "OLEO'S SHAMPOO CONTROL CASPA", "Óleos", 6.40m, 3);
                product product_106 = new product("OLE30312", "OLEO'S ACONDICIONADOR  CONTROL CASPA", "Óleos", 6.40m, 4);
                product product_107 = new product("OLE30313", "OLEO'S SHAMPOO CUIDADO DIARIO", "Óleos", 6.40m, 3);
                product product_108 = new product("OLE30314", "OLEO'S ACONDICIONADOR CUIDADO DIARIO", "Óleos", 6.40m, 4);
                product product_109 = new product("OLE30315", "OLEO'S SHAMPOO RIZOS DEFINIDOS", "Óleos", 6.40m, 3);
                product product_110 = new product("OLE30316", "OLEO'S ACONDICIONADOR  RIZOS DEFINIDOS", "Óleos", 6.40m, 4);
                product product_111 = new product("OLE30317", "OLEO'S  MASCARILLA HIDRATANTE RESTAURADORA", "Óleos", 6.40m, 3);
                product product_112 = new product("REM30406", "PRE-TRATAMIENTO PLACENTA OVEJO  1 LITRO -", "Rembrandt", 5.33m, 4);
                product product_113 = new product("REM30407", "TRATAMIENTO INTENSIVO PLACENTA OVEJO  400 GR", "Rembrandt", 4.67m, 3);
                product product_114 = new product("REM30408", "Pre-Tratamiento Argán 360 ml REMBRANDT", "Rembrandt", 5.01m, 4);
                product product_115 = new product("REM30409", "Post-Tratamiento Aceite/Argán 360ml REMBRANDT", "Rembrandt", 5.11m, 3);
                product product_116 = new product("REM30410", "Tratamiento Intensivo Capilar Baño de Crema Aceite/Argán 240ml REMBRANDT", "Rembrandt", 4.93m, 4);
                product product_117 = new product("REM30411", "Crema Reafirmante con Colageno y Vitamina E 60 Grs. REMBRANDT", "Rembrandt", 5.13m, 3);
                product product_118 = new product("REM30412", "Agua Micelar  120 ML. REMBRANDT", "Rembrandt", 5.13m, 4);
                product product_119 = new product("REM30413", "Locion Desmaquillante  120 ML. REMBRANDT", "Rembrandt", 3.96m, 3);
                product product_120 = new product("REM30414", "Crema Corporal Hidratante  400 ML. REMBRANDT", "Rembrandt", 5.75m, 4);
                product product_121 = new product("REM30415", "Body Splah Frambuesa Desire  240 ML. REMBRANDT", "Rembrandt", 4.91m, 3);
                product product_122 = new product("REM30416", "Body Splah Vainilla  Rocio  240 ML. REMBRANDT", "Rembrandt", 4.91m, 4);
                product product_123 = new product("REM30417", "AGUA DE ROSA  REMBRANDT 120 ML", "Rembrandt", 5.13m, 3);
                product product_124 = new product("REM30418", "KID´S HAIR CLEAN CHAMPU NIÑOS", "Rembrandt", 3.33m, 4);
                product product_125 = new product("REM30419", "PRE - TRATAMIENTO PLACENTA OVEJO   500 ML", "Rembrandt", 4.00m, 3);
                product product_126 = new product("AMA31001", "CHAMPU EXTRA NATURAL CEBOLLA MORADA", "Amazonia Secret", 2.67m, 4);
                product product_127 = new product("AMA31002", "TRATAMIENTO INTENSIVO DE CEBOLLA MORADA - SOLO EN COMBO", "Amazonia Secret", 3.33m, 3);
                product product_128 = new product("AMA31003", "ACONDICIONADOR  CEBOLLA   MORADA - AGOTADO", "Amazonia Secret", 3.33m, 4);
                product product_129 = new product("KED32001", "CHAMPU  ANTICAIDA", "Kedam", 4.67m, 3);
                product product_130 = new product("KED32002", "CHAMPU  HIDRATACION", "Kedam", 4.67m, 4);
                product product_131 = new product("KED32003", "CHAMPU  2 en 1", "Kedam", 4.67m, 3);
                product product_132 = new product("KED32004", "ACONDICIONADOR FLORES TROPICALES", "Kedam", 4.67m, 4);
                product product_133 = new product("KED32005", "CHAMPU CEBOLLA", "Kedam", 4.67m, 3);
                product product_134 = new product("KED32006", "CHAMPU PARA NIÑOS", "Kedam", 4.67m, 4);
                product product_135 = new product("KED32007", "CHAMPU ANTICASPA", "Kedam", 4.67m, 3);
                product product_136 = new product("KED32008", "CHAMPU FRESH CON LECHE DE COCO", "Kedam", 4.67m, 4);
                product product_137 = new product("DEP30501", "ACEITE POST DEPIL MANZANILLA", "Depil Clear", 3.33m, 3);
                product product_138 = new product("DEP30502", "ACEITE POST DEPIL ARGAN", "Depil Clear", 3.33m, 4);
                product product_139 = new product("DEP30503", "ACEITE POST DEPIL ALMENDRAS", "Depil Clear", 3.33m, 3);
                product product_140 = new product("DEP30504", "AMPOLLA POST DEPILACION", "Depil Clear", 1.20m, 4);
                product product_141 = new product("DEP30505", "DEPILIA TIRAS DEPILATORIAS", "Depil Clear", 4.67m, 3);
                product product_142 = new product("DEP30506", "DEPILIA ROLLO DE DEPILACION", "Depil Clear", 13.00m, 4);
                product product_143 = new product("DEP30508", "CERA LATA MANZANA VERDE (DEPIL CLEAR )", "Depil Clear", 13.00m, 3);
                product product_144 = new product("DEP30510", "CERA LATA BANANA (DEPIL CLEAR )", "Depil Clear", 13.00m, 4);
                product product_145 = new product("DEP30511", "CERA LATA TALCO (DEPIL CLEAR )", "Depil Clear", 13.00m, 3);
                product product_146 = new product("DEP30513", "CALENTADOR DE CERA DEPILWAX", "Depil Clear", 90.00m, 4);
                product product_147 = new product("EST30601", "CAPA PARA TINTE PLASTICA DESCARTABLE  X 30 PIEZAS", "Estilista", 13.33m, 3);
                product product_148 = new product("EST30602", "CAPA COLORES SURTIDO", "Estilista", 8.00m, 4);
                product product_149 = new product("EST30806", "PAÑUELO COSMETICO MULTIUSO 48 PIEZAS", "Estilista", 4.11m, 3);
                product product_150 = new product("EST30807", "PAÑUELO COSMETICO MULTIUSO 40 PIEZAS", "Estilista", 4.00m, 4);
                product product_151 = new product("EST30808", "GORRO BAÑO AZUL OSCURO", "Estilista", 2.00m, 3);
                product product_152 = new product("EST30609", "GORRO DE BAÑO AMARILLO", "Estilista", 2.00m, 4);
                product product_153 = new product("EST30610", "GORRO DE BAÑO VERDE", "Estilista", 2.00m, 3);
                product product_154 = new product("EST30613", "PEINE NARANJA GRANDE", "Estilista", 1.33m, 4);
                product product_155 = new product("EST30614", "PEINE NARANJA PEQUEÑO", "Estilista", 1.33m, 3);
                product product_156 = new product("EST30612", "PEINE NEGRO CON EMPAQUE", "Estilista", 1.33m, 4);
                product product_157 = new product("EST30615", "PORTA HILO  DENTAL", "Estilista", 1.33m, 3);
                product product_158 = new product("EST30616", "PEINE MARRON", "Estilista", 1.33m, 4);
                db_context.products.AddRange(product_1, product_2, product_3, product_4, product_5, product_6, product_7, product_8, product_9, product_10, product_11, product_12, product_13, product_14, product_15, product_16, product_17, product_18, product_19, product_20);
                db_context.products.AddRange(product_21, product_22, product_23, product_24, product_25, product_26, product_27, product_28, product_29, product_30, product_31, product_32, product_33, product_34, product_35, product_36, product_37, product_38, product_39, product_40);
                db_context.products.AddRange(product_41, product_42, product_43, product_44, product_45, product_46, product_47, product_48, product_49, product_50, product_51, product_52, product_53, product_54, product_55, product_56, product_57, product_58, product_59, product_60);
                db_context.products.AddRange(product_61, product_62, product_63, product_64, product_65, product_66, product_67, product_68, product_69, product_70, product_71, product_72, product_73, product_74, product_75, product_76, product_77, product_78, product_79, product_80);
                db_context.products.AddRange(product_81, product_82, product_83, product_84, product_85, product_86, product_87, product_88, product_89, product_90, product_91, product_92, product_93, product_94, product_95, product_96, product_97, product_98, product_99, product_100);
                db_context.products.AddRange(product_101, product_102, product_103, product_104, product_105, product_106, product_107, product_108, product_109, product_110, product_111, product_112, product_113, product_114, product_115, product_116, product_117, product_118, product_119, product_120);
                db_context.products.AddRange(product_121, product_122, product_123, product_124, product_125, product_126, product_127, product_128, product_129, product_130, product_131, product_132, product_133, product_134, product_135, product_136, product_137, product_138, product_139, product_140);
                db_context.products.AddRange(product_141, product_142, product_143, product_144, product_145, product_146, product_147, product_148, product_149, product_150, product_151, product_152, product_153, product_154, product_155, product_156, product_157, product_158);
                promotion promo_1 = new promotion("PROMO-001", "PRE-TRATAMIENTO BIOTINA DAMA MAS AMPOLLA BIOTINA 10 ML (CHAMPO Y AMPOLLA)", "Defile", 4.90m);
                promotion promo_2 = new promotion("KIT-001", "AMAZONIA CHAMPO CEBOLLA MORADA MAS BAÑO DE CREMA CEBOLLA MORADA", "Amazonia Secret", 5.90m);
                promotion promo_3 = new promotion("KIT-002", "KIT LINEA BLANCA DEFILE CHAMPO K BOTROX 360 ML BALSAMO K BOTROX 240 ML", "Defile", 5.90m);
                promotion promo_4 = new promotion("KIT-003", "KIT LINEA BLANCA DEFILE CHAMPO TRICOMPLEX 360 ML CON AMPOLLA MAS BAÑO DE CREMA TRICOMPLEX 240 ML", "Defile", 5.90m);
                promotion promo_5 = new promotion("KIT-004", "KIT LINEA BLANCA DEFILE CHAMPO K BOTROX 360 ML MAS BALSAMO K BOTROX 360 ML", "Defile", 5.90m);
                promotion promo_6 = new promotion("PROMO-002", "DEFILE ROSA PRE-TRATAMIENTO TRICOMPLEX CON ACIDO HIALURONICO CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_7 = new promotion("PROMO-003", "DEFILE ROSA PRE-TRATAMIENTO TRICOMPLEX CON ACIDO HIALURONICO CHAMPO Y CHAMPO 2 X 1", "Defile", 5.90m);
                promotion promo_8 = new promotion("PROMO-004", "DEFILE ROSA PRE-TRATAMIENTO MATIZADOR TRICOMPLEX CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_9 = new promotion("PROMO-005", "DEFILE ROSA PRE-TRATAMIENTO REGULADOR DE GRASA CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_10 = new promotion("PROMO-006", "DEFILE ROSA PRE-TRATAMIENTO ACIDO HIALURONICO CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_11 = new promotion("PROMO-007", "DEFILE ROSA PRE-TRATAMIENTO ARGAN CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_12 = new promotion("PROMO-008", "DEFILE ROSA PRE-TRATAMIENTO TRICOMPLEX VITAMINA E CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_13 = new promotion("PROMO-009", "DEFILE ROSA PRE-TRATAMIENTO K BOTROX CHAMPO Y BAÑO DE CREMA 2 X 1", "Defile", 5.90m);
                promotion promo_14 = new promotion("PROMO-010", "DEFILE ROSA PRE-TRATAMIENTO MATIZADOR TRICOMPLEX CHAMPO Y CHAMPO 2 X 1", "Defile", 5.90m);
                promotion promo_15 = new promotion("KIT-005", "CHAMPO PLACENTA OVEJO 1 LITRO Y BAÑO DE CREMA 400 GR", "Rembrandt", 8.50m);
                promotion promo_16 = new promotion("DEF30126", "CIRUGIA LISS EVOLUTION 911 (KIT-DE 3 PASOS)", "Defile", 25.00m);
                promotion promo_17 = new promotion("DEF30127", "CIRUGIA LISS EVOLUTION 911 (KIT-DE 2 PASOS)", "Defile", 20.00m);
                promotion promo_18 = new promotion("DEP30508", "CERA LATA MANZANA VERDE (DEPIL CLEAR)", "Depil Clear", 8.00m);
                promotion promo_19 = new promotion("DEP30510", "CERA LATA MIEL BANANA (DEPIL CLEAR)", "Depil Clear", 8.00m);
                promotion promo_20 = new promotion("DEP30511", "CERA LATA TALCO (DEPIL CLEAR)", "Depil Clear", 8.00m);
                db_context.promotions.AddRange(promo_1, promo_2, promo_3, promo_4, promo_5, promo_6, promo_7, promo_8, promo_9, promo_10, promo_11, promo_12, promo_13, promo_14, promo_15, promo_16, promo_17, promo_18, promo_19, promo_20);
                db_context.SaveChanges();
                var prod_1_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30114");
                var prod_1_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30010");
                if (prod_1_1 != null) db_context.promotion_items.Add(new promotion_item(prod_1_1.id_product, 1) { id_promotion = promo_1.id_promotion });
                if (prod_1_2 != null) db_context.promotion_items.Add(new promotion_item(prod_1_2.id_product, 1) { id_promotion = promo_1.id_promotion });
                var prod_2_1 = db_context.products.FirstOrDefault(p => p.product_code == "AMA31001");
                var prod_2_2 = db_context.products.FirstOrDefault(p => p.product_code == "AMA31002");
                if (prod_2_1 != null) db_context.promotion_items.Add(new promotion_item(prod_2_1.id_product, 1) { id_promotion = promo_2.id_promotion });
                if (prod_2_2 != null) db_context.promotion_items.Add(new promotion_item(prod_2_2.id_product, 1) { id_promotion = promo_2.id_promotion });
                var prod_3_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30108");
                var prod_3_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30109");
                if (prod_3_1 != null) db_context.promotion_items.Add(new promotion_item(prod_3_1.id_product, 1) { id_promotion = promo_3.id_promotion });
                if (prod_3_2 != null) db_context.promotion_items.Add(new promotion_item(prod_3_2.id_product, 1) { id_promotion = promo_3.id_promotion });
                var prod_4_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30104");
                var prod_4_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30105");
                var prod_4_3 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30003");
                if (prod_4_1 != null) db_context.promotion_items.Add(new promotion_item(prod_4_1.id_product, 1) { id_promotion = promo_4.id_promotion });
                if (prod_4_2 != null) db_context.promotion_items.Add(new promotion_item(prod_4_2.id_product, 1) { id_promotion = promo_4.id_promotion });
                if (prod_4_3 != null) db_context.promotion_items.Add(new promotion_item(prod_4_3.id_product, 1) { id_promotion = promo_4.id_promotion });
                var prod_5_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30108");
                var prod_5_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30109");
                if (prod_5_1 != null) db_context.promotion_items.Add(new promotion_item(prod_5_1.id_product, 1) { id_promotion = promo_5.id_promotion });
                if (prod_5_2 != null) db_context.promotion_items.Add(new promotion_item(prod_5_2.id_product, 1) { id_promotion = promo_5.id_promotion });
                var prod_6_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30104");
                var prod_6_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30105");
                if (prod_6_1 != null) db_context.promotion_items.Add(new promotion_item(prod_6_1.id_product, 1) { id_promotion = promo_6.id_promotion });
                if (prod_6_2 != null) db_context.promotion_items.Add(new promotion_item(prod_6_2.id_product, 1) { id_promotion = promo_6.id_promotion });
                var prod_7_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30104");
                if (prod_7_1 != null) db_context.promotion_items.Add(new promotion_item(prod_7_1.id_product, 2) { id_promotion = promo_7.id_promotion });
                var prod_8_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30100");
                var prod_8_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30101");
                if (prod_8_1 != null) db_context.promotion_items.Add(new promotion_item(prod_8_1.id_product, 1) { id_promotion = promo_8.id_promotion });
                if (prod_8_2 != null) db_context.promotion_items.Add(new promotion_item(prod_8_2.id_product, 1) { id_promotion = promo_8.id_promotion });
                var prod_9_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30110");
                var prod_9_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30111");
                if (prod_9_1 != null) db_context.promotion_items.Add(new promotion_item(prod_9_1.id_product, 1) { id_promotion = promo_9.id_promotion });
                if (prod_9_2 != null) db_context.promotion_items.Add(new promotion_item(prod_9_2.id_product, 1) { id_promotion = promo_9.id_promotion });
                var prod_10_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30106");
                var prod_10_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30107");
                if (prod_10_1 != null) db_context.promotion_items.Add(new promotion_item(prod_10_1.id_product, 1) { id_promotion = promo_10.id_promotion });
                if (prod_10_2 != null) db_context.promotion_items.Add(new promotion_item(prod_10_2.id_product, 1) { id_promotion = promo_10.id_promotion });
                var prod_11_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30112");
                var prod_11_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30113");
                if (prod_11_1 != null) db_context.promotion_items.Add(new promotion_item(prod_11_1.id_product, 1) { id_promotion = promo_11.id_promotion });
                if (prod_11_2 != null) db_context.promotion_items.Add(new promotion_item(prod_11_2.id_product, 1) { id_promotion = promo_11.id_promotion });
                var prod_12_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30102");
                var prod_12_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30103");
                if (prod_12_1 != null) db_context.promotion_items.Add(new promotion_item(prod_12_1.id_product, 1) { id_promotion = promo_12.id_promotion });
                if (prod_12_2 != null) db_context.promotion_items.Add(new promotion_item(prod_12_2.id_product, 1) { id_promotion = promo_12.id_promotion });
                var prod_13_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30108");
                var prod_13_2 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30109");
                if (prod_13_1 != null) db_context.promotion_items.Add(new promotion_item(prod_13_1.id_product, 1) { id_promotion = promo_13.id_promotion });
                if (prod_13_2 != null) db_context.promotion_items.Add(new promotion_item(prod_13_2.id_product, 1) { id_promotion = promo_13.id_promotion });
                var prod_14_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30100");
                if (prod_14_1 != null) db_context.promotion_items.Add(new promotion_item(prod_14_1.id_product, 2) { id_promotion = promo_14.id_promotion });
                var prod_15_1 = db_context.products.FirstOrDefault(p => p.product_code == "REM30406");
                var prod_15_2 = db_context.products.FirstOrDefault(p => p.product_code == "REM30407");
                if (prod_15_1 != null) db_context.promotion_items.Add(new promotion_item(prod_15_1.id_product, 1) { id_promotion = promo_15.id_promotion });
                if (prod_15_2 != null) db_context.promotion_items.Add(new promotion_item(prod_15_2.id_product, 1) { id_promotion = promo_15.id_promotion });
                var prod_16_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30126");
                if (prod_16_1 != null) db_context.promotion_items.Add(new promotion_item(prod_16_1.id_product, 1) { id_promotion = promo_16.id_promotion });
                var prod_17_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30127");
                if (prod_17_1 != null) db_context.promotion_items.Add(new promotion_item(prod_17_1.id_product, 1) { id_promotion = promo_17.id_promotion });
                var prod_18_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30508");
                if (prod_18_1 != null) db_context.promotion_items.Add(new promotion_item(prod_18_1.id_product, 1) { id_promotion = promo_18.id_promotion });
                var prod_19_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30510");
                if (prod_19_1 != null) db_context.promotion_items.Add(new promotion_item(prod_19_1.id_product, 1) { id_promotion = promo_19.id_promotion });
                var prod_20_1 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30511");
                if (prod_20_1 != null) db_context.promotion_items.Add(new promotion_item(prod_20_1.id_product, 1) { id_promotion = promo_20.id_promotion });
                db_context.SaveChanges();
            }
        }
    }
}