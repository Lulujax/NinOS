path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''        public Action? OnNoteSaved;
        public Action? RequestRevertSellerSelection;
        public Action? RequestRevertNoteTypeSelection;
        private bool _is_reverting_selection;'''

new = '''        public Action? OnNoteSaved;
        public Action? RequestRevertSellerSelection;
        public Action? RequestRevertNoteTypeSelection;
        private bool _is_reverting_selection;
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
