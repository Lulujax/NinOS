BEGIN;

INSERT INTO note_type (name, code, id_seller, header_title, calculation_type, discount_configurable, default_discount_percentage, promo_discount_percentage, mandatory_discount_percentage, conditions_template, discount_conditions_template, is_active, sort_order, es_volumen)
SELECT 'Vesupronta', 'VES', src.id_seller, src.header_title, src.calculation_type, src.discount_configurable, src.default_discount_percentage, src.promo_discount_percentage, src.mandatory_discount_percentage, src.conditions_template, src.discount_conditions_template, src.is_active, 9, src.es_volumen
FROM note_type src
WHERE src.code = 'GEN'
  AND NOT EXISTS (SELECT 1 FROM note_type existing WHERE existing.code = 'VES');

INSERT INTO note_type (name, code, id_seller, header_title, calculation_type, discount_configurable, default_discount_percentage, promo_discount_percentage, mandatory_discount_percentage, conditions_template, discount_conditions_template, is_active, sort_order, es_volumen)
SELECT 'Promocion Vesupronta', 'VESPRM', src.id_seller, src.header_title, src.calculation_type, src.discount_configurable, src.default_discount_percentage, src.promo_discount_percentage, src.mandatory_discount_percentage, src.conditions_template, src.discount_conditions_template, src.is_active, 10, src.es_volumen
FROM note_type src
WHERE src.code = 'PRM'
  AND NOT EXISTS (SELECT 1 FROM note_type existing WHERE existing.code = 'VESPRM');

INSERT INTO note_type (name, code, id_seller, header_title, calculation_type, discount_configurable, default_discount_percentage, promo_discount_percentage, mandatory_discount_percentage, conditions_template, discount_conditions_template, is_active, sort_order, es_volumen)
SELECT 'Volumen Vesupronta', 'VESVOL', src.id_seller, src.header_title, src.calculation_type, src.discount_configurable, src.default_discount_percentage, src.promo_discount_percentage, src.mandatory_discount_percentage, src.conditions_template, src.discount_conditions_template, src.is_active, 11, true
FROM note_type src
WHERE src.code = 'VOL'
  AND NOT EXISTS (SELECT 1 FROM note_type existing WHERE existing.code = 'VESVOL');

SELECT id_note_type, name, code, calculation_type, default_discount_percentage, es_volumen, sort_order
FROM note_type
ORDER BY sort_order;

COMMIT;
