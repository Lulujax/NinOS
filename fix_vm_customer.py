path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add using System.Windows.Threading if not present (already has it)
# Add properties and fields
old = '''        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<note_type> note_type_options { get; }
        public ObservableCollection<customer> filtered_customers { get; }
        public ObservableCollection<billable_item> all_items { get; }
        public ObservableCollection<note_detail_row> note_details { get; }

        public ICommand add_item_command { get; }
        public ICommand remove_item_command { get; }
        public ICommand save_note_command { get; }'''

new = '''        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<note_type> note_type_options { get; }
        public ObservableCollection<customer> filtered_customers { get; }
        public ObservableCollection<customer> filtered_customers_display { get; }
        public ObservableCollection<billable_item> all_items { get; }
        public ObservableCollection<note_detail_row> note_details { get; }

        public ICommand add_item_command { get; }
        public ICommand remove_item_command { get; }
        public ICommand save_note_command { get; }
        public ICommand select_customer_command { get; }
        public ICommand toggle_customer_popup_command { get; }'''

if old not in content:
    print('NOT FOUND 1')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done1')
