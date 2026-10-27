using System;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    /// <summary>
    /// Fila de la seccion "Precios por lista" de la ventana de producto. El texto
    /// vacio significa herencia: esa lista usa el precio original del producto.
    /// </summary>
    public class product_price_list_row : ViewModelBase
    {
        public int id_price_list { get; set; }
        public string name { get; set; } = string.Empty;

        private string _price_text = string.Empty;
        public string price_text
        {
            get { return _price_text; }
            set { _price_text = value; on_property_changed(); }
        }

        /// <summary>
        /// Precio propio guardado en base (null = heredado). Solo se usa al cargar:
        /// la edicion vive en price_text para que vacio siga significando herencia.
        /// </summary>
        public decimal? own_price { get; set; }

        public string effective_price_display
        {
            get
            {
                if (decimal.TryParse((price_text ?? string.Empty).Trim().Replace(',', '.'),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal precio)
                    && !string.IsNullOrWhiteSpace(price_text))
                {
                    return precio.ToString("N2");
                }
                return original_price_display;
            }
        }

        private string _original_price_display = string.Empty;
        public string original_price_display
        {
            get { return _original_price_display; }
            set
            {
                _original_price_display = value;
                on_property_changed();
                on_property_changed(nameof(effective_price_display));
            }
        }

        public bool is_inheriting
        {
            get { return string.IsNullOrWhiteSpace(price_text); }
        }
    }
}
