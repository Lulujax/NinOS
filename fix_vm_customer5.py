path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''        public string note_number
        {
            get { return _note_number; }
            private set
            {
                if (_note_number == value) return;
                _note_number = value;
                on_property_changed();
            }
        }'''

new = '''        public string customer_search_text
        {
            get { return _customer_search_text; }
            set
            {
                if (_customer_search_text == value) return;
                _customer_search_text = value;
                on_property_changed();
                _customer_filter_debounce.Stop();
                _customer_filter_debounce.Start();
            }
        }

        public bool show_customer_popup
        {
            get { return _show_customer_popup; }
            set
            {
                if (_show_customer_popup == value) return;
                _show_customer_popup = value;
                on_property_changed();
            }
        }

        public string note_number
        {
            get { return _note_number; }
            private set
            {
                if (_note_number == value) return;
                _note_number = value;
                on_property_changed();
            }
        }'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
