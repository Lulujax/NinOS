path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''            filtered_customers.Clear();

            if (_selected_seller != null && _selected_note_type != null)
            {
                var assignedZoneIds = _seller_assigned_zones.Select(z => z.id_zona).ToHashSet();
                bool isPv = NoteTypeCodes.is_pro_venta(_selected_note_type.code);

                var match = _all_customers_cache
                    .Where(c => c.id_zona.HasValue && assignedZoneIds.Contains(c.id_zona.Value))
                    .Where(c =>
                    {
                        int idZona = c.id_zona!.Value;
                        var custZona = c.zona ?? _seller_assigned_zones.FirstOrDefault(z => z.id_zona == idZona);
                        return isPv ? IsProVentaZona(custZona) : !IsProVentaZona(custZona);
                    })
                    .OrderBy(c => c.business_name);

                foreach (customer c in match)
                {
                    filtered_customers.Add(c);
                }
            }'''

new = '''            filtered_customers.Clear();

            if (_selected_seller != null && _selected_note_type != null)
            {
                var assignedZoneIds = _seller_assigned_zones.Select(z => z.id_zona).ToHashSet();
                bool isPv = NoteTypeCodes.is_pro_venta(_selected_note_type.code);

                var match = _all_customers_cache
                    .Where(c => c.id_zona.HasValue && assignedZoneIds.Contains(c.id_zona.Value))
                    .Where(c =>
                    {
                        int idZona = c.id_zona!.Value;
                        var custZona = c.zona ?? _seller_assigned_zones.FirstOrDefault(z => z.id_zona == idZona);
                        return isPv ? IsProVentaZona(custZona) : !IsProVentaZona(custZona);
                    })
                    .OrderBy(c => c.business_name);

                foreach (customer c in match)
                {
                    filtered_customers.Add(c);
                }
            }
            update_customer_display();'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
