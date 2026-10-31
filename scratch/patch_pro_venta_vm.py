import sys

with open('src/NinOS.UI/Common/ViewModels/ProVentaViewModel.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Update load_report_async
old_load = '''        private async Task load_report_async()
        {
            if (_selected_week == null)
            {
                _report = null;
                report_rows.Clear();
                raise_report_changed();
                return;
            }

            try
            {
                var report = await _pro_venta_service.get_weekly_report_async(_selected_week);'''

new_load = '''        private async Task load_report_async()
        {
            if (_selected_week == null && _selected_relation == null)
            {
                _report = null;
                report_rows.Clear();
                raise_report_changed();
                return;
            }

            try
            {
                pro_venta_weekly_dto report;
                if (_selected_relation != null && _selected_relation.id_relacion > 0)
                {
                    report = await _pro_venta_service.get_relation_report_async(_selected_relation.id_relacion);
                }
                else if (_selected_week != null)
                {
                    report = await _pro_venta_service.get_weekly_report_async(_selected_week);
                }
                else
                {
                    return;
                }'''

# Normalize newlines for replacement
content = content.replace('\r\n', '\n')
old_load = old_load.replace('\r\n', '\n')
new_load = new_load.replace('\r\n', '\n')

if old_load not in content:
    print('Error: old_load not found!')
    sys.exit(1)
content = content.replace(old_load, new_load, 1)

# 2. Update execute_relation_detail_pdf
old_exec_detail = '''        private async void execute_relation_detail_pdf(object? parameter)
        {
            if (parameter is not pro_venta_relation_row row) return;

            try
            {
                var week = new pro_venta_week_info
                {
                    week_index = row.relation_number,
                    start = row.week_start,
                    end = row.week_end,
                    label = $"{row.week_start:dd} AL {row.week_end:dd}"
                };

                var report = await _pro_venta_service.get_weekly_report_async(week);'''

new_exec_detail = '''        private async void execute_relation_detail_pdf(object? parameter)
        {
            if (parameter is not pro_venta_relation_row row) return;

            try
            {
                var report = await _pro_venta_service.get_relation_report_async(row.id_relacion);'''

old_exec_detail = old_exec_detail.replace('\r\n', '\n')
new_exec_detail = new_exec_detail.replace('\r\n', '\n')

if old_exec_detail not in content:
    print('Error: old_exec_detail not found!')
    sys.exit(1)
content = content.replace(old_exec_detail, new_exec_detail, 1)

# 3. Update print_relation_pdf_async
old_print_rel = '''        public async Task print_relation_pdf_async(pro_venta_relation_row row)
        {
            try
            {
                var week = new pro_venta_week_info
                {
                    week_index = row.relation_number,
                    start = row.week_start,
                    end = row.week_end,
                    label = $"{row.week_start:dd} AL {row.week_end:dd}"
                };

                var report = await _pro_venta_service.get_weekly_report_async(week);'''

new_print_rel = '''        public async Task print_relation_pdf_async(pro_venta_relation_row row)
        {
            try
            {
                var report = await _pro_venta_service.get_relation_report_async(row.id_relacion);'''

old_print_rel = old_print_rel.replace('\r\n', '\n')
new_print_rel = new_print_rel.replace('\r\n', '\n')

if old_print_rel not in content:
    print('Error: old_print_rel not found!')
    sys.exit(1)
content = content.replace(old_print_rel, new_print_rel, 1)

# 4. Update sync_relation_with_week
old_sync_rel = '''        private void sync_relation_with_week()
        {
            if (_is_syncing_week_relation) return;
            if (_selected_week == null)
            {
                if (_selected_relation != null)
                {
                    _selected_relation = null;
                    on_property_changed(nameof(selected_relation));
                }
                return;
            }

            _is_syncing_week_relation = true;'''

new_sync_rel = '''        private void sync_relation_with_week()
        {
            if (_is_syncing_week_relation) return;
            if (_selected_week == null)
            {
                if (_selected_relation != null)
                {
                    _selected_relation = null;
                    on_property_changed(nameof(selected_relation));
                }
                return;
            }

            if (_selected_relation != null && _selected_relation.week_start.Date == _selected_week.start.Date)
            {
                return;
            }

            _is_syncing_week_relation = true;'''

old_sync_rel = old_sync_rel.replace('\r\n', '\n')
new_sync_rel = new_sync_rel.replace('\r\n', '\n')

if old_sync_rel not in content:
    print('Error: old_sync_rel not found!')
    sys.exit(1)
content = content.replace(old_sync_rel, new_sync_rel, 1)

# 5. Update sync_week_with_relation (call load_report_async even if current_week_match == _selected_week)
old_sync_wk = '''                var current_week_match = weeks.FirstOrDefault(w => w.start.Date == target_start);
                if (current_week_match != null)
                {
                    if (_selected_week != current_week_match)
                    {
                        _selected_week = current_week_match;
                        on_property_changed(nameof(selected_week));
                        if (!_is_loading)
                        {
                            _ = load_report_async();
                        }
                    }
                    return;
                }'''

new_sync_wk = '''                var current_week_match = weeks.FirstOrDefault(w => w.start.Date == target_start);
                if (current_week_match != null)
                {
                    if (_selected_week != current_week_match)
                    {
                        _selected_week = current_week_match;
                        on_property_changed(nameof(selected_week));
                    }
                    if (!_is_loading)
                    {
                        _ = load_report_async();
                    }
                    return;
                }'''

old_sync_wk = old_sync_wk.replace('\r\n', '\n')
new_sync_wk = new_sync_wk.replace('\r\n', '\n')

if old_sync_wk not in content:
    print('Error: old_sync_wk not found!')
    sys.exit(1)
content = content.replace(old_sync_wk, new_sync_wk, 1)

with open('src/NinOS.UI/Common/ViewModels/ProVentaViewModel.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(content)

print('ProVentaViewModel.cs updated successfully!')

