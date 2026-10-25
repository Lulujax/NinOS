path = 'src/NinOS.UI/Views/DeliveryNotesView.xaml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '''                            <Border Grid.Row="0" Grid.Column="1" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2">
                                <ComboBox ItemsSource="{Binding filtered_customers}" SelectedItem="{Binding selected_customer}" DisplayMemberPath="business_name" IsEditable="True" IsTextSearchEnabled="True" TextSearch.TextPath="business_name" BorderThickness="0" Background="Transparent"/>
                            </Border>'''

new = '''                            <Border Grid.Row="0" Grid.Column="1" BorderBrush="Black" BorderThickness="0,0,1,1" Padding="2">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="22"/>
                                    </Grid.ColumnDefinitions>
                                    <TextBox Grid.Column="0" Text="{Binding customer_search_text, UpdateSourceTrigger=PropertyChanged}" BorderThickness="0" Background="Transparent" Padding="2" VerticalContentAlignment="Center"/>
                                    <Button Grid.Column="1" Content="&#x25BC;" FontSize="9" Foreground="#555555" Background="Transparent" BorderThickness="0" Padding="0" Cursor="Hand" ToolTip="Ver todos" Command="{Binding toggle_customer_popup_command}">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <Border Background="Transparent" Padding="0,0,2,0">
                                                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter Property="Background" Value="#E0E0E0"/>
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Button.Template>
                                    </Button>
                                    <Popup Grid.ColumnSpan="2" IsOpen="{Binding show_customer_popup, Mode=TwoWay}" Placement="Bottom" StaysOpen="False" AllowsTransparency="False" PopupAnimation="Fade">
                                        <Border Background="White" BorderBrush="#888888" BorderThickness="1" MaxHeight="200" MinWidth="400" CornerRadius="2" Padding="2">
                                            <ListBox ItemsSource="{Binding filtered_customers_display}" BorderThickness="0" ScrollViewer.VerticalScrollBarVisibility="Auto" VirtualizingPanel.IsVirtualizing="True" VirtualizingPanel.VirtualizationMode="Recycling" Background="White">
                                                <ListBox.ItemTemplate>
                                                    <DataTemplate>
                                                        <Border Padding="4,3" Cursor="Hand" BorderBrush="#EEEEEE" BorderThickness="0,0,0,1" Background="White">
                                                            <Border.Style>
                                                                <Style TargetType="Border">
                                                                    <Style.Triggers>
                                                                        <Trigger Property="IsMouseOver" Value="True">
                                                                            <Setter Property="Background" Value="#E8F5E9"/>
                                                                        </Trigger>
                                                                    </Style.Triggers>
                                                                </Style>
                                                            </Border.Style>
                                                            <Border.InputBindings>
                                                                <MouseBinding MouseAction="LeftClick" Command="{Binding DataContext.select_customer_command, RelativeSource={RelativeSource AncestorType=Popup}}" CommandParameter="{Binding}"/>
                                                            </Border.InputBindings>
                                                            <TextBlock Text="{Binding business_name}" FontSize="11" Foreground="Black"/>
                                                        </Border>
                                                    </DataTemplate>
                                                </ListBox.ItemTemplate>
                                            </ListBox>
                                        </Border>
                                    </Popup>
                                </Grid>
                            </Border>'''

if old not in content:
    print('NOT FOUND')
    raise SystemExit
content = content.replace(old, new)
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
print('done')
