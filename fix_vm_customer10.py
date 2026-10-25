path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''                else
                {
                    filtered_customers.Clear();
                    note_number = string.Empty;
                }'''

new = '''                else
                {
                    filtered_customers.Clear();
                    filtered_customers_display.Clear();
                    _selected_customer = null;
                    customer_code_text = string.Empty;
                    contact_name_text = string.Empty;
                    _customer_search_text = string.Empty;
                    show_customer_popup = false;
                    on_property_changed(nameof(selected_customer));
                    on_property_changed(nameof(customer_code_text));
                    on_property_changed(nameof(contact_name_text));
                    on_property_changed(nameof(customer_search_text));
                    note_number = string.Empty;
                }'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
