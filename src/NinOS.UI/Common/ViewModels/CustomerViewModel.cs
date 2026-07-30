using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class customer_row_dto
    {
        public string id_display { get; set; } = string.Empty;
        public string customer_code { get; set; } = string.Empty;
        public string business_name { get; set; } = string.Empty;
        public string rif { get; set; } = string.Empty;
        public string contact_name { get; set; } = string.Empty;
        public string phone_number { get; set; } = string.Empty;
        public string fiscal_address { get; set; } = string.Empty;
        public string delivery_address { get; set; } = string.Empty;
        public string seller_name { get; set; } = string.Empty;
    }

    public class CustomerViewModel : ViewModelBase
    {
        private readonly List<customer_row_dto> _all_customers_source;
        private customer_row_dto? _editing_customer;

        private string _search_query = string.Empty;
        private int _selected_tab_index;
        private string _new_customer_code = string.Empty;
        private string _new_business_name = string.Empty;
        private string _new_rif = string.Empty;
        private string _new_contact_name = string.Empty;
        private string _new_phone_number = string.Empty;
        private string _new_fiscal_address = string.Empty;
        private string _new_delivery_address = string.Empty;
        private string _new_seller_name = string.Empty;

        public ObservableCollection<customer_row_dto> all_customers { get; }
        public ObservableCollection<customer_row_dto> juan_customers { get; }
        public ObservableCollection<customer_row_dto> sandra_customers { get; }
        public ObservableCollection<customer_row_dto> anais_customers { get; }
        public ObservableCollection<string> seller_options { get; }

        public ICommand open_add_window_command { get; }
        public ICommand save_customer_command { get; }
        public ICommand edit_customer_command { get; }

        public Action? on_request_add_customer_window;
        public Action? on_close_add_customer_window;

        public string search_query
        {
            get { return _search_query; }
            set
            {
                _search_query = value;
                on_property_changed();
                filter_customers();
            }
        }

        public int selected_tab_index
        {
            get { return _selected_tab_index; }
            set
            {
                _selected_tab_index = value;
                on_property_changed();
                apply_default_seller_by_tab();
                on_property_changed(nameof(can_edit_seller));
            }
        }

        public bool can_edit_seller
        {
            get { return _selected_tab_index == 0; }
        }

        public string add_or_edit_title
        {
            get { return _editing_customer == null ? "Añadir Cliente" : "Editar Cliente"; }
        }

        public string save_button_text
        {
            get { return _editing_customer == null ? "Guardar Cliente" : "Guardar Cambios"; }
        }

        public string new_customer_code
        {
            get { return _new_customer_code; }
            set { _new_customer_code = value; on_property_changed(); }
        }

        public string new_business_name
        {
            get { return _new_business_name; }
            set { _new_business_name = value; on_property_changed(); }
        }

        public string new_rif
        {
            get { return _new_rif; }
            set { _new_rif = value; on_property_changed(); }
        }

        public string new_contact_name
        {
            get { return _new_contact_name; }
            set { _new_contact_name = value; on_property_changed(); }
        }

        public string new_phone_number
        {
            get { return _new_phone_number; }
            set { _new_phone_number = value; on_property_changed(); }
        }

        public string new_fiscal_address
        {
            get { return _new_fiscal_address; }
            set { _new_fiscal_address = value; on_property_changed(); }
        }

        public string new_delivery_address
        {
            get { return _new_delivery_address; }
            set { _new_delivery_address = value; on_property_changed(); }
        }

        public string new_seller_name
        {
            get { return _new_seller_name; }
            set { _new_seller_name = value; on_property_changed(); }
        }

        public string add_button_text
        {
            get { return "+ Añadir Cliente"; }
        }

        public CustomerViewModel()
        {
            _all_customers_source = new List<customer_row_dto>();

            all_customers = new ObservableCollection<customer_row_dto>();
            juan_customers = new ObservableCollection<customer_row_dto>();
            sandra_customers = new ObservableCollection<customer_row_dto>();
            anais_customers = new ObservableCollection<customer_row_dto>();
            seller_options = new ObservableCollection<string> { "Juan", "Sandra", "Anais" };

            open_add_window_command = new RelayCommand(execute_open_add_window);
            save_customer_command = new RelayCommand(execute_save_customer);
            edit_customer_command = new RelayCommand(execute_edit_customer);

            load_initial_anais_customers();
            apply_default_seller_by_tab();
            filter_customers();
        }

        private void load_initial_anais_customers()
        {
            _all_customers_source.AddRange(new[]
            {
                create_seed_customer("3300_01", "BAZAR SUPER COMPLETO C.A", "J-294385915", "CHICHI", "0412 8861755", "CALLE SUCRE LOCAL 98-31 SECTOR TOCUYITO TOCUYITO CARABOBO ZONA POSTAL 2001", "CALLE SUCRE LOCAL 98-31 SECTOR TOCUYITO TOCUYITO CARABOBO ZONA POSTAL 2002"),
                create_seed_customer("3300_02", "AUTOMERCADO MARKET CENTER II C.A", "J-401202969", "XVENA", "0412 8812458", "CALLE SUCRE C/C CALLE CEDEÑO CC LAS 3 J NIVEL S/E LOCAL 4 ZONA TOCUYITO TOCUYITO ZONA POSTAL 2035", "DIAGONAL A CHICHI CALLE SUCRE C/C CALLE CEDEÑO CC LAS 3 J NIVEL S/E LOCAL 4 ZONA TOCUYITO TOCUYITO ZONA POSTAL 2035"),
                create_seed_customer("3300_03", "INVERSIONES LISBEMAR C.A", "J-312573023", "IRIS / ANGELA", "0424 425 6174", "CALLE GIRARDOT CC MI VIEJO MERCADO NIVEL P.B LOCAL PASILLO COLOMBIA LOCAL O_12 SECTOR CENTRO VALENCIA CARABOBO ZONA POSTAL 2001", "BELLA FLORIDA DETRAS DE LA GUACAMAYA AV PRINCIPAL BELLA FLORIDA AV 113 VALENCIA 2001"),
                create_seed_customer("3300_04", "INVERSIONES SAM LYZ C.A", "J-405797444", "ANA / LISETH", "0412 7439938", "AV PRINCIPAL DE LA COMUNIDAD DE SAN JOSE DE LOS CHORRITOS CASA NRO 2 LOCAL 4 SECTOR TOCUYITO TOCUYITO CARABOBO ZONA POSTAL 2053", "AV PRINCIPAL DE LA COMUNIDAD DE SAN JOSE DE LOS CHORRITOS CASA NRO 2 LOCAL 4 SECTOR TOCUYITO TOCUYITO CARABOBO ZONA POSTAL 2053"),
                create_seed_customer("3300_05", "INVERSIONES NAILS A.Z.M C.A (MUJER BONITA)", "J-500650078", "MINERVA", "0412 533 63 24", "AV 113 /114 LOCAL NRO 01 URB BELLA FLORIDA SECTOR 20 VALENCIA CARABOBO ZONA POSTAL 2003", "MUJER BONITA AV 113 /114 LOCAL NRO 01 URB BELLA FLORIDA SECTOR 20 VALENCIA CARABOBO ZONA POSTAL 2004"),
                create_seed_customer("3300_06", "QUINCALLERIA FELIZ C.A", "J-299286664", "KATTY", "0412-8905708", "AV BOLIVAR PPAL LOC 10 PB SECTOR CENTRAL TACARIGUA VALENCIA EDO CARABOBO", "AV BOLIVAR PPAL LOC 10 PB SECTOR CENTRAL TACARIGUA VALENCIA EDO CARABOBO"),
                create_seed_customer("3300_07", "COMERCIALIZADORA WLAFRANSANG C.A", "J-501038082", "", "0412-400.37.00", "CALLE SUCRE C/C CEDEÑO CC LAS 3 J NIVEL P/B LOCAL 2 Y 3 SECTOR TOCUYITO TOCUYITO CARABOBO ZONA POSTAL 2035", "CALLE SUCRE C/C CEDEÑO CC LAS 3 J NIVEL P/B LOCAL 2 Y 3 SECTOR TOCUYITO TOCUYITO CARABOBO ZONA POSTAL 2035"),
                create_seed_customer("3300_08", "COMERCIALIZADORA CUTE MIAO 2023 C.A", "J-504229822", "PAOLA", "0424-843.70.74", "CALLE SUCRE CRUCE CON LA AVENIDA BOLIVAR CC SANTA FORTUNATA NIVEL PLANTA BAJA LOCAL 01, 02 Y 03 SECTOR S/N TOCUYITO CARABOBO ZONA POSTAL 2036", "CALLE SUCRE CRUCE CON LA AVENIDA BOLIVAR CC SANTA FORTUNATA NIVEL PLANTA BAJA LOCAL 01, 02 Y 03 SECTOR S/N TOCUYITO CARABOBO ZONA POSTAL 2036"),
                create_seed_customer("3300_09", "ROMENSAN DISTRIBUIDORA C.A", "J-405448997", "", "0412-435.87.08", "AV SUCRE NOR 06 LOCAL 3 FRENTE A MONUMENTO SAN JOAQUIN", "AV SUCRE NOR 06 LOCAL 3 FRENTE A MONUMENTO SAN JOAQUIN"),
                create_seed_customer("3300_10", "INVERSIONES NESYO 2022 C.A", "J-502657100", "NESTOR / EGLEE", "0412-500.63.06", "CALLE 73 CC MEDIA LUNA NIVEL S/N LOCAL 5 BARRIO EL CARMEN VALENCIA CARABOBO ZONA POSTAL 2001", "TERMINAL DE MERCADO GOAJIROS CALLE 73 CC MEDIA LUNA NIVEL S/N LOCAL 5 BARRIO EL CARMEN VALENCIA CARABOBO ZONA POSTAL 2002"),
                create_seed_customer("3300_11", "INVERSIONES P & T 2022 C.A", "J-502178929", "", "", "CALLE VALENCIA CASA NRO 19 SECTOR LA ALIANZA CENTRAL TACARIGUA CARABOBO ZONA POSTAL 2010", "CALLE VALENCIA CASA NRO 19 SECTOR LA ALIANZA CENTRAL TACARIGUA CARABOBO ZONA POSTAL 2011"),
                create_seed_customer("3300_12", "SUPERMERCADO KAIROS C.A", "J-408247054", "TISIANA", "0412-747.43.96", "AV BOLIVAR CALLE 11-2 CC TACARIGUA NIVEL P.B LOCAL 4 SECTOR CENTRAL TACARIGUA CARABOBO ZONA POSTAL 2010", "AV BOLIVAR CALLE 11-2 CC TACARIGUA NIVEL P.B LOCAL 4 SECTOR CENTRAL TACARIGUA CARABOBO ZONA POSTAL 2011"),
                create_seed_customer("3300_13", "BUENOS ESTILOS C.A", "J-400334357", "DARLIN", "0412.501.84.34", "AV 113 NRO 20-10 LOCAL 01 URB BELLA FLORIDA VALENCIA CARABOBO ZONA POSTAL 2003", "AV 113 NRO 20-10 LOCAL 01 URB BELLA FLORIDA VALENCIA CARABOBO ZONA POSTAL 2003"),
                create_seed_customer("3300_14", "SALON DE BELLEZA STYLOS CHAVEZ F.P", "V-15446408", "YANET CHAVEZ", "0412-412.11.93", "AV 01 CASA NRO 30 URB LAS AGUITAS SECTOR 02 LOS GUAYOS EDO CARABOBO", "AV 01 CASA NRO 30 URB LAS AGUITAS SECTOR 02 LOS GUAYOS EDO CARABOBO"),
                create_seed_customer("3300_15", "MEGA SOL C.A", "J-308660698", "LILI DENG", "0412-762.56.33", "AV BOLIVAR URB FLOR AMARILLO C.C OASIS LOCAL 01 VALENCIA EDO CARABOBO", "AV BOLIVAR URB FLOR AMARILLO C.C OASIS LOCAL 01 VALENCIA EDO CARABOBO"),
                create_seed_customer("3300_16", "SUPERMERCADO GRAN OFERTA DE CARABOBO C.A", "J-315780100", "MEKY", "0412.036.80.31", "AV EL APO CARABOBO LOCAL NRO 07 SECTOR CAMPOCARABOBO TOCUYITO CARABOBO ZONA POSTAL 2035", "PASANDO CAMPO CARABOBO EN EL RETORNO AV EL APO CARABOBO LOCAL NRO 07 SECTOR CAMPOCARABOBO TOCUYITO CARABOBO ZONA POSTAL 2036"),
                create_seed_customer("3300_17", "ATIMAS S & S C.A", "J-411942960", "", "", "CALLE SUCRE CON CALLE COLOMBIA CC SUCRE NRO 10-35 NIVEL S/N LOCAL 2 SECTOR S/N SAN JOAQUIN CARABOBO ZONA POSTAL 2018", "CALLE SUCRE CON CALLE COLOMBIA CC SUCRE NRO 10-35 NIVEL S/N LOCAL 2 SECTOR S/N SAN JOAQUIN CARABOBO ZONA POSTAL 2018"),
                create_seed_customer("3300_18", "TASCA RESTAURANT EL ALBORAL C.A", "J-406474517", "SUSAN", "0412.334.66.88", "AV BOLIVAR DE FLOR AMARILLO CC EL ALBORAL NIVEL 1 LOCAL 9 Y 10 SECTOR FLOR AMARILLO VALENCIA EDO CARABOBO", "AV BOLIVAR DE FLOR AMARILLO CC EL ALBORAL NIVEL 1 LOCAL 9 Y 10 SECTOR FLOR AMARILLO VALENCIA EDO CARABOBO"),
                create_seed_customer("3300_19", "SUPER MERCADO PINO C.A", "J-314051148", "LIN XIANTING", "0424-843.70.74", "AV BOLIVAR C/C SUCRE TOCUYITO EDIF VA-PAZ PISO P.B LOCAL 02 ZONA CENTRO TOCUYITO CARABOBO ZONA POSTAL 2035", "AV BOLIVAR C/C SUCRE TOCUYITO EDIF VA-PAZ PISO P.B LOCAL 02 ZONA CENTRO TOCUYITO CARABOBO ZONA POSTAL 2035"),
                create_seed_customer("3300_20", "INVERSIONES SAN LUIS VALENCIA C.A", "J-412509853", "DANIELA", "0412.454.38.39", "AUTOPISTA VALENCIA CAMPO CARABOBO LOCALES 29,30,31 Y 32 MERCADO MAYORISTA PLANTA ALTA TOCUYITO EDO CARABOBO", "AUTOPISTA VALENCIA CAMPO CARABOBO LOCALES 29,30,31 Y 32 MERCADO MAYORISTA PLANTA ALTA TOCUYITO EDO CARABOBO"),
                create_seed_customer("3300_21", "BLUE STORE GM", "V-22204645", "MARIANNY MEDINA", "0412.406.30.48", "AV PRINCIPAL FLOR AMARILLO FRENTE AL C.C OASIS AL LADO DEL LOCAL DE LA PELUQUERIA", "AV PRINCIPAL FLOR AMARILLO FRENTE AL C.C OASIS AL LADO DEL LOCAL DE LA PELUQUERIA"),
                create_seed_customer("3300_22", "SUPERMERCADO DIEGON C.A", "J-297512195", "CELINA - EMILY", "0424-498.66.51 / 0412.912.2109", "CALLE AREVALO LOCAL NRO 01 SECTOR CASCO CENTRAL TOCUYITO", "ANTES DEL CRUCE PI Y CU CALLE AREVALO LOCAL NRO 01 SECTOR CASCO CENTRAL TOCUYITO"),
                create_seed_customer("3300_23", "SUPERMERCADO BELLA FLORIDA C.A", "J-504310719", "RAY", "0412.483.34.99", "AV 129 GALPON 1 ZONA INDUSTRIAL LA GUACAMAYA VALENCIA CARABOBO ZONA POSTAL 2003", "ENTRANDO POR LA AUTOPISTA AV 129 GALPON 1 ZONA INDUSTRIAL LA GUACAMAYA VALENCIA CARABOBO ZONA POSTAL 2003"),
                create_seed_customer("3300_24", "MAXI MERCADO BELLA FLORIDA", "J-503769866", "YENIFER", "0412-534.26.02", "AV PRINCIPAL BELLA FLORIDA C.C CONJUNTO RESIDENCIAL DIEGO 1 PRIMERA ETAPA NIVEL P.B LOCAL A,B,C,D URB PARQUE RESIDENCIAL LA FLORIDA SECTOR 2 VALENCIA CARABOBO ZONA POSTAL 2001", ""),
                create_seed_customer("3300_25", "COMERCIAL EL FUTURO STYLE C.A", "J-403881685", "EMILY", "0412-742.52.58", "CALLE PRINCIPAL CENTRO COMERCIAL HERMANOS BOSCO NRO 93-11 LOCAL 7 Y 8 URBANIZACION POPULAR HERMOGENES LOPEZ VALENCIA", ""),
                create_seed_customer("3300_26", "ANGEL FONG", "", "", "", "", ""),
                create_seed_customer("3300_27", "SUPERMERCADO PANDA 888 C.A", "J-500038747", "KALINA", "0412-488.83.18 / 0424-417.66.25", "AV BOLIVAR C.C IBERIA NIVEL 1 LOCAL 1 SECTOR CENTRAL TACARIGUA CARABOBO ZONA POSTAL 2010", "AV PRINCIPAL CENTRAL AV BOLIVAR C.C IBERIA NIVEL 1 LOCAL 1 SECTOR CENTRAL TACARIGUA CARABOBO ZONA POSTAL 2011"),
                create_seed_customer("3300_28", "INVERSIONES LA GRANDEZA LIANG C.A", "J-296678278", "", "0412-083.19.92", "CALLE BOLIVAR NRO 23 SECTOR CENTRO DE GUACARA EDO CARABOBO", "UNA CUADRA ANTES DE LA IGLESIA CALLE SUBIENDO A LA PLAZA CALLE BOLIVAR NRO 23 SECTOR CENTRO DE GUACARA EDO CARABOBO"),
                create_seed_customer("3300_29", "DIVAS SHOP VALENCIA", "J-502940057", "YULI", "0412-139.55.66", "AV BOLIVAR CRUCE CON CALLE AREVALO EDIFICIO GIAMBALVO PISO S/N LOCAL 2 SECTOR S/N GUACARA CARABOBO", "EN LA ESQUINA DE LA PLAZA GUACARA AV BOLIVAR CRUCE CON CALLE AREVALO EDIFICIO GIAMBALVO PISO S/N LOCAL 2 SECTOR S/N GUACARA CARABOBO"),
                create_seed_customer("3300_30", "UNIMARKETT LA FE C.A", "J-409939197", "WENDY CELIS", "0414-048.58.15", "CALLE MORILLO LOCAL NRO 003 SECTOR LAS MANZANAS CAMPO CARABOBO TOCUYITO ZONA POSTAL 2035", "CALLE MORILLO LOCAL NRO 003 SECTOR LAS MANZANAS CAMPO CARABOBO TOCUYITO ZONA POSTAL 2035")
            });
        }

        private customer_row_dto create_seed_customer(
            string customer_code,
            string business_name,
            string rif,
            string contact_name,
            string phone_number,
            string fiscal_address,
            string delivery_address)
        {
            return new customer_row_dto
            {
                customer_code = customer_code,
                business_name = business_name,
                rif = rif,
                contact_name = contact_name,
                phone_number = phone_number,
                fiscal_address = fiscal_address,
                delivery_address = delivery_address,
                seller_name = "Anais"
            };
        }

        private void apply_default_seller_by_tab()
        {
            if (_selected_tab_index == 1)
            {
                new_seller_name = "Juan";
                return;
            }

            if (_selected_tab_index == 2)
            {
                new_seller_name = "Sandra";
                return;
            }

            new_seller_name = "Anais";
        }

        private void filter_customers()
        {
            IEnumerable<customer_row_dto> filtered = _all_customers_source.Where(customer_item =>
                string.IsNullOrWhiteSpace(_search_query) ||
                customer_item.customer_code.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.business_name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.rif.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.contact_name.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.phone_number.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.fiscal_address.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.delivery_address.Contains(_search_query, StringComparison.OrdinalIgnoreCase) ||
                customer_item.seller_name.Contains(_search_query, StringComparison.OrdinalIgnoreCase));

            all_customers.Clear();
            juan_customers.Clear();
            sandra_customers.Clear();
            anais_customers.Clear();

            int sequence = 1;

            foreach (customer_row_dto customer_item in filtered)
            {
                customer_item.id_display = sequence.ToString();
                all_customers.Add(customer_item);
                sequence++;

                if (string.Equals(customer_item.seller_name, "Juan", StringComparison.OrdinalIgnoreCase))
                {
                    juan_customers.Add(customer_item);
                }
                else if (string.Equals(customer_item.seller_name, "Sandra", StringComparison.OrdinalIgnoreCase))
                {
                    sandra_customers.Add(customer_item);
                }
                else if (string.Equals(customer_item.seller_name, "Anais", StringComparison.OrdinalIgnoreCase))
                {
                    anais_customers.Add(customer_item);
                }
            }
        }

        private void execute_open_add_window(object? parameter)
        {
            _editing_customer = null;
            clear_form_values();
            apply_default_seller_by_tab();
            on_property_changed(nameof(add_or_edit_title));
            on_property_changed(nameof(save_button_text));
            on_request_add_customer_window?.Invoke();
        }

        private void execute_edit_customer(object? parameter)
        {
            if (parameter is not customer_row_dto customer_item)
            {
                return;
            }

            _editing_customer = customer_item;
            new_customer_code = customer_item.customer_code;
            new_business_name = customer_item.business_name;
            new_rif = customer_item.rif;
            new_contact_name = customer_item.contact_name;
            new_phone_number = customer_item.phone_number;
            new_fiscal_address = customer_item.fiscal_address;
            new_delivery_address = customer_item.delivery_address;
            new_seller_name = customer_item.seller_name;

            on_property_changed(nameof(add_or_edit_title));
            on_property_changed(nameof(save_button_text));
            on_request_add_customer_window?.Invoke();
        }

        private void execute_save_customer(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(new_customer_code) || string.IsNullOrWhiteSpace(new_business_name))
            {
                return;
            }

            if (_editing_customer != null)
            {
                _editing_customer.customer_code = new_customer_code.Trim();
                _editing_customer.business_name = new_business_name.Trim();
                _editing_customer.rif = new_rif.Trim();
                _editing_customer.contact_name = new_contact_name.Trim();
                _editing_customer.phone_number = new_phone_number.Trim();
                _editing_customer.fiscal_address = new_fiscal_address.Trim();
                _editing_customer.delivery_address = new_delivery_address.Trim();
                _editing_customer.seller_name = new_seller_name.Trim();
            }
            else
            {
                _all_customers_source.Add(new customer_row_dto
                {
                    customer_code = new_customer_code.Trim(),
                    business_name = new_business_name.Trim(),
                    rif = new_rif.Trim(),
                    contact_name = new_contact_name.Trim(),
                    phone_number = new_phone_number.Trim(),
                    fiscal_address = new_fiscal_address.Trim(),
                    delivery_address = new_delivery_address.Trim(),
                    seller_name = string.IsNullOrWhiteSpace(new_seller_name) ? "Anais" : new_seller_name.Trim()
                });
            }

            _editing_customer = null;
            clear_form_values();
            filter_customers();
            on_property_changed(nameof(add_or_edit_title));
            on_property_changed(nameof(save_button_text));
            on_close_add_customer_window?.Invoke();
        }

        private void clear_form_values()
        {
            new_customer_code = string.Empty;
            new_business_name = string.Empty;
            new_rif = string.Empty;
            new_contact_name = string.Empty;
            new_phone_number = string.Empty;
            new_fiscal_address = string.Empty;
            new_delivery_address = string.Empty;
        }
    }
}