import sys

# 1. Patch InventoryViewModel.cs
with open('src/NinOS.UI/Common/ViewModels/InventoryViewModel.cs', 'r', encoding='utf-8') as f:
    vm_content = f.read()

# Add public ICommand generate_price_list_excel_command
target_cmd = 'public ICommand generate_price_list_command { get; }'
repl_cmd = 'public ICommand generate_price_list_command { get; }\n        public ICommand generate_price_list_excel_command { get; }'
assert target_cmd in vm_content, 'target_cmd not found'
vm_content = vm_content.replace(target_cmd, repl_cmd, 1)

# Add generate_price_list_excel_command in ctor
target_ctor = 'generate_price_list_command = new RelayCommand(execute_generate_price_list);'
repl_ctor = 'generate_price_list_command = new RelayCommand(execute_generate_price_list);\n            generate_price_list_excel_command = new RelayCommand(execute_generate_price_list_excel);'
assert target_ctor in vm_content, 'target_ctor not found'
vm_content = vm_content.replace(target_ctor, repl_ctor, 1)

# Replace execute_generate_price_list implementation
target_method_start = '        private void execute_generate_price_list(object? parameter)'
target_method_end = '        private bool is_brand_selected(string? category)'

start_idx = vm_content.find(target_method_start)
end_idx = vm_content.find(target_method_end)
assert start_idx != -1 and end_idx != -1, 'execute_generate_price_list boundaries not found'

new_implementation = '''        private (List<price_list_item> items, List<price_list_column> columns)? prepare_price_list_data()
        {
            if (price_list_brand_options.Count == 0 || price_list_brand_options.All(o => !o.is_checked))
            {
                AppDialog.Show(
                    "Seleccione al menos una marca/categoría para generar la lista de precios.",
                    "Lista de precios",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }

            // Cargar precios propios si hay servicio
            Dictionary<int, Dictionary<int, decimal>> allOwn = new Dictionary<int, Dictionary<int, decimal>>();
            if (_price_list_service != null)
            {
                try
                {
                    allOwn = _price_list_service.GetAllOwnPricesAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    allOwn = new Dictionary<int, Dictionary<int, decimal>>();
                }
            }

            // Determinar listas seleccionadas
            var selectedLists = price_list_selection_options.Where(x => x.is_checked).ToList();
            bool includePrincipal = is_principal_included_in_report;

            if (!includePrincipal && selectedLists.Count == 0)
            {
                AppDialog.Show(
                    "Seleccione al menos una lista de precios para generar el reporte.",
                    "Lista de precios",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }

            var columns = new List<price_list_column>();
            if (includePrincipal)
            {
                columns.Add(new price_list_column { key = "ORIGINAL", name = _principal_price_list_name, is_original = true });
            }
            foreach (var sl in selectedLists)
            {
                columns.Add(new price_list_column { key = sl.id_price_list.ToString(), name = sl.name, is_original = false });
            }

            var items = new List<price_list_item>();

            foreach (product p in _all_products_source)
            {
                if (string.IsNullOrWhiteSpace(p.product_code) || string.IsNullOrWhiteSpace(p.name)) continue;
                if (p.unit_price_usd <= 0) continue;
                if (!is_brand_selected(p.category)) continue;

                var pricesByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                if (allOwn.TryGetValue(p.id_product, out var ownDict))
                {
                    foreach (var kv in ownDict)
                    {
                        pricesByKey[kv.Key.ToString()] = kv.Value;
                    }
                }

                items.Add(new price_list_item
                {
                    product_code = p.product_code,
                    name = p.name,
                    brand = p.category,
                    unit_price_usd = p.unit_price_usd,
                    prices_by_key = pricesByKey
                });
            }

            foreach (promotion promo in _all_promotions_source)
            {
                if (promo.items == null || promo.items.Count == 0) continue;
                if (promo.items.Any(i => i.product == null || i.quantity_required <= 0)) continue;

                string raw = promo.promotion_code ?? string.Empty;

                // Las ofertas no son precios de lista estandar.
                if (promo_is_oferta(raw)) continue;

                if (promo.unit_price_usd <= 0) continue;

                // Solo kits y combos (productos empaquetados con precio fijo). Si el
                // codigo viene del formato antiguo se deduce de la composicion.
                int tipo = promo_type_from_code(
                    raw,
                    promo.items?.Count ?? 0,
                    promo.items != null && promo.items.Count == 1 && promo.items[0].quantity_required == 1);

                if (tipo == 0) continue;

                string code = raw;

                var brands = promo.items
                    .Select(i => string.IsNullOrWhiteSpace(i.product!.category) ? "Otros" : i.product!.category!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(is_brand_selected)
                    .ToList();

                if (brands.Count == 0) continue;

                foreach (string brand in brands)
                {
                    items.Add(new price_list_item
                    {
                        product_code = code,
                        name = promo.name,
                        brand = brand,
                        unit_price_usd = promo.unit_price_usd,
                        prices_by_key = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    });
                }
            }

            return (items, columns);
        }

        private void execute_generate_price_list(object? parameter)
        {
            try
            {
                var data = prepare_price_list_data();
                if (data == null) return;

                PriceListPdfGenerator.generate(data.Value.items, data.Value.columns);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo generar la lista de precios en PDF: {ErrorText.Get(ex)}";
            }
        }

        private void execute_generate_price_list_excel(object? parameter)
        {
            try
            {
                var data = prepare_price_list_data();
                if (data == null) return;

                PriceListExcelGenerator.generate(data.Value.items, data.Value.columns);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo exportar la lista de precios a Excel: {ErrorText.Get(ex)}";
            }
        }

'''

