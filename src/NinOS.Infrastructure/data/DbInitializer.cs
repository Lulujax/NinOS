using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NinOS.Domain;
using NinOS.Infrastructure.Common;

namespace NinOS.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void initialize(NinOSDbContext db_context)
        {
            if (db_context == null) throw new ArgumentNullException(nameof(db_context));

            db_context.Database.Migrate();

            cleanup_credit_note_payments_and_adjust_totals(db_context);
            migrate_legacy_series(db_context);
            initialize_zonas_and_sellers(db_context);
            initialize_product_lines(db_context);
            migrate_customer_correlatives_to_global5(db_context);

            const string brand_header = "DEFILE_REMBRANT_OLEOS_TRICOMPLEX";

            // Todos los tipos usan el header vigente. Se corrige en cada arranque para que
            // el cambio de marca no dependa de una migracion.
            var tipos_con_header = db_context.note_types
                .Where(t => t.header_title != brand_header)
                .ToList();
            if (tipos_con_header.Count > 0)
            {
                foreach (var t in tipos_con_header)
                {
                    t.header_title = brand_header;
                }
                db_context.SaveChanges();
            }

            var existing_maracay = db_context.note_types.FirstOrDefault(t => t.code == "MAR");
            if (existing_maracay == null)
            {
                db_context.note_types.Add(new note_type(
                    "Pro Venta", "MAR", brand_header, "standard", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 2
                });
                db_context.SaveChanges();
            }
            else
            {
                bool changed = false;
                if (existing_maracay.name != "Pro Venta") { existing_maracay.name = "Pro Venta"; changed = true; }
                if (existing_maracay.id_seller != null) { existing_maracay.id_seller = null; changed = true; }
                if (existing_maracay.mandatory_discount_percentage != null) { existing_maracay.mandatory_discount_percentage = null; changed = true; }
                if (changed) db_context.SaveChanges();
            }

            var existing_general = db_context.note_types.FirstOrDefault(t => t.code == "GEN");
            if (existing_general == null)
            {
                db_context.note_types.Add(new note_type(
                    "General", "GEN", brand_header, "standard", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 1
                });
                db_context.SaveChanges();
            }

            // Tipo "Promocion": disponible para los vendedores que no son Juan Luis. Por ahora identico a General.
            var existing_promocion = db_context.note_types.FirstOrDefault(t => t.code == "PRM");
            if (existing_promocion == null)
            {
                var promocion = new note_type(
                    "Promocion", "PRM", brand_header, "standard", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 6
                };
                db_context.note_types.Add(promocion);
                db_context.SaveChanges();
            }

            // Tipo "Promocion Pro Venta": promocion exclusiva de Juan Luis, tratada como Pro Venta.
            var existing_promo_pro_venta = db_context.note_types.FirstOrDefault(t => t.code == "PVP");
            if (existing_promo_pro_venta == null)
            {
                var promo_pro_venta = new note_type(
                    "Promocion Pro Venta", "PVP", brand_header, "promo", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 5,
                    promo_discount_percentage = 10m
                };
                db_context.note_types.Add(promo_pro_venta);
                db_context.SaveChanges();
            }
            else if (existing_promo_pro_venta.promo_discount_percentage == null)
            {
                existing_promo_pro_venta.promo_discount_percentage = 10m;
                db_context.SaveChanges();
            }

            // Tipos "Volumen": son una nota General (o Pro Venta) MAS el descuento por
            // volumen. Arrancan con el mismo descuento de condicion y las mismas
            // condiciones que su tipo base; lo unico que se suma es el renglon de volumen,
            // cuyo porcentaje elige el vendedor en cada nota.
            var volumen_seed = new (string code, string name, int sort_order)[]
            {
                (NoteTypeCodes.Volumen, "Volumen", 7),
                (NoteTypeCodes.VolumenProVenta, "Volumen Pro Venta", 8),
            };

            foreach (var (code, name, sort_order) in volumen_seed)
            {
                var tipo_volumen = db_context.note_types.FirstOrDefault(t => t.code == code);
                if (tipo_volumen == null)
                {
                    db_context.note_types.Add(new note_type(
                        name, code, brand_header, "standard", true, 10m,
                        "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                        "Descuento 10% SOLO\nCONTADO")
                    {
                        sort_order = sort_order,
                        es_volumen = true
                    });
                    db_context.SaveChanges();
                    continue;
                }

                // Ya existe: se le completa lo que le falta para comportarse como
                // General + volumen.
                bool changed = false;
                if (string.IsNullOrWhiteSpace(tipo_volumen.conditions_template))
                {
                    tipo_volumen.conditions_template = "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(tipo_volumen.discount_conditions_template))
                {
                    tipo_volumen.discount_conditions_template = "Descuento 10% SOLO\nCONTADO";
                    changed = true;
                }
                if (tipo_volumen.default_discount_percentage <= 0m)
                {
                    tipo_volumen.default_discount_percentage = 10m;
                    changed = true;
                }
                if (!tipo_volumen.es_volumen)
                {
                    tipo_volumen.es_volumen = true;
                    changed = true;
                }
                if (changed) db_context.SaveChanges();
            }

            // Tipos obsoletos (Promo Oleos, Canecalon, Hair Liss): se eliminan.
            // Las notas que los usan pasan a tipo General.
            var removed_type_codes = new[] { "PRO", "CAN", "HLS" };
            var removed_types = db_context.note_types
                .Where(t => removed_type_codes.Contains(t.code))
                .ToList();
            if (removed_types.Count > 0)
            {
                int? general_type_id = db_context.note_types
                    .Where(t => t.code == "GEN")
                    .Select(t => (int?)t.id_note_type)
                    .FirstOrDefault();

                var removed_type_ids = removed_types.Select(t => t.id_note_type).ToHashSet();
                var notes_of_removed_types = db_context.delivery_notes
                    .Where(n => n.note_type_id != null && removed_type_ids.Contains(n.note_type_id.Value))
                    .ToList();
                foreach (var note in notes_of_removed_types)
                {
                    note.note_type_id = general_type_id;
                }
                if (notes_of_removed_types.Count > 0) db_context.SaveChanges();

                db_context.note_types.RemoveRange(removed_types);
                db_context.SaveChanges();
            }

            if (!db_context.note_types.Any())
            {
                var general = new note_type(
                    "General", "GEN", brand_header, "standard", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 1
                };

                var pro_venta = new note_type(
                    "Pro Venta", "MAR", brand_header, "standard", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 2
                };

                var promocion = new note_type(
                    "Promocion", "PRM", brand_header, "standard", true, 10m,
                    "DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO",
                    "Descuento 10% SOLO\nCONTADO")
                {
                    sort_order = 3
                };

                db_context.note_types.AddRange(general, pro_venta, promocion);
                db_context.SaveChanges();
            }

            // Backfill: asigna una relacion semanal (correlativo global) a las notas Pro Venta existentes.
            // VOLMAR entra en la familia Pro Venta: tambien lleva relacion semanal.
            // is_pro_venta es un helper de C# y EF Core no lo traduce a SQL: la consulta
            // reventaba en el arranque. Se filtra contra el arreglo de codigos.
            var mar_type_ids = db_context.note_types
                .Where(t => t.code != null && NoteTypeCodes.pro_venta_codes.Contains(t.code))
                .Select(t => t.id_note_type)
                .ToList();

            if (mar_type_ids.Count > 0)
            {
                var mar_notes = db_context.delivery_notes
                    .Where(n => n.note_type_id != null && mar_type_ids.Contains(n.note_type_id.Value))
                    .ToList();

                var week_starts = mar_notes
                    .Select(n => monday_of(n.creation_date))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                bool created_any = false;
                int next_number = (db_context.relaciones.Max(r => (int?)r.relation_number) ?? 0) + 1;

                foreach (var wk in week_starts)
                {
                    if (db_context.relaciones.Any(r => r.week_start == wk)) continue;

                    db_context.relaciones.Add(new relacion(next_number, wk, wk.AddDays(6)));
                    next_number++;
                    created_any = true;
                }
                if (created_any) db_context.SaveChanges();

                if (mar_notes.Any(n => n.id_relacion == null))
                {
                    // Varias relaciones huecas comparten semana, asi que no se puede agrupar por
                    // week_start con ToDictionary: se toma la normal de cada semana y, si
                    // solo hay huecas, la primera.
                    var relation_by_week = db_context.relaciones
                        .GroupBy(r => r.week_start)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Where(r => !r.es_hueca).Select(r => (int?)r.id_relacion).FirstOrDefault()
                                ?? g.Min(r => r.id_relacion));

                    foreach (var n in mar_notes)
                    {
                        if (relation_by_week.TryGetValue(monday_of(n.creation_date), out int id_relacion))
                        {
                            n.id_relacion = id_relacion;
                        }
                    }
                    db_context.SaveChanges();
                }

                // Asegurar que las relaciones existentes esten numeradas de forma unica y estrictamente
                // cronologica (1, 2, 3...). Las relaciones huecas (cartera heredada del Excel)
                // conservan el numero que se les asigno al migrarlas: varias comparten la misma
                // semana, asi que ordenarlas por week_start las mezclaria entre si. Las normales
                // se numeran a partir de la 22 para no chocar con el indice unico.
                var all_relaciones = db_context.relaciones
                    .Where(r => !r.es_hueca)
                    .OrderBy(r => r.week_start)
                    .ToList();
                if (all_relaciones.Count > 0)
                {
                    // Las relaciones normales se numeran a partir de la mas alta que ya existe,
                    // para no invadir el rango de las huecas (cartera heredada en 149..179).
                    int max_number = db_context.relaciones.Max(r => (int?)r.relation_number) ?? 0;

                    bool needs_renumber = false;
                    for (int i = 0; i < all_relaciones.Count; i++)
                    {
                        if (all_relaciones[i].relation_number != max_number - all_relaciones.Count + 1 + i)
                        {
                            needs_renumber = true;
                            break;
                        }
                    }
                    if (needs_renumber)
                    {
                        // Usar base positiva alta para no chocar con el indice unico ni violar value > 0
                        int temp_base = 1000000;
                        for (int i = 0; i < all_relaciones.Count; i++)
                        {
                            all_relaciones[i].relation_number = temp_base + i + 1;
                        }
                        db_context.SaveChanges();

                        for (int i = 0; i < all_relaciones.Count; i++)
                        {
                            all_relaciones[i].relation_number = max_number - all_relaciones.Count + 1 + i;
                        }
                        db_context.SaveChanges();
                    }
                }
            }

            if (db_context.sellers.Any() || db_context.customers.Any() || db_context.products.Any())
            {
                return;
            }

            if (!db_context.sellers.Any())
            {
                db_context.sellers.AddRange(
                    new seller("Sandra", "001", "001"),
                    new seller("Anais", "002", "002"),
                    new seller("Alejandra", "003", "003"),
                    new seller("Juan Luis", "004", "004")
                );
                db_context.SaveChanges();
            }
            else
            {
                var juan = db_context.sellers.FirstOrDefault(s => s.full_name == "Juan Luis");
                if (juan == null)
                {
                    db_context.sellers.Add(new seller("Juan Luis", "004", "004"));
                    db_context.SaveChanges();
                }
            }

            if (!db_context.customers.Any())
            {
                db_context.customers.AddRange(
                    new customer("C-001", "Carlos Perez", "", "", "0414-1234567", "Valencia", "", ""),
                    new customer("C-002", "Maria Gomez", "", "", "0412-7654321", "Naguanagua", "", "")
                );
                db_context.SaveChanges();
            }

            if (!db_context.products.Any())
            {
                product product_1 = new product("OLE30300", "OLEO'S AMPOLLA ANTICAIDA 24 UNDS. OLEOS", "Oleos", 1.11m, 0);
                product product_2 = new product("OLE30302", "OLEO'S AMPOLLA ANTI-FRIZZ OLEOS", "Oleos", 1.11m, 0);
                product product_3 = new product("OLE30303", "OLEO'S AMPOLLA ALISADORA OLEOS", "Oleos", 1.11m, 0);
                product product_4 = new product("OLE30304", "OLEO'S AMPOLLA C.DE SABILA/ACEITE OLIVA OLEOS", "Oleos", 1.11m, 0);
                product product_5 = new product("OLE30318", "OLEO'S CUBRE CANAS HIDRATANTE", "Oleos", 1.11m, 0);
                product product_6 = new product("REM30401", "AMPOLLA ANTI CAIDA REMBRANT", "Rembrandt", 1.21m, 0);
                product product_7 = new product("REM30402", "AMPOLLA GOTAS DE SEDA REMBRANT", "Rembrandt", 1.21m, 0);
                product product_8 = new product("REM30403", "AMPOLLA SEMILINO REMBRANT", "Rembrandt", 1.21m, 0);
                product product_9 = new product("REM30404", "AMPOLLA PHYTO KERATINA REMBRANT", "Rembrandt", 1.21m, 0);
                product product_10 = new product("REM30405", "AMPOLLA PLACENTA DE OVEJO REMBRANT", "Rembrandt", 1.21m, 0);
                product product_11 = new product("REM30406", "AMPOLLA ANTICAIDA REMBRANT", "Rembrandt", 1.21m, 0);
                product product_12 = new product("REM30407", "AMPOLLA SEMILINO REMBRANT", "Rembrandt", 1.21m, 0);
                product product_13 = new product("DEF30004", "AMPOLLA K-BOTROX HIDRATANTE", "Defile", 1.39m, 0);
                product product_14 = new product("DEF30005", "AMPOLLA K-BOTROX ACONDICIONADOR", "Defile", 1.39m, 0);
                product product_15 = new product("DEF30007", "AMPOLLA REGULADOR (CABELLOS GRASOS)", "Defile", 1.39m, 0);
                product product_16 = new product("DEF30008", "AMPOLLA ACEITE DE ARGAN ACONDICIONADOR", "Defile", 1.39m, 0);
                product product_17 = new product("DEF30009", "AMPOLLA ACEITE DE ARGAN SUAVIDAD", "Defile", 1.39m, 0);
                product product_18 = new product("DEF30012", "AMPOLLA ANTICAIDA (FORTALECE LA RAIZ)", "Defile", 1.39m, 0);
                product product_19 = new product("DEF30013", "AMPOLLA KERATINA (Ideal para el cabello fino y fragil)", "Defile", 1.39m, 0);
                product product_20 = new product("DEF30014", "AMPOLLA SILICON Y SEDA", "Defile", 1.39m, 0);
                product product_21 = new product("DEF30015", "AMPOLLA ANTICASPA", "Defile", 1.39m, 0);
                product product_22 = new product("DEF30017", "AMPOLLA KERATINA SHOCK", "Defile", 1.39m, 0);
                product product_23 = new product("DEF30018", "AMPOLLA SEMILINO", "Defile", 1.39m, 0);
                product product_24 = new product("DEF30019", "AMPOLLA PLACENTA DE OVEJO", "Defile", 1.39m, 0);
                product product_25 = new product("DEF30020", "AMPOLLA CRISTAL DE SAVILA", "Defile", 1.39m, 0);
                product product_26 = new product("DEF30022", "AMPOLLA MEZCLA TINTE", "Defile", 1.39m, 0);
                product product_27 = new product("DEF30023", "AMPOLLA LISO Y BRILLO", "Defile", 1.39m, 0);
                product product_28 = new product("DEF30024", "AMPOLLA UVA THERAPY", "Defile", 1.39m, 0);
                product product_29 = new product("DEF30025", "AMPOLLA CUBRE CANAS", "Defile", 1.39m, 0);
                product product_30 = new product("DEF30026", "AMPOLLA ACEITE MACADAMIA NUTRI (NUTRE)", "Defile", 1.39m, 0);
                product product_31 = new product("DEF30027", "AMPOLLA ACEITE MACADAMIA HIDRATACION", "Defile", 1.39m, 0);
                product product_32 = new product("DEF30029", "AMPOLLA LECHE DE ALMENDRA", "Defile", 1.39m, 0);
                product product_33 = new product("DEF30002", "AMPOLLA MATIZADORA (tipo Embudo)", "Defile", 1.69m, 0);
                product product_34 = new product("DEF30010", "AMPOLLA BIOTINA (FORTALECE LA FIBRAS CAPILARES)", "Defile", 1.69m, 0);
                product product_35 = new product("DEF30011", "AMPOLLA TRICOMPLEX (Ultra acondicionador y brillo)", "Defile", 1.69m, 0);
                product product_36 = new product("DEF30016", "AMPOLLA KERATINA PLANCHADO EXPRESS", "Defile", 1.69m, 0);
                product product_37 = new product("DEF30021", "AMPOLLA SBLOCK 27", "Defile", 1.69m, 0);
                product product_38 = new product("DEF30028", "AMPOLLA ISOSFOLIEX HAIR SPA", "Defile", 1.69m, 0);
                product product_39 = new product("OLE30301", "OLEO'S AMPOLLA COMPLEX (Hidratacion intensiva)", "Oleos", 1.23m, 0);
                product product_40 = new product("DEF30003", "AMPOLLA TRICOMPLEX CON ACIDO HIALURONICO", "Defile", 1.84m, 0);
                product product_41 = new product("DEF30030", "AMPOLLA TRICOMPLEX MATIZADOR (tipo embudo)", "Defile", 2.45m, 0);
                product product_42 = new product("DEF30001", "AMPOLLA TRICOMPLEX MATIZADOR (tipo vial)", "Defile", 2.76m, 0);
                product product_43 = new product("DEF30006", "AMPOLLA K-BOTROX 3 (Ultra Hidratante D-Phantenol)", "Defile", 2.76m, 0);
                product product_44 = new product("DEF30100", "PRE-TRATAMIENTO TRICOMPLEX MATIZADOR", "Defile", 5.83m, 0);
                product product_45 = new product("DEF30101", "TRATAMIENTO INTENSIVO TRICOMPLEX MATIZADORA", "Defile", 5.60m, 0);
                product product_46 = new product("DEF30102", "PRE-TRATAMIENTO TRICOMPLEX CON VITAMINA E", "Defile", 5.83m, 0);
                product product_47 = new product("DEF30103", "TRATAMIENTO INTENSIVO TRICOMPLEX CON VITAMINA E", "Defile", 5.60m, 0);
                product product_48 = new product("DEF30104", "PRE-TRATAMIENTO TRICOMPLEX CON ACIDO HIALURONICO", "Defile", 5.83m, 0);
                product product_49 = new product("DEF30105", "TRATAMIENTO INTENSIVO TRICOMPLEX CON ACIDO HIALURONICO", "Defile", 5.60m, 0);
                product product_50 = new product("DEF30106", "PRE-TRATAMIENTO ACIDO HIALURONICO (BLANCO)", "Defile", 5.59m, 0);
                product product_51 = new product("DEF30107", "TRATAMIENTO INTENSIVO ACIDO HIALURONICO (BLANCO)", "Defile", 5.68m, 0);
                product product_52 = new product("DEF30108", "PRE-TRATAMIENTO K-BOTROX", "Defile", 5.60m, 0);
                product product_53 = new product("DEF30109", "TRATAMIENTO INTENSIVO K-BOTROX", "Defile", 5.45m, 0);
                product product_54 = new product("DEF30110", "PRE-TRATAMIENTO REGULADOR", "Defile", 5.60m, 0);
                product product_55 = new product("DEF30111", "TRATAMIENTO INTENSIVO REGULADOR", "Defile", 5.45m, 0);
                product product_56 = new product("DEF30112", "PRE-TRATAMIENTO ARGAN", "Defile", 5.83m, 0);
                product product_57 = new product("DEF30113", "TRATAMIENTO INTENSIVO ACEITE DE ARGAN", "Defile", 5.52m, 0);
                product product_58 = new product("DEF30114", "PRE-TRATAMIENTO BIOTINA DAMA", "Defile", 5.60m, 0);
                product product_59 = new product("DEF30115", "PRE-TRATAMIENTO BIOTINA CABALLERO", "Defile", 5.60m, 0);
                product product_60 = new product("DEF30116", "CHAMPU PROFESIONAL PH NEUTRO 2 Lt", "Defile", 8.85m, 0);
                product product_61 = new product("DEF30117", "PRE-TRATAMIENTO PH NEUTRO GALON", "Defile", 15.33m, 0);
                product product_62 = new product("DEF30118", "POST TRATAMIENTO PH NEUTRO GALON", "Defile", 15.33m, 0);
                product product_63 = new product("DEF30119", "SUERO CAPILAR K-BOTROX", "Defile", 4.60m, 0);
                product product_64 = new product("DEF30120", "ACEITE DE ARGAN CAPILAR", "Defile", 5.37m, 0);
                product product_65 = new product("DEF30121", "ACTIVADOR DE RIZOS", "Defile", 6.57m, 0);
                product product_66 = new product("DEF30122", "CREMA DESENREDANTE CON ACIDO HIALURONICO Y COLAGENO", "Defile", 6.57m, 0);
                product product_67 = new product("DEF30123", "CREMA ALISADORA SUAVE CON KERATINA", "Defile", 3.07m, 0);
                product product_68 = new product("DEF30124", "CREMA ALISADORA FUERTE CON KERATINA", "Defile", 5.33m, 0);
                product product_69 = new product("DEF30125", "POLVO DECOLORANTE DEFILE", "Defile", 17.71m, 0);
                product product_70 = new product("DEF30127", "CIRUGIA LISS EVOLUTION 911 KIT-DE 2", "Defile", 24.00m, 0);
                product product_71 = new product("DEF30128", "LISS EVOLUTION 911 SPRAY PROTEC TERMICO", "Defile", 6.63m, 0);
                product product_72 = new product("DEF30129", "TONICO CAPILAR ISOSFOLIEX", "Defile", 5.75m, 0);
                product product_73 = new product("DEF30130", "DESENGRASANTE MULTIUSO GALON", "Defile", 12.27m, 0);
                product product_74 = new product("DEF30131", "AGUA OXIGENADA VOL. 20", "Defile", 1.08m, 0);
                product product_75 = new product("DEF30132", "AGUA OXIGENADA VOL. 30", "Defile", 1.08m, 0);
                product product_76 = new product("DEF30135", "BALSAMO PROFESIONAL PH NEUTRO 2 Lt", "Defile", 8.85m, 0);
                product product_77 = new product("BIO30200", "AGUA MISCELAR", "Bioline", 5.15m, 0);
                product product_78 = new product("BIO30201", "LOCION DESMAQUILLANTE", "Bioline", 3.96m, 0);
                product product_79 = new product("BIO30202", "AGUA DE ROSAS", "Bioline", 5.15m, 0);
                product product_80 = new product("BIO30203", "LIMPIADOR FACIAL HIDRATANTE", "Bioline", 7.32m, 0);
                product product_81 = new product("BIO30204", "LIMPIADOR DE BROCHAS", "Bioline", 7.65m, 0);
                product product_82 = new product("BIO30205", "CREMA FACIAL REAFIRMANTE CON COLAGENO Y VIT. E", "Bioline", 5.15m, 0);
                product product_83 = new product("BIO30206", "CREMA FACIAL COLAGENO CON ANTIOXIDANTE", "Bioline", 5.15m, 0);
                product product_84 = new product("BIO30207", "CREMA FACIAL SKIN PERFECT NOCHE CON ALOE VERA Y RETINOL", "Bioline", 5.15m, 0);
                product product_85 = new product("BIO30208", "CREMA FACIAL ANTI ARRUGAS ACIDO HIALURONICO Y VIT. E", "Bioline", 5.15m, 0);
                product product_86 = new product("BIO30209", "SERUM ACIDO HIALURONICO Y COLAGENO", "Bioline", 6.08m, 0);
                product product_87 = new product("BIO30210", "SERUM COLAGENO", "Bioline", 6.08m, 0);
                product product_88 = new product("BIO30211", "SERUM NIACINAMIDA VITAMINA B3", "Bioline", 6.08m, 0);
                product product_89 = new product("BIO30212", "SERUM DE VITAMINA C", "Bioline", 6.08m, 0);
                product product_90 = new product("BIO30213", "BODY CREAM FRAMBUESA", "Bioline", 5.75m, 0);
                product product_91 = new product("BIO30214", "BODY CREAM ORQUIDEA", "Bioline", 5.75m, 0);
                product product_92 = new product("BIO30215", "BODY CREAM MANZANA MELON", "Bioline", 5.75m, 0);
                product product_93 = new product("BIO30216", "BODY CREAM ROSA", "Bioline", 5.75m, 0);
                product product_94 = new product("BIO30217", "BODY CREAM VAINILLA", "Bioline", 5.75m, 0);
                product product_95 = new product("BIO30223", "GEL ANTIBACTERIAL 70% ALCOHOL", "Bioline", 12.27m, 0);
                product product_96 = new product("BIO30224", "GEL ANTIBACTERIAL 70% ALCOHOL", "Bioline", 1.53m, 0);
                product product_97 = new product("BIO30225", "DESODORANTE ACLARANTE", "Bioline", 2.31m, 0);
                product product_98 = new product("BIO30226", "DESODORANTE UNISEX", "Bioline", 1.53m, 0);
                product product_99 = new product("OLE30305", "OLEO'S SHAMPOO CONTROL FRIZZ", "Oleos", 6.40m, 0);
                product product_100 = new product("OLE30306", "OLEO'S ACONDICIONADOR CONTROL FRIZZ", "Oleos", 6.40m, 0);
                product product_101 = new product("OLE30307", "OLEO'S SHAMPOO CONTROL CAIDA", "Oleos", 6.40m, 0);
                product product_102 = new product("OLE30308", "OLEO'S ACONDICIONADOR CONTROL CAIDA", "Oleos", 6.40m, 0);
                product product_103 = new product("OLE30309", "OLEO'S SHAMPOO RESTAURADOR", "Oleos", 6.40m, 0);
                product product_104 = new product("OLE30310", "OLEO'S ACONDICIONADOR RESTAURADOR", "Oleos", 6.40m, 0);
                product product_105 = new product("OLE30311", "OLEO'S SHAMPOO CONTROL CASPA", "Oleos", 6.40m, 0);
                product product_106 = new product("OLE30312", "OLEO'S ACONDICIONADOR CONTROL CASPA", "Oleos", 6.40m, 0);
                product product_107 = new product("OLE30313", "OLEO'S SHAMPOO CUIDADO DIARIO", "Oleos", 6.40m, 0);
                product product_108 = new product("OLE30314", "OLEO'S ACONDICIONADOR CUIDADO DIARIO", "Oleos", 6.40m, 0);
                product product_109 = new product("OLE30315", "OLEO'S SHAMPOO RIZOS DEFINIDOS", "Oleos", 6.40m, 0);
                product product_110 = new product("OLE30316", "OLEO'S ACONDICIONADOR RIZOS DEFINIDOS", "Oleos", 6.40m, 0);
                product product_111 = new product("OLE30317", "OLEO'S MASCARILLA HIDRATANTE + PROTEINAS", "Oleos", 6.40m, 0);
                product product_112 = new product("REM30420", "PRE-TRATAMIENTO PLACENTA OVEJO 1 LITRO", "Rembrandt", 5.33m, 0);
                product product_113 = new product("REM30421", "TRATAMIENTO INTENSIVO PLACENTA OVEJO 400 GR", "Rembrandt", 4.67m, 0);
                product product_114 = new product("REM30408", "Pre-Tratamiento Argan 360 ml REMBRANDT", "Rembrandt", 5.01m, 0);
                product product_115 = new product("REM30409", "Post-Tratamiento Aceite/Argan 360ml REMBRANDT", "Rembrandt", 5.11m, 0);
                product product_116 = new product("REM30410", "Tratamiento Intensivo Capilar Baño de Crema Aceite/Argan 240ml REMBRANDT", "Rembrandt", 4.93m, 0);
                product product_117 = new product("REM30411", "Crema Reafirmante con Colageno y Vitamina E 60 Grs. REMBRANDT", "Rembrandt", 5.13m, 0);
                product product_118 = new product("REM30412", "Agua Micelar 120 ML. REMBRANDT", "Rembrandt", 5.13m, 0);
                product product_119 = new product("REM30413", "Locion Desmaquillante 120 ML. REMBRANDT", "Rembrandt", 3.96m, 0);
                product product_120 = new product("REM30414", "Crema Corporal Hidratante 400 ML. REMBRANDT", "Rembrandt", 5.75m, 0);
                product product_121 = new product("REM30415", "Body Splah Frambuesa Desire 240 ML. REMBRANDT", "Rembrandt", 4.91m, 0);
                product product_122 = new product("REM30416", "Body Splah Vainilla Rocio 240 ML. REMBRANDT", "Rembrandt", 4.91m, 0);
                product product_123 = new product("REM30417", "AGUA DE ROSA REMBRANDT 120 ML", "Rembrandt", 5.13m, 0);
                product product_124 = new product("REM30418", "KID'S HAIR CLEAN CHAMPU NIÑOS Fragancia Manzanilla", "Rembrandt", 3.33m, 0);
                product product_125 = new product("REM30419", "PRE-TRATAMIENTO PLACENTA OVEJO 500 ML", "Rembrandt", 4.00m, 0);
                product product_126 = new product("AMA31001", "CHAMPU EXTRA NATURAL CEBOLLA MORADA", "Amazonia Secret", 2.67m, 0);
                product product_127 = new product("AMA31002", "TRATAMIENTO INTENSIVO DE CEBOLLA MORADA", "Amazonia Secret", 3.33m, 0);
                product product_128 = new product("AMA31003", "ACONDICIONADOR CEBOLLA MORADA", "Amazonia Secret", 3.33m, 0);
                product product_129 = new product("KED32001", "CHAMPU ANTICAIDA", "Kedam", 4.67m, 0);
                product product_130 = new product("KED32002", "CHAMPU HIDRATACION", "Kedam", 4.67m, 0);
                product product_131 = new product("KED32003", "CHAMPU 2 en 1", "Kedam", 4.67m, 0);
                product product_132 = new product("KED32004", "ACONDICIONADOR FLORES TROPICALES", "Kedam", 4.67m, 0);
                product product_133 = new product("KED32005", "CHAMPU CEBOLLA", "Kedam", 4.67m, 0);
                product product_134 = new product("KED32006", "CHAMPU PARA NIÑOS", "Kedam", 4.67m, 0);
                product product_135 = new product("KED32007", "CHAMPU ANTICASPA", "Kedam", 4.67m, 0);
                product product_136 = new product("KED32008", "CHAMPU FRESH CON LECHE DE COCO", "Kedam", 4.67m, 0);
                product product_137 = new product("DEP30501", "ACEITE POST DEPIL MANZANILLA", "Depil Clear", 3.33m, 0);
                product product_138 = new product("DEP30502", "ACEITE POST DEPIL ARGAN", "Depil Clear", 3.33m, 0);
                product product_139 = new product("DEP30503", "ACEITE POST DEPIL ALMENDRAS", "Depil Clear", 3.33m, 0);
                product product_140 = new product("DEP30504", "AMPOLLA POST DEPILACION", "Depil Clear", 1.20m, 0);
                product product_141 = new product("DEP30505", "DEPILIA TIRAS DEPILATORIAS", "Depil Clear", 4.67m, 0);
                product product_142 = new product("DEP30506", "DEPILIA ROLLO DE DEPILACION", "Depil Clear", 13.00m, 0);
                product product_143 = new product("DEP30508", "CERA LATA MANZANA VERDE (DEPIL CLEAR)", "Depil Clear", 13.00m, 0);
                product product_144 = new product("DEP30509", "CERA LATA MIEL (DEPIL CLEAR)", "Depil Clear", 13.00m, 0);
                product product_145 = new product("DEP30510", "CERA LATA BANANA (DEPIL CLEAR)", "Depil Clear", 13.00m, 0);
                product product_146 = new product("DEP30511", "CERA LATA TALCO (DEPIL CLEAR)", "Depil Clear", 13.00m, 0);
                product product_147 = new product("DEP30513", "CALENTADOR DE CERA DEPILWAX", "Depil Clear", 90.00m, 0);
                product product_148 = new product("DEP30514", "CALENTADOR DE CERA DEPILWAX", "Depil Clear", 104.00m, 0);
                product product_149 = new product("EST30601", "CAPA PARA TINTE PLASTICA DESCARTABLE X 30 PIEZAS", "Estilista", 13.33m, 0);
                product product_150 = new product("EST30602", "CAPA COLORES SURTIDO", "Estilista", 8.00m, 0);
                product product_151 = new product("EST30806", "PAÑUELO COSMETICO MULTIUSO 48 PIEZAS", "Estilista", 4.11m, 0);
                product product_152 = new product("EST30807", "PAÑUELO COSMETICO MULTIUSO 40 PIEZAS", "Estilista", 4.00m, 0);
                product product_153 = new product("EST30808", "GORRO BAÑO AZUL OSCURO", "Estilista", 2.00m, 0);
                product product_154 = new product("EST30609", "GORRO DE BAÑO AMARILLO", "Estilista", 2.00m, 0);
                product product_155 = new product("EST30610", "GORRO DE BAÑO VERDE", "Estilista", 2.00m, 0);
                product product_156 = new product("EST30613", "PEINE NARANJA GRANDE", "Estilista", 1.33m, 0);
                product product_157 = new product("EST30614", "PEINE NARANJA PEQUEÑO", "Estilista", 1.33m, 0);
                product product_158 = new product("EST30612", "PEINE NEGRO CON EMPAQUE", "Estilista", 1.33m, 0);
                product product_159 = new product("EST30615", "PORTA HILO DENTAL", "Estilista", 1.33m, 0);
                product product_160 = new product("EST30616", "PEINE MARRON", "Estilista", 1.33m, 0);
                product product_161 = new product("CUT32001", "MEN SHAMPOO CUTIQUE CONTROL DE CASPA 300 ML", "Cutique", 0.00m, 0);
                product product_162 = new product("CUT32002", "MEN SHAMPOO CUTIQUE CONTROL DE CAIDA 300 ML", "Cutique", 0.00m, 0);
                product product_163 = new product("CUT32003", "MEN 3 EN 1 CARA, CUERPO Y CABELLO 300 ML", "Cutique", 0.00m, 0);

                db_context.products.AddRange(product_1, product_2, product_3, product_4, product_5, product_6, product_7, product_8, product_9, product_10, product_11, product_12, product_13, product_14, product_15, product_16, product_17, product_18, product_19, product_20);
                db_context.products.AddRange(product_21, product_22, product_23, product_24, product_25, product_26, product_27, product_28, product_29, product_30, product_31, product_32, product_33, product_34, product_35, product_36, product_37, product_38, product_39, product_40);
                db_context.products.AddRange(product_41, product_42, product_43, product_44, product_45, product_46, product_47, product_48, product_49, product_50, product_51, product_52, product_53, product_54, product_55, product_56, product_57, product_58, product_59, product_60);
                db_context.products.AddRange(product_61, product_62, product_63, product_64, product_65, product_66, product_67, product_68, product_69, product_70, product_71, product_72, product_73, product_74, product_75, product_76, product_77, product_78, product_79, product_80);
                db_context.products.AddRange(product_81, product_82, product_83, product_84, product_85, product_86, product_87, product_88, product_89, product_90, product_91, product_92, product_93, product_94, product_95, product_96, product_97, product_98, product_99, product_100);
                db_context.products.AddRange(product_101, product_102, product_103, product_104, product_105, product_106, product_107, product_108, product_109, product_110, product_111, product_112, product_113, product_114, product_115, product_116, product_117, product_118, product_119, product_120);
                db_context.products.AddRange(product_121, product_122, product_123, product_124, product_125, product_126, product_127, product_128, product_129, product_130, product_131, product_132, product_133, product_134, product_135, product_136, product_137, product_138, product_139, product_140);
                db_context.products.AddRange(product_141, product_142, product_143, product_144, product_145, product_146, product_147, product_148, product_149, product_150, product_151, product_152, product_153, product_154, product_155, product_156, product_157, product_158, product_159, product_160);
                db_context.products.AddRange(product_161, product_162, product_163);

                db_context.SaveChanges();

                promotion promo_1 = new promotion("OF00001", "PROMO 2 X 1 LINEA BLANCA TRICOMPLEX CON ACIDO HIALURONICO - CHAMPO 2 X 1", "Defile", 5.90m);
                promotion promo_2 = new promotion("OF00002", "PROMO 2 X 1 CEBOLLA MORADA - AMAZONIA CHAMPO CEBOLLA MORADA 2 X 1", "Amazonia Secret", 5.90m);
                promotion promo_3 = new promotion("COM00001", "PROMO 2 X 1 TRICOMPLEX MATIZADOR - PRE-TRATAMIENTO MATIZADOR TRICOMPLEX CHAMPO Y BAÑO DE CREMA", "Defile", 5.90m);
                promotion promo_4 = new promotion("COM00002", "PROMO 2 X 1 REGULADOR - PRE-TRATAMIENTO REGULADOR DE GRASA CHAMPO Y BAÑO DE CREMA", "Defile", 5.90m);
                promotion promo_5 = new promotion("COM00003", "PROMO 2 X 1 ACIDO HIALURONICO - PRE-TRATAMIENTO ACIDO HIALURONICO CHAMPO Y BAÑO DE CREMA", "Defile", 5.90m);
                promotion promo_6 = new promotion("COM00004", "PROMO 2 X 1 ARGAN - PRE-TRATAMIENTO ARGAN CHAMPO Y BAÑO DE CREMA", "Defile", 5.90m);
                promotion promo_7 = new promotion("COM00005", "PROMO 2 X 1 TRICOMPLEX VITAMINA E - PRE-TRATAMIENTO TRICOMPLEX VITAMINA E CHAMPO Y BAÑO DE CREMA", "Defile", 5.90m);
                promotion promo_8 = new promotion("COM00006", "PROMO 2 X 1 K BOTROX - PRE-TRATAMIENTO K BOTROX CHAMPO Y ACONDICIONADOR", "Defile", 5.90m);
                promotion promo_9 = new promotion("OF00003", "CERA LATA MANZANA VERDE (DEPIL CLEAR)", "Depil Clear", 8.00m);
                promotion promo_10 = new promotion("OF00004", "CERA LATA MIEL BANANA (DEPIL CLEAR)", "Depil Clear", 8.00m);
                promotion promo_11 = new promotion("OF00005", "CERA LATA TALCO (DEPIL CLEAR)", "Depil Clear", 8.00m);
                promotion promo_12 = new promotion("COM00007", "PROMO 3 X 2 OLEOS - SHAMPOO Y ACONDICIONADOR CONTROL FRIZZ + CREMA DE OBSEQUIO", "Oleos", 12.80m);
                promotion promo_13 = new promotion("COM00008", "PROMO 3 X 2 OLEOS - SHAMPOO Y ACONDICIONADOR CONTROL CASPA + CREMA DE OBSEQUIO", "Oleos", 12.80m);
                promotion promo_14 = new promotion("COM00009", "PROMO 3 X 2 OLEOS - SHAMPOO Y ACONDICIONADOR CONTROL DE CAIDA + CREMA DE OBSEQUIO", "Oleos", 12.80m);
                promotion promo_15 = new promotion("COM00010", "PROMO 3 X 2 OLEOS - SHAMPOO Y ACONDICIONADOR RESTAURADOR + CREMA DE OBSEQUIO", "Oleos", 12.80m);
                promotion promo_16 = new promotion("COM00011", "PROMO 3 X 2 OLEOS - SHAMPOO Y ACONDICIONADOR CUIDADO DIARIO + CREMA DE OBSEQUIO", "Oleos", 12.80m);
                promotion promo_17 = new promotion("COM00012", "PROMO 3 X 2 OLEOS - SHAMPOO Y ACONDICIONADOR RIZOS + CREMA DE OBSEQUIO", "Oleos", 12.80m);
                promotion promo_18 = new promotion("OF00006", "OFERTA DEPICLEAR - ACEITE VARIADOS 240 ML", "Depil Clear", 2.50m);
                promotion promo_19 = new promotion("OF00007", "OFERTA KEDAM - SHAMPO KEDAM VARIO 360 ML", "Kedam", 2.50m);
                promotion promo_20 = new promotion("OF00008", "OFERTA POLVO - POLVO DECOLORANTE DEFILE 200GR", "Defile", 8.50m);

                db_context.promotions.AddRange(promo_1, promo_2, promo_3, promo_4, promo_5, promo_6, promo_7, promo_8, promo_9, promo_10, promo_11, promo_12, promo_13, promo_14, promo_15, promo_16, promo_17, promo_18, promo_19, promo_20);

                db_context.SaveChanges();

                var prod_p1 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30104");
                if (prod_p1 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p1.id_product, 2) { id_promotion = promo_1.id_promotion });
                }

                var prod_p2 = db_context.products.FirstOrDefault(p => p.product_code == "AMA31001");
                if (prod_p2 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p2.id_product, 2) { id_promotion = promo_2.id_promotion });
                }

                var prod_p3 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30100");
                var prod_p4 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30101");
                if (prod_p3 != null && prod_p4 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p3.id_product, 1) { id_promotion = promo_3.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p4.id_product, 1) { id_promotion = promo_3.id_promotion });
                }

                var prod_p5 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30110");
                var prod_p6 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30111");
                if (prod_p5 != null && prod_p6 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p5.id_product, 1) { id_promotion = promo_4.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p6.id_product, 1) { id_promotion = promo_4.id_promotion });
                }

                var prod_p7 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30106");
                var prod_p8 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30107");
                if (prod_p7 != null && prod_p8 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p7.id_product, 1) { id_promotion = promo_5.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p8.id_product, 1) { id_promotion = promo_5.id_promotion });
                }

                var prod_p9 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30112");
                var prod_p10 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30113");
                if (prod_p9 != null && prod_p10 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p9.id_product, 1) { id_promotion = promo_6.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p10.id_product, 1) { id_promotion = promo_6.id_promotion });
                }

                var prod_p11 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30102");
                var prod_p12 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30103");
                if (prod_p11 != null && prod_p12 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p11.id_product, 1) { id_promotion = promo_7.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p12.id_product, 1) { id_promotion = promo_7.id_promotion });
                }

                var prod_p13 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30108");
                var prod_p14 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30109");
                if (prod_p13 != null && prod_p14 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p13.id_product, 1) { id_promotion = promo_8.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p14.id_product, 1) { id_promotion = promo_8.id_promotion });
                }

                var prod_p15 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30508");
                if (prod_p15 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p15.id_product, 1) { id_promotion = promo_9.id_promotion });
                }

                var prod_p16 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30509");
                if (prod_p16 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p16.id_product, 1) { id_promotion = promo_10.id_promotion });
                }

                var prod_p17 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30511");
                if (prod_p17 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p17.id_product, 1) { id_promotion = promo_11.id_promotion });
                }

                var prod_p18 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30305");
                var prod_p19 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30306");
                var prod_p20 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30317");
                if (prod_p18 != null && prod_p19 != null && prod_p20 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p18.id_product, 1) { id_promotion = promo_12.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p19.id_product, 1) { id_promotion = promo_12.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p20.id_product, 1) { id_promotion = promo_12.id_promotion });
                }

                var prod_p21 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30311");
                var prod_p22 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30312");
                if (prod_p21 != null && prod_p22 != null && prod_p20 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p21.id_product, 1) { id_promotion = promo_13.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p22.id_product, 1) { id_promotion = promo_13.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p20.id_product, 1) { id_promotion = promo_13.id_promotion });
                }

                var prod_p23 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30307");
                var prod_p24 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30308");
                if (prod_p23 != null && prod_p24 != null && prod_p20 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p23.id_product, 1) { id_promotion = promo_14.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p24.id_product, 1) { id_promotion = promo_14.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p20.id_product, 1) { id_promotion = promo_14.id_promotion });
                }

                var prod_p25 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30309");
                var prod_p26 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30310");
                if (prod_p25 != null && prod_p26 != null && prod_p20 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p25.id_product, 1) { id_promotion = promo_15.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p26.id_product, 1) { id_promotion = promo_15.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p20.id_product, 1) { id_promotion = promo_15.id_promotion });
                }

                var prod_p27 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30313");
                var prod_p28 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30314");
                if (prod_p27 != null && prod_p28 != null && prod_p20 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p27.id_product, 1) { id_promotion = promo_16.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p28.id_product, 1) { id_promotion = promo_16.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p20.id_product, 1) { id_promotion = promo_16.id_promotion });
                }

                var prod_p29 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30315");
                var prod_p30 = db_context.products.FirstOrDefault(p => p.product_code == "OLE30316");
                if (prod_p29 != null && prod_p30 != null && prod_p20 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p29.id_product, 1) { id_promotion = promo_17.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p30.id_product, 1) { id_promotion = promo_17.id_promotion });
                    db_context.promotion_items.Add(new promotion_item(prod_p20.id_product, 1) { id_promotion = promo_17.id_promotion });
                }

                var prod_p31 = db_context.products.FirstOrDefault(p => p.product_code == "DEP30501");
                if (prod_p31 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p31.id_product, 1) { id_promotion = promo_18.id_promotion });
                }

                var prod_p32 = db_context.products.FirstOrDefault(p => p.product_code == "KED32001");
                if (prod_p32 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p32.id_product, 1) { id_promotion = promo_19.id_promotion });
                }

                var prod_p33 = db_context.products.FirstOrDefault(p => p.product_code == "DEF30125");
                if (prod_p33 != null)
                {
                    db_context.promotion_items.Add(new promotion_item(prod_p33.id_product, 1) { id_promotion = promo_20.id_promotion });
                }

                db_context.SaveChanges();
            }
        }

        // Migra series con prefijos viejos (customer_code_prefix) a los prefijos nuevos (seller_code).
        // Ej: Sandra 3301 -> 3200; Anais 3300 -> 3300 (sin cambio); Alejandra 3305 -> 3500.
        // Idempotente: si no hay prefijos viejos en la data, no cambia nada.
        // Clientes: siempre se alinean sufijos a 3 digitos. Notas: se renumeran solo si cambia el prefijo.
        private static void migrate_legacy_series(NinOSDbContext db_context)
        {
            var sellers = db_context.sellers.ToList();
            foreach (var seller in sellers)
            {
                if (string.IsNullOrWhiteSpace(seller.customer_code_prefix) || string.IsNullOrWhiteSpace(seller.seller_code)) continue;

                string legacy_prefix = seller.customer_code_prefix.Trim();
                string current_prefix = seller.seller_code.Trim();
                if (!long.TryParse(legacy_prefix, out _) || !long.TryParse(current_prefix, out _)) continue;

                bool prefix_changed = legacy_prefix != current_prefix;
                string legacy_marker = legacy_prefix + "_";
                string current_marker = current_prefix + "_";
                long seed = SeriesCalculator.Seed(current_prefix);

                // --- Clientes ---
                // Renumera en orden: clientes con prefijo viejo (3301_xx...) y clientes con prefijo
                // actual pero sufijo no alineado a 3 digitos (3200_5 / 3200_05 de generadores viejos).
                bool customer_changed = false;
                var all_customers = db_context.customers.ToList();
                var customers_to_fix = all_customers
                    .Where(c => c.customer_code.StartsWith(legacy_marker)
                        || (c.customer_code.StartsWith(current_marker) && has_non_3digit_suffix(c.customer_code)))
                    .OrderBy(c => c.id_customer)
                    .ToList();
                if (customers_to_fix.Count > 0)
                {
                    long used_max = all_customers
                        .Where(c => c.customer_code.StartsWith(current_marker) && !has_non_3digit_suffix(c.customer_code))
                        .Select(c => SeriesCalculator.ParseFullNumber(c.customer_code))
                        .Where(v => v > 0)
                        .DefaultIfEmpty(seed)
                        .Max();

                    long counter = Math.Max(used_max, seed);
                    foreach (var c in customers_to_fix)
                    {
                        counter++;
                        c.customer_code = SeriesCalculator.FormatNumber(counter);
                    }
                    db_context.SaveChanges();
                    customer_changed = true;
                }

                // --- Notas (solo cuando el prefijo realmente cambio) ---
                if (prefix_changed)
                {
                    // --- Notas de entrega ---
                    var legacy_delivery = db_context.delivery_notes
                        .Where(n => n.note_number.StartsWith(legacy_marker))
                        .OrderBy(n => n.id_delivery_note)
                        .ToList();
                    if (legacy_delivery.Count > 0)
                    {
                        long used_max = db_context.delivery_notes
                            .Where(n => n.note_number.StartsWith(current_marker))
                            .ToList()
                            .Select(n => SeriesCalculator.ParseFullNumber(n.note_number))
                            .Where(v => v > 0)
                            .DefaultIfEmpty(seed)
                            .Max();

                        long counter = Math.Max(used_max, seed);
                        foreach (var n in legacy_delivery)
                        {
                            counter++;
                            n.note_number = SeriesCalculator.FormatNumber(counter);
                        }
                        db_context.SaveChanges();
                        customer_changed = true;
                    }

                    // --- Notas de credito ---
                    var legacy_credit = db_context.credit_notes
                        .Where(n => n.note_number.StartsWith(legacy_marker))
                        .OrderBy(n => n.id_credit_note)
                        .ToList();
                    if (legacy_credit.Count > 0)
                    {
                        long used_max = db_context.credit_notes
                            .Where(n => n.note_number.StartsWith(current_marker))
                            .ToList()
                            .Select(n => SeriesCalculator.ParseFullNumber(n.note_number))
                            .Where(v => v > 0)
                            .DefaultIfEmpty(seed)
                            .Max();

                        long counter = Math.Max(used_max, seed);
                        foreach (var n in legacy_credit)
                        {
                            counter++;
                            n.note_number = SeriesCalculator.FormatNumber(counter);
                        }
                        db_context.SaveChanges();
                        customer_changed = true;
                    }
                }

                if (customer_changed)
                {
                    seller.last_customer_number = db_context.customers
                        .Where(c => c.customer_code.StartsWith(current_marker))
                        .ToList()
                        .Select(c => SeriesCalculator.ParseFullNumber(c.customer_code))
                        .Where(v => v > 0)
                        .DefaultIfEmpty(seed)
                        .Max();
                    db_context.SaveChanges();
                }
            }
        }

        private static DateTime monday_of(DateTime date)
        {
            // La fecha se convierte a hora de Venezuela antes de buscar el lunes. Sin esto, una
            // nota creada a medianoche se guardaba con el instante UTC equivalente al dia
            // anterior en Caracas y caia en la semana equivocada, lo que hacia que este
            // inicializador creara una relacion extra para una semana que no correspondia.
            DateTime local = DateTime.SpecifyKind(date, DateTimeKind.Utc) + AppTimeZone.Offset;
            int offset = ((int)local.DayOfWeek + 6) % 7;
            return local.Date.AddDays(-offset);
        }

        // True si el sufijo tras "_" no son exactamente 3 digitos (ej: "3200_5", "3200_05").
        private static bool has_non_3digit_suffix(string code)
        {
            int separator = code.LastIndexOf('_');
            if (separator <= 0 || separator >= code.Length - 1) return true;

            string suffix = code.Substring(separator + 1);
            return suffix.Length != 3 || !suffix.All(char.IsDigit);
        }

        private static void initialize_zonas_and_sellers(NinOSDbContext db_context)
        {
            // 1. Asegurar o actualizar las 6 zonas definitivas
            var defaultZoneData = new Dictionary<string, string>
            {
                { "01", "Zona 1" },
                { "02", "Zona 2" },
                { "03", "Zona 3" },
                { "04", "Zona 4" },
                { "05", "Zona 5" },
                { "06", "Maracay" }
            };

            foreach (var kvp in defaultZoneData)
            {
                var existingZona = db_context.zonas.FirstOrDefault(z => z.code == kvp.Key);
                if (existingZona == null)
                {
                    db_context.zonas.Add(new zona
                    {
                        code = kvp.Key,
                        name = kvp.Value,
                        sort_order = int.Parse(kvp.Key),
                        is_active = true,
                        is_pro_venta = kvp.Key == "06"
                    });
                }
                else
                {
                    // Si estaba eliminada (ej: Maracay), restaurarla
                    if (!existingZona.is_active)
                    {
                        existingZona.is_active = true;
                        existingZona.deleted_at = null;
                        existingZona.deleted_reason = null;
                    }
                    if (existingZona.code == "06" && !existingZona.is_pro_venta)
                    {
                        existingZona.is_pro_venta = true;
                    }
                    // Actualizar nombre si tenía el nombre anterior
                    if (existingZona.name != kvp.Value && 
                        (existingZona.name == "Isabelica" || existingZona.name == "San Diego" || 
                         existingZona.name == "Tocuyito" || existingZona.name == "Centro" || 
                         existingZona.name == "Flor Amarillo" || existingZona.code == "06"))
                    {
                        existingZona.name = kvp.Value;
                    }
                }
            }
            db_context.SaveChanges();

            // 2. Backfill clientes sin zona a Zona 01
            var zona01 = db_context.zonas.FirstOrDefault(z => z.code == "01");
            if (zona01 != null)
            {
                var customersWithoutZona = db_context.customers.Where(c => c.id_zona == null).ToList();
                if (customersWithoutZona.Count > 0)
                {
                    foreach (var c in customersWithoutZona)
                    {
                        c.id_zona = zona01.id_zona;
                    }
                    db_context.SaveChanges();
                }
            }

            // 3. Asegurar vendedores base si no existen
            if (!db_context.sellers.Any())
            {
                db_context.sellers.AddRange(
                    new seller("Sandra", "001", "001"),
                    new seller("Anais", "002", "002"),
                    new seller("Alejandra", "003", "003"),
                    new seller("Juan Luis", "004", "004")
                );
                db_context.SaveChanges();
            }
            else
            {
                var juan = db_context.sellers.FirstOrDefault(s => s.seller_code == "004" || s.seller_code == "3400" || s.full_name == "Juan Luis");
                if (juan == null)
                {
                    db_context.sellers.Add(new seller("Juan Luis", "004", "004"));
                    db_context.SaveChanges();
                }
            }

            // 4. Asignar Maracay (06) a Juan Luis (004)
            var juanLuis = db_context.sellers.FirstOrDefault(s => s.seller_code == "004" || s.seller_code == "3400" || s.full_name == "Juan Luis");
            var maracayZona = db_context.zonas.FirstOrDefault(z => z.code == "06");
            if (juanLuis != null && maracayZona != null)
            {
                if (!db_context.seller_zones.Any(sz => sz.id_seller == juanLuis.id_seller && sz.id_zona == maracayZona.id_zona))
                {
                    db_context.seller_zones.Add(new seller_zone
                    {
                        id_seller = juanLuis.id_seller,
                        id_zona = maracayZona.id_zona
                    });
                    db_context.SaveChanges();
                }
            }
        }

        private static void initialize_product_lines(NinOSDbContext db_context)
        {
            var defaultLines = new (string name, string prefix, int sortOrder)[]
            {
                ("DEFILE", "DEF", 1),
                ("OLEOS", "OLE", 2),
                ("REMBRANDT", "REM", 3),
                ("BIOLINE", "BIO", 4),
                ("AMAZONIA SECRET", "AMA", 5),
                ("KEDAM", "KED", 6),
                ("DEPIL CLEAR", "DEP", 7),
                ("ESTILISTA", "EST", 8),
                ("CUTIQUE", "CUT", 9),
                ("OTROS", "OTR", 10)
            };

            bool changed = false;
            foreach (var item in defaultLines)
            {
                var existing = db_context.product_lines.FirstOrDefault(l => l.name == item.name || l.code_prefix == item.prefix);
                if (existing == null)
                {
                    db_context.product_lines.Add(new product_line
                    {
                        name = item.name,
                        code_prefix = item.prefix,
                        sort_order = item.sortOrder,
                        is_active = true
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                db_context.SaveChanges();
            }

            // Registrar todos los prefijos activos en product_code_rules para acceso rápido
            var allLines = db_context.product_lines.Where(l => l.is_active).ToList();
            foreach (var line in allLines)
            {
                product_code_rules.RegisterPrefix(line.name, line.code_prefix);
            }
        }

        private static void migrate_customer_correlatives_to_global5(NinOSDbContext db_context)
        {
            var customers = db_context.customers
                .OrderBy(c => c.id_customer)
                .ToList();

            if (customers.Count == 0) return;

            bool needsMigration = customers.Any(c => string.IsNullOrWhiteSpace(c.customer_code) ||
                                                     c.customer_code.Contains("_") ||
                                                     c.customer_code.Length != 5 ||
                                                     !c.customer_code.All(char.IsDigit));

            if (!needsMigration) return;

            long counter = 1;
            foreach (var c in customers)
            {
                c.customer_code = counter.ToString("D5");
                counter++;
            }
            db_context.SaveChanges();
        }

        private static void cleanup_credit_note_payments_and_adjust_totals(NinOSDbContext db_context)
        {
            // Las notas de crédito tipo Devolución no son abonos de pago: descuentan directamente
            // el monto de la nota de entrega original (total_amount_usd y adjusted_total_usd).
            // Si existen registros históricos en la tabla payments con payment_type == "NOTA DE CREDITO",
            // se descuenta el monto de las notas de entrega correspondientes y se eliminan dichos pagos.
            var nc_payments = db_context.payments
                .Where(p => p.payment_type == "NOTA DE CREDITO")
                .ToList();

            if (nc_payments.Count == 0) return;

            var note_ids = nc_payments
                .Where(p => p.id_delivery_note != null)
                .Select(p => p.id_delivery_note!.Value)
                .Distinct()
                .ToList();

            var notes = db_context.delivery_notes
                .Where(n => note_ids.Contains(n.id_delivery_note))
                .ToList();

            foreach (var note in notes)
            {
                var payments_for_note = nc_payments.Where(p => p.id_delivery_note == note.id_delivery_note).ToList();
                decimal total_nc_amount = payments_for_note.Sum(p => p.amount_usd);

                note.total_amount_usd = Math.Max(0, note.total_amount_usd - total_nc_amount);
                note.adjusted_total_usd = Math.Max(0, note.adjusted_total_usd - total_nc_amount);

                // Recalcular status de la nota considerando solo pagos reales
                var real_payments_sum = db_context.payments
                    .Where(p => p.id_delivery_note == note.id_delivery_note && p.payment_type != "NOTA DE CREDITO")
                    .Sum(p => (decimal?)p.amount_usd) ?? 0m;

                if (note.status != "Anulada")
                {
                    if (note.adjusted_total_usd == 0)
                    {
                        note.status = "Devuelta";
                    }
                    else if (real_payments_sum >= note.adjusted_total_usd)
                    {
                        note.status = "Pagada";
                    }
                    else
                    {
                        note.status = "Pendiente";
                    }
                }

                if (note.status == "Pendiente")
                {
                    var pending_commission = db_context.commissions
                        .FirstOrDefault(c => c.id_delivery_note == note.id_delivery_note && !c.is_paid);
                    if (pending_commission != null)
                    {
                        db_context.commissions.Remove(pending_commission);
                    }
                }
                else if (note.status == "Pagada")
                {
                    var pending_commission = db_context.commissions
                        .FirstOrDefault(c => c.id_delivery_note == note.id_delivery_note && !c.is_paid);
                    if (pending_commission != null)
                    {
                        decimal new_commissionable = Math.Min(real_payments_sum, note.adjusted_total_usd);
                        pending_commission.generated_amount_usd = Math.Round(new_commissionable * 0.10m, 2);
                    }
                }
            }

            db_context.payments.RemoveRange(nc_payments);
            db_context.SaveChanges();
        }
    }
}