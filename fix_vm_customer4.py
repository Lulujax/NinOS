path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''            _all_customers_cache = new List<customer>();
            _all_note_types_cache = new List<note_type>();
            sellers = new ObservableCollection<seller>();
            note_type_options = new ObservableCollection<note_type>();
            filtered_customers = new ObservableCollection<customer>();
            all_items = new ObservableCollection<billable_item>();
            note_details = new ObservableCollection<note_detail_row>();

            add_item_command = new RelayCommand(execute_add_item);
            remove_item_command = new RelayCommand(execute_remove_item);
            save_note_command = new RelayCommand(execute_save_note);'''

new = '''            _all_customers_cache = new List<customer>();
            _all_note_types_cache = new List<note_type>();
            sellers = new ObservableCollection<seller>();
            note_type_options = new ObservableCollection<note_type>();
            filtered_customers = new ObservableCollection<customer>();
            filtered_customers_display = new ObservableCollection<customer>();
            all_items = new ObservableCollection<billable_item>();
            note_details = new ObservableCollection<note_detail_row>();

            add_item_command = new RelayCommand(execute_add_item);
            remove_item_command = new RelayCommand(execute_remove_item);
            save_note_command = new RelayCommand(execute_save_note);
            select_customer_command = new RelayCommand(execute_select_customer);
            toggle_customer_popup_command = new RelayCommand(execute_toggle_customer_popup);

            _customer_filter_debounce = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _customer_filter_debounce.Tick += (s, e) =>
            {
                _customer_filter_debounce.Stop();
                update_customer_display();
                update_customer_popup();
            };'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
