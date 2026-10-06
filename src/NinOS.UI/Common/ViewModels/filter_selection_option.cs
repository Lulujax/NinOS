using System;

namespace NinOS.UI.Common.ViewModels
{
    public class filter_selection_option : ViewModelBase
    {
        private bool _is_checked;
        private readonly Action? _on_changed;

        public int? id { get; }
        public string name { get; }

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

        public filter_selection_option(string name, Action? on_changed, bool initial_checked = true, int? id = null)
        {
            this.name = name;
            this.id = id;
            _on_changed = on_changed;
            _is_checked = initial_checked;
        }
    }
}
