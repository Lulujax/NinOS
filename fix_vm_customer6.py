path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add methods before recalculate_total or somewhere
old = '''        private void load_initial_data_async()
        {
            refresh_data();
        }'''

new = '''        private void execute_select_customer(object? parameter)
        {
            if (parameter is customer c)
            {
                selected_customer = c;
                _customer_search_text = c.business_name ?? string.Empty;
                on_property_changed(nameof(customer_search_text));
                show_customer_popup = false;
            }
        }

        private void execute_toggle_customer_popup(object? parameter)
        {
            _customer_filter_debounce.Stop();
            _customer_search_text = string.Empty;
            on_property_changed(nameof(customer_search_text));
            update_customer_display();
            show_customer_popup = filtered_customers_display.Count > 0 && !show_customer_popup ? true : !show_customer_popup;
            if (filtered_customers_display.Count == 0) show_customer_popup = false;
        }

        private void update_customer_display()
        {
            filtered_customers_display.Clear();
            string search = _customer_search_text ?? string.Empty;
            foreach (var c in filtered_customers)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    filtered_customers_display.Add(c);
                    continue;
                }
                if (c.business_name != null && c.business_name.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    filtered_customers_display.Add(c);
                }
            }
        }

        private void update_customer_popup()
        {
            bool has = filtered_customers_display.Count > 0;
            bool filled = !string.IsNullOrWhiteSpace(_customer_search_text);
            show_customer_popup = filled && has;
        }

        private void load_initial_data_async()
        {
            refresh_data();
        }'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
