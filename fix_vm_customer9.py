path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''            filtered_customers.Clear();
            _selected_customer = null;
            customer_code_text = string.Empty;
            contact_name_text = string.Empty;
            note_number = string.Empty;

            on_property_changed(nameof(selected_note_type));
            on_property_changed(nameof(selected_customer));
            on_property_changed(nameof(customer_code_text));
            on_property_changed(nameof(contact_name_text));
            on_property_changed(nameof(note_number));
            on_property_changed(nameof(has_selection));'''

new = '''            filtered_customers.Clear();
            filtered_customers_display.Clear();
            _selected_customer = null;
            customer_code_text = string.Empty;
            contact_name_text = string.Empty;
            _customer_search_text = string.Empty;
            show_customer_popup = false;
            note_number = string.Empty;

            on_property_changed(nameof(selected_note_type));
            on_property_changed(nameof(selected_customer));
            on_property_changed(nameof(customer_code_text));
            on_property_changed(nameof(contact_name_text));
            on_property_changed(nameof(customer_search_text));
            on_property_changed(nameof(note_number));
            on_property_changed(nameof(has_selection));'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
