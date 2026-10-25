import sys

path = "src/NinOS.UI/Views/DeliveryNotesView.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

old = """                            <Border Grid.Row="1" Grid.Column="2" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2">
                                <Border.Style>
                                    <Style TargetType="Border">
                                        <Setter Property="Visibility" Value="Visible"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding is_promo}" Value="True">
                                                <Setter Property="Visibility" Value="Collapsed"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Border.Style>
                                <TextBlock Text="CREDITO" FontStyle="Italic" TextAlignment="Center"/>
                            </Border>
                            <Border Grid.Row="1" Grid.Column="3" BorderBrush="Black" BorderThickness="0,0,0,1" Padding="2">
                                <Border.Style>
                                    <Style TargetType="Border">
                                        <Setter Property="Visibility" Value="Visible"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding is_promo}" Value="True">
                                                <Setter Property="Visibility" Value="Collapsed"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Border.Style>
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
                                    <TextBox Grid.Column="0" Text="{Binding credit_days_text, UpdateSourceTrigger=PropertyChanged, ValidatesOnExceptions=True}" BorderThickness="0" Background="Transparent" TextAlignment="Center" VerticalContentAlignment="Center" FontSize="13" Padding="4" PreviewTextInput="OnCreditDaysPreview" MaxLength="4"/>
                                    <TextBlock Grid.Column="1" Text="DIAS" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="Bold"/>
                                </Grid>
                            </Border>

                            <Border Grid.Row="2" Grid.Column="0" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2"><TextBlock Text="DOMICILIO FISCAL" FontStyle="Italic" TextAlignment="Center" TextWrapping="Wrap"/></Border>
                            <Border Grid.Row="2" Grid.Column="1" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2"><TextBlock Text="{Binding selected_customer.fiscal_address}" TextWrapping="Wrap" TextAlignment="Center"/></Border>
                            <Border Grid.Row="2" Grid.Column="2" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2">
                                <Border.Style>
                                    <Style TargetType="Border">
                                        <Setter Property="Visibility" Value="Visible"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding is_promo}" Value="True">
                                                <Setter Property="Visibility" Value="Collapsed"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Border.Style>
                                <TextBlock Text="Fecha Vencimiento" FontStyle="Italic" TextAlignment="Center"/>
                            </Border>
                            <Border Grid.Row="2" Grid.Column="3" BorderBrush="Black" BorderThickness="0,0,0,1" Padding="2">
                                <Border.Style>
                                    <Style TargetType="Border">
                                        <Setter Property="Visibility" Value="Visible"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding is_promo}" Value="True">
                                                <Setter Property="Visibility" Value="Collapsed"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Border.Style>
                                <DatePicker SelectedDate="{Binding due_date, ValidatesOnExceptions=True}" BorderThickness="0" HorizontalAlignment="Center"/>
                            </Border>"""

new = """                            <Border Grid.Row="1" Grid.Column="2" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2"><TextBlock Text="CREDITO" FontStyle="Italic" TextAlignment="Center"/></Border>
                            <Border Grid.Row="1" Grid.Column="3" BorderBrush="Black" BorderThickness="0,0,0,1" Padding="2">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
                                    <TextBox Grid.Column="0" Text="{Binding credit_days_text, UpdateSourceTrigger=PropertyChanged, ValidatesOnExceptions=True}" BorderThickness="0" Background="Transparent" TextAlignment="Center" VerticalContentAlignment="Center" FontSize="13" Padding="4" PreviewTextInput="OnCreditDaysPreview" MaxLength="4"/>
                                    <TextBlock Grid.Column="1" Text="DIAS" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="Bold"/>
                                </Grid>
                            </Border>

                            <Border Grid.Row="2" Grid.Column="0" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2"><TextBlock Text="DOMICILIO FISCAL" FontStyle="Italic" TextAlignment="Center" TextWrapping="Wrap"/></Border>
                            <Border Grid.Row="2" Grid.Column="1" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2"><TextBlock Text="{Binding selected_customer.fiscal_address}" TextWrapping="Wrap" TextAlignment="Center"/></Border>
                            <Border Grid.Row="2" Grid.Column="2" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2"><TextBlock Text="Fecha Vencimiento" FontStyle="Italic" TextAlignment="Center"/></Border>
                            <Border Grid.Row="2" Grid.Column="3" BorderBrush="Black" BorderThickness="0,0,0,1" Padding="2"><DatePicker SelectedDate="{Binding due_date, ValidatesOnExceptions=True}" BorderThickness="0" HorizontalAlignment="Center"/></Border>"""

if old not in content:
    print("NOT FOUND")
    sys.exit(1)
content = content.replace(old, new)
with open(path, "w", encoding="utf-8") as f:
    f.write(content)
print("done")
