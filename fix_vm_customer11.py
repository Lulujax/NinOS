path = 'src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''                if (keepSelection && previousSelection != null && note_type_options.Any(nt => nt.code == previousSelection.code))
                {
                    _selected_note_type = note_type_options.First(nt => nt.code == previousSelection.code);
                }
                else
                {
                    _selected_note_type = null;
                }

                ApplyNoteType(_selected_note_type);'''

new = '''                if (keepSelection && previousSelection != null && note_type_options.Any(nt => nt.code == previousSelection.code))
                {
                    _selected_note_type = note_type_options.First(nt => nt.code == previousSelection.code);
                }
                else
                {
                    _selected_note_type = null;
                }

                ApplyNoteType(_selected_note_type);'''

# Also need to ensure filter_customers is called
if old not in content:
    print('NOT FOUND')
    raise SystemExit
# Just in case, add filter call if not present
if 'filter_customers();' not in content[content.find('RefreshNoteTypeOptions'):content.find('RefreshNoteTypeOptions')+500] or True:
    pass
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('ok')
