path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''            if (_selected_customer != null && !filtered_customers.Any(c => c.id_customer == _selected_customer.id_customer))
            {
                _selected_customer = null;
                customer_code_text = string.Empty;
                contact_name_text = string.Empty;
                on_property_changed(nameof(selected_customer));
                on_property_changed(nameof(customer_code_text));
                on_property_changed(nameof(contact_name_text));
            }
        }

        public customer? selected_customer
        {
            get { return _selected_customer; }
            set
            {
                if (_selected_customer == value) return;
                _selected_customer = value;
                if (_selected_customer != null)
                {
                    customer_code_text = _selected_customer.customer_code;
                    contact_name_text = _selected_customer.contact_name;
                }
                else
                {
                    customer_code_text = string.Empty;
                    contact_name_text = string.Empty;
                }
                on_property_changed();
                on_property_changed(nameof(customer_code_text));
                on_property_changed(nameof(contact_name_text));
                on_property_changed(nameof(has_pending_data));
            }
        }'''

new = '''            if (_selected_customer != null && !filtered_customers.Any(c => c.id_customer == _selected_customer.id_customer))
            {
                _selected_customer = null;
                customer_code_text = string.Empty;
                contact_name_text = string.Empty;
                _customer_search_text = string.Empty;
                on_property_changed(nameof(selected_customer));
                on_property_changed(nameof(customer_code_text));
                on_property_changed(nameof(contact_name_text));
                on_property_changed(nameof(customer_search_text));
                show_customer_popup = false;
            }
            update_customer_display();
        }

        public customer? selected_customer
        {
            get { return _selected_customer; }
            set
            {
                if (_selected_customer == value) return;
                _selected_customer = value;
                if (_selected_customer != null)
                {
                    customer_code_text = _selected_customer.customer_code;
                    contact_name_text = _selected_customer.contact_name;
                    _customer_search_text = _selected_customer.business_name ?? string.Empty;
                }
                else
                {
                    customer_code_text = string.Empty;
                    contact_name_text = string.Empty;
                    _customer_search_text = string.Empty;
                }
                on_property_changed();
                on_property_changed(nameof(customer_code_text));
                on_property_changed(nameof(contact_name_text));
                on_property_changed(nameof(customer_search_text));
                on_property_changed(nameof(has_pending_data));
                update_customer_display();
                show_customer_popup = false;
            }
        }'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
