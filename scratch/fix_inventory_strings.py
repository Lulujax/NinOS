with open('src/NinOS.UI/Common/ViewModels/InventoryViewModel.cs', 'r', encoding='utf-8') as f:
    c = f.read()

bad1 = '''                AppDialog.Show($"No se pudo generar la lista de precios en PDF:

{ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);'''

good1 = '''                AppDialog.Show("No se pudo generar la lista de precios en PDF: " + ErrorText.Get(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);'''

bad2 = '''                AppDialog.Show($"No se pudo exportar la lista de precios a Excel:

{ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);'''

good2 = '''                AppDialog.Show("No se pudo exportar la lista de precios a Excel: " + ErrorText.Get(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);'''

c = c.replace('\r\n', '\n')
bad1 = bad1.replace('\r\n', '\n')
bad2 = bad2.replace('\r\n', '\n')

assert bad1 in c, 'bad1 not found'
assert bad2 in c, 'bad2 not found'

c = c.replace(bad1, good1, 1)
c = c.replace(bad2, good2, 1)

with open('src/NinOS.UI/Common/ViewModels/InventoryViewModel.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(c)

print('InventoryViewModel.cs string literals fixed!')