vm_content = vm_content[:start_idx] + new_implementation + vm_content[end_idx:]

with open('src/NinOS.UI/Common/ViewModels/InventoryViewModel.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(vm_content)
print('InventoryViewModel.cs updated successfully!')

# 2. Patch InventoryView.xaml
with open('src/NinOS.UI/Views/InventoryView.xaml', 'r', encoding='utf-8') as f:
    view_content = f.read()

view_content = view_content.replace('\r\n', '\n')

old_button = '''                        <Button Content="📥 Descargar Lista (PDF)"
                                Background="#1B3A2D" Foreground="White" FontWeight="Bold"
                                FontSize="12" Height="30" BorderThickness="0" Cursor="Hand"
                                Click="on_download_price_list_click">
                            <Button.Template>
                                <ControlTemplate TargetType="Button">
                                    <Border x:Name="bd" Background="{TemplateBinding Background}" CornerRadius="4">
                                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property="IsMouseOver" Value="True">
                                            <Setter TargetName="bd" Property="Background" Value="#2E5A46"/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Button.Template>
                        </Button>'''

new_button = '''                        <Grid Margin="0,4,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="8"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <Button Grid.Column="0" Content="📥 PDF"
                                    Background="#1B3A2D" Foreground="White" FontWeight="Bold"
                                    FontSize="11.5" Height="30" BorderThickness="0" Cursor="Hand"
                                    Click="on_download_price_list_click">
                                <Button.Template>
                                    <ControlTemplate TargetType="Button">
                                        <Border x:Name="bd" Background="{TemplateBinding Background}" CornerRadius="4">
                                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                        </Border>
                                        <ControlTemplate.Triggers>
                                            <Trigger Property="IsMouseOver" Value="True">
                                                <Setter TargetName="bd" Property="Background" Value="#2E5A46"/>
                                            </Trigger>
                                        </ControlTemplate.Triggers>
                                    </ControlTemplate>
                                </Button.Template>
                            </Button>
                            <Button Grid.Column="2" Content="📊 Excel"
                                    Background="#107C41" Foreground="White" FontWeight="Bold"
                                    FontSize="11.5" Height="30" BorderThickness="0" Cursor="Hand"
                                    Click="on_download_price_list_excel_click">
                                <Button.Template>
                                    <ControlTemplate TargetType="Button">
                                        <Border x:Name="bd" Background="{TemplateBinding Background}" CornerRadius="4">
                                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                        </Border>
                                        <ControlTemplate.Triggers>
                                            <Trigger Property="IsMouseOver" Value="True">
                                                <Setter TargetName="bd" Property="Background" Value="#189953"/>
                                            </Trigger>
                                        </ControlTemplate.Triggers>
                                    </ControlTemplate>
                                </Button.Template>
                            </Button>
                        </Grid>'''

old_button = old_button.replace('\r\n', '\n')
new_button = new_button.replace('\r\n', '\n')

assert old_button in view_content, 'old_button not found in InventoryView.xaml'
view_content = view_content.replace(old_button, new_button, 1)

with open('src/NinOS.UI/Views/InventoryView.xaml', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(view_content)
print('InventoryView.xaml updated successfully!')

# 3. Patch InventoryView.xaml.cs
with open('src/NinOS.UI/Views/InventoryView.xaml.cs', 'r', encoding='utf-8') as f:
    code_content = f.read()

code_content = code_content.replace('\r\n', '\n')

old_code = '''        private void on_download_price_list_click(object sender, RoutedEventArgs e)
        {
            popup_reports.IsOpen = false;

            if (DataContext is InventoryViewModel view_model)
            {
                view_model.generate_price_list_command.Execute(null);
            }
        }'''

new_code = '''        private void on_download_price_list_click(object sender, RoutedEventArgs e)
        {
            popup_reports.IsOpen = false;

            if (DataContext is InventoryViewModel view_model)
            {
                view_model.generate_price_list_command.Execute(null);
            }
        }

        private void on_download_price_list_excel_click(object sender, RoutedEventArgs e)
        {
            popup_reports.IsOpen = false;

            if (DataContext is InventoryViewModel view_model)
            {
                view_model.generate_price_list_excel_command.Execute(null);
            }
        }'''

old_code = old_code.replace('\r\n', '\n')
new_code = new_code.replace('\r\n', '\n')

assert old_code in code_content, 'old_code not found in InventoryView.xaml.cs'
code_content = code_content.replace(old_code, new_code, 1)

with open('src/NinOS.UI/Views/InventoryView.xaml.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(code_content)
print('InventoryView.xaml.cs updated successfully!')

