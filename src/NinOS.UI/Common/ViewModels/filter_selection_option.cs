using System;

namespace NinOS.UI.Common.ViewModels
{
    public class filter_selection_option : ViewModelBase
    {
        private bool _is_checked;
        private readonly Action? _on_changed;

        public int? id { get; }
        public string name { get; }

        /// <summary>
        /// Texto que se muestra en pantalla. `name` es el valor con el que se filtra (el nombre
        /// real del vendedor/zona) y este es solo la etiqueta: sirve para marcar los que ya no
        /// estan activos sin romper el filtro por nombre.
        /// </summary>
        public string display_name { get; }

        public bool is_checked
        {
            get => _is_checked;
            set
            {
                if (_is_checked == value) return;
                _is_checked = value;
                on_property_changed();
                _on_changed?.Invoke();
            }
        }

        public filter_selection_option(string name, Action? on_changed, bool initial_checked = true, int? id = null, string? display_name = null)
        {
            this.name = name;
            this.id = id;
            this.display_name = string.IsNullOrWhiteSpace(display_name) ? name : display_name;
            _on_changed = on_changed;
            _is_checked = initial_checked;
        }
    }
}
