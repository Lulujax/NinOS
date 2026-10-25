path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''        private bool _is_reverting_selection;
        private bool _is_updating_note_types;'''

new = '''        private bool _is_reverting_selection;
        private bool _is_updating_note_types;
        private string _customer_search_text = string.Empty;
        private bool _show_customer_popup;
        private System.Windows.Threading.DispatcherTimer _customer_filter_debounce;'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
